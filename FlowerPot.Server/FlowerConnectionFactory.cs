using System;
using FlowerPot.Connection;

namespace FlowerPot.Server
{
    internal class FlowerConnectionFactory
    {
        private static readonly Lazy<FlowerConnection> _instance = new Lazy<FlowerConnection>(() => new FlowerConnection("FlowerPot.Server.ConnectionFactory"));
        internal static FlowerConnection Instance
        {
            get { return _instance.Value; }
        }
        internal static bool IsValid
        {
            get { return _instance.IsValueCreated && _instance.Value.Connection != null; }
        }
    }
}