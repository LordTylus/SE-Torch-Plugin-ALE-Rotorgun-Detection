using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torch;
using Torch.API;
using VRage.Scripting;

namespace ALE_Rotorgun_Detection
{
    public class RotorgunDetectorPlugin : TorchPluginBase {

        public static readonly Logger Log = LogManager.GetCurrentClassLogger();
        public static RotorgunDetectorPlugin Instance { get; private set; }

        private ConcurrentDictionary<long, CurrentCooldown> _detachCooldowns = new ConcurrentDictionary<long, CurrentCooldown>();
        private ConcurrentDictionary<long, CurrentCooldown> _loggingCooldowns = new ConcurrentDictionary<long, CurrentCooldown>();

        public ConcurrentDictionary<long, CurrentCooldown> DetachCooldowns { get { return _detachCooldowns; } }
        public ConcurrentDictionary<long, CurrentCooldown> LoggingCooldowns { get { return _loggingCooldowns; } }

        public long DetachCooldown { get { return 300 * 1000; } }
        public long LoggingCooldown { get { return 10 * 1000; } }
        public int MinRotorGridCount { get { return 4; } }

        public override void Init(ITorchBase torch) {
            base.Init(torch);

            var pgmr = new RotorgunDetectorManager(torch);
            torch.Managers.AddManager(pgmr);

            Instance = this;
        }
    }
}
