﻿using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Foundation;
using FlowerPot.Logging;

namespace FlowerPot.Connection
{
    public class FlowerConnection : IDisposable
    {
         private Logger _log;

        private readonly static string _serviceNameDefault = "net.manipulatormanor.LoopyWebServer";
        private readonly static string _serviceFamilyNameDefault = "LoopyVideo.WebService-uwp_n1q2psqd6svm2";
        /// <summary>
        /// Message Received event handler
        /// </summary>
        /// <param name="command">The command received</param>
        /// <returns>The current status </returns>
        public delegate AppMessage ReceiveMessage(AppMessage command);
        /// <summary>
        /// Event fired when a message is received by from the app service
        /// </summary>
        public event ReceiveMessage MessageReceived;

        private string _serviceName;
        /// <summary>
        /// The name of the service to connect to.
        /// </summary>
        public string ServiceName
        {
            get
            {
                if(string.IsNullOrEmpty(_serviceName))
                {
                    _serviceName = _serviceNameDefault;
                }
                return _serviceName;
            }
            set { _serviceName = value; }
        }

        private string _familyName;
        /// <summary>
        /// The name of the service family of the app to connect to.
        /// </summary>
        public string ServiceFamilyName
        {
            get
            {
                if (string.IsNullOrEmpty(_familyName))
                {
                    _familyName = _serviceFamilyNameDefault;
                }
                return _familyName;
            }
            set { _familyName = value; }
        }

        private object _connectionLock = new object();    
        private AppServiceConnection _connection;
        /// <summary>
        /// The connections to the app service
        /// </summary>
        public AppServiceConnection Connection
        {
            get { lock (_connectionLock) { return _connection; } }
            set
            {
                lock (_connectionLock)
                {

                    try
                    {
                        _log.Information($"AppConnection.Connection.set enter: {((null != _connection) ? "has a connection" : "is disconnected")} and Status : {Status.ToString()}");
                        if (_connection != null)
                        {
                            _connection.RequestReceived -= RequestReceived;
                            _connection.ServiceClosed -= ServiceClosed;
                            _connection.Dispose();
                            Status = AppServiceConnectionStatus.Unknown;
                        }
                        _connection = value;
                        if (_connection != null)
                        {
                            Status = AppServiceConnectionStatus.Success;
                            _connection.ServiceClosed += ServiceClosed;
                            _connection.RequestReceived += RequestReceived;
                        }
                        _log.Information($"AppConnection.Connection.set exit: {((null != _connection) ? "has a Connection" : "is disconnected")} and Status : {Status.ToString()}");

                    }
                    catch (Exception ex)
                    {
                        _log.Error($"Error setting Connection : {ex.Message}");
                        throw;
                    }                }
            }
        }

        /// <summary>
        /// The connection status
        /// </summary>
        public AppServiceConnectionStatus Status
        {
            get;
            private set;
        }

        /// <summary>
        /// Test if the connection is actually connected
        /// </summary>
        /// <returns></returns>
        public bool IsValid()
        {
            try
            {
                _log.Information($"AppConnection.IsValid: {((null != Connection) ? "is Connected" : "is Not connected")} and Status : {Status.ToString()}");
                return (null != Connection) && (Status == AppServiceConnectionStatus.Success);
            }
            catch(Exception ex)
            {
                _log.Error($"Exception getting status: {ex.Message}");
            }
            return false;
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public FlowerConnection() : this("AppConnection")
        {
        }
        /// <summary>
        /// Initializing constructor
        /// </summary>
        /// <param name="logProviderName">The name of the log provider to use for this connection</param>
        public FlowerConnection(string logProviderName)
        {
            _log = new Logger(logProviderName);
            _connection = null;
            Status = AppServiceConnectionStatus.Unknown;
        }
        /// <summary>
        /// Destructor
        /// </summary>
        ~FlowerConnection()
        {
            Dispose(true);
        }

        #region IDisposable Members

        /// <summary>
        /// Internal variable which checks if Dispose has already been called
        /// </summary>
        private Boolean disposed;

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        private void Dispose(Boolean disposing)
        {
            if (disposed)
            {
                return;
            }

            _log.Information("AppConnection is Disposed");
            if (disposing)
            {
                if(Connection != null)
                {
                    Connection.Dispose();
                }
                Connection = null;
                if (_log != null)
                {
                    _log.Dispose();
                }
                _log = null;
            }
            disposed = true;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            // Call the private Dispose(bool) helper and indicate 
            // that we are explicitly disposing
            this.Dispose(true);

            // Tell the garbage collector that the object doesn't require any
            // cleanup when collected since Dispose was called explicitly.
            GC.SuppressFinalize(this);
        }

        #endregion

        /// <summary>
        /// Open the connection to the App service
        /// </summary>
        /// <returns>The AppServiceConnectionStatus object giving the status of the connection</returns>
        public IAsyncOperation<AppServiceConnectionStatus> OpenConnectionAsync()
        {
            _log.Information("OpenConnectionAsync: called");
            return Task<AppServiceConnectionStatus>.Run(async () =>
            {
                _log.Information("OpenConnectionAsync: Creating App Service Connection");
                AppServiceConnection connection = new AppServiceConnection();

                // Here, we use the app service name defined in the app service provider's Package.appxmanifest file in the <Extension> section.
                connection.AppServiceName = ServiceName;

                // Use Windows.ApplicationModel.Package.Current.Id.FamilyName within the app service provider to get this value.
                connection.PackageFamilyName = ServiceFamilyName;

                Status = await connection.OpenAsync();
                bool bRet = Status == AppServiceConnectionStatus.Success;
                _log.Information($"OpenConnectionAsync: Connection Status = {Status.ToString()}");

                if (bRet)
                {
                    Connection = connection;
                }
                return Status;
            }).AsAsyncOperation<AppServiceConnectionStatus>();
        }

        /// <summary>
        /// Send a Command message to the head application
        /// </summary>
        /// <param name="command">The command to send</param>
        /// <returns>The AppMessage containing the response</returns>
        public IAsyncOperation<AppMessage> SendCommandAsync(AppMessage command)
        {

            return Task<AppMessage>.Run(async () =>
           {
               AppServiceResponse response = null;
               AppMessage retCommand;
               if (IsValid())
               {
                   _log.Information($"SendCommandAsync: Sending {command.ToString()}");
                   response = await Connection.SendMessageAsync(command.ToValueSet());
                   if(response.Status != AppServiceResponseStatus.Success)
                   {
                       retCommand = new AppMessage(
                                                    AppMessage.CommandType.Error,
                                                    $"Command response status: {response.Status} with message: {ValueSetOut.ToString(response.Message)}"
                                                    );
                       _log.Error(retCommand.Param.ToString());  
                   }
                   else
                   {
                       retCommand = AppMessage.FromValueSet(response.Message);
                   }

               }
               else
               {
                   _log.Error("SendCommandAsync: called before the connection is valid");
                   throw new InvalidOperationException("Cannot send a command until connection is opened");
               }
               return retCommand;
           }
            ).AsAsyncOperation<AppMessage>();
        }


        /// <summary>
        /// Receive messages from the other app
        /// </summary>
        /// <param name="sender">The connection the message is from</param>
        /// <param name="args">The arguments for the message</param>
        private async void RequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            if(!IsValid())
            {
                _log.Information("AppConnection.RequestReceived: called before the connection is valid");
                throw new InvalidOperationException("Message received before connection is opened");
            }
            var requestDefferal = args.GetDeferral();
            try
            {

                _log.Information($"AppConnection.RequestReceived: received the following message: {ValueSetOut.ToString(args.Request.Message)}");
                AppServiceResponseStatus status = AppServiceResponseStatus.Unknown;
                if (MessageReceived != null)
                {
                    AppMessage lc = AppMessage.FromValueSet(args.Request.Message);
                    AppMessage result = MessageReceived(lc);
                    _log.Information($"AppConnection.RequestRecieved: response: {result.ToString()}");
                    status = await args.Request.SendResponseAsync(result.ToValueSet());
                }

                _log.Information($"AppConnection.RequestReceived: Response to Request returned: {status.ToString()}");
            }
            finally
            {
                requestDefferal.Complete();
            }
        }
        /// <summary>
        /// Handler for the Service connection closed event from the AppServiceConnection
        /// </summary>
        /// <param name="sender">The AppConnection that is closing (i.e. this pointer)</param>
        /// <param name="args">any arguments for the service closure (unused)</param>
        private void ServiceClosed(AppServiceConnection sender, AppServiceClosedEventArgs args)
        {
            _log.Information($"AppService Connection has be closed by: {args.Status.ToString()}");
            Connection = null;
            Status = AppServiceConnectionStatus.Unknown;
        }

    }
}
