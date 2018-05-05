using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlowerPot.Connection;

namespace FlowerPot.Server
{
    internal class PotConnection
    {
        private static readonly Lazy<FlowerConnection> _instance = new Lazy<FlowerConnection>(() => new FlowerConnection("PotConnection"));
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
