using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FlowerPot.Connection;
using Microsoft.AspNetCore.Mvc;

namespace FlowerPot.Server
{
    public class FlowerPotController : ControllerBase
    {
       Logging.Logger _log = new Logging.Logger("FowerPotController");

        private IGetResponse SendAppCommand(AppMessage.CommandType command, string param = "")
        {
            AppMessage lc = new AppMessage(command, param);
            AppMessage retCommand = new AppMessage(AppMessage.CommandType.Error, "AppConnection is invalid");
            _log.Information($"Sending command {lc.ToString()}");
            if (AppConnectionFactory.IsValid)
            {
                retCommand = AppConnectionFactory.Instance.SendCommandAsync(lc).GetAwaiter().GetResult();
            }

            var response = new GetResponse(GetResponse.ResponseStatus.OK, retCommand);
            _log.Information($"Command responding with: {response.ToString()}");
            return response;
        }

        [UriFormat("/play")]
        public IGetResponse PlayCommand()
        {
            return SendAppCommand(AppMessage.CommandType.Play);
        }

        [UriFormat("/stop")]
        public IGetResponse StopCommand()
        {
            return SendAppCommand(AppMessage.CommandType.Stop);
        }

        [UriFormat("/state")]
        public IGetResponse GetPlayerStatus()
        {
            return SendAppCommand(AppMessage.CommandType.State);
        }
    }
    }
}
