using ALE_Core.Cooldown;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Torch;
using Torch.API;
using Torch.API.Plugins;

namespace ALE_Rotorgun_Detection
{
    public class RotorgunDetectorPlugin : TorchPluginBase, IWpfPlugin {

        public static readonly Logger Log = LogManager.GetCurrentClassLogger();
        public static RotorgunDetectorPlugin Instance { get; private set; }

        public CooldownManager DetachCooldowns { get; } = new CooldownManager();
        public CooldownManager LoggingCooldowns { get; } = new CooldownManager();

        private Persistent<RotorgunDetectorConfig> _config;
        public RotorgunDetectorConfig Config => _config?.Data;

        public void Save() => _config.Save();

        public long DetachCooldown { get { return Config.DetachCooldown * 1000; } }
        public long LoggingCooldown { get { return Config.LoggingCooldown * 1000; } }
        public int MinRotorGridCount { get { return Config.MinRotorGridCount; } }

        private Control _control;
        public UserControl GetControl() => _control ?? (_control = new Control(this));

        public override void Init(ITorchBase torch) {

            base.Init(torch);

            var configFile = Path.Combine(StoragePath, "RotorgunDetector.cfg");

            try {

                _config = Persistent<RotorgunDetectorConfig>.Load(configFile);

            } catch (Exception e) {
                Log.Warn(e);
            }

            if (_config?.Data == null) {

                Log.Info("Create Default Config, because none was found!");

                _config = new Persistent<RotorgunDetectorConfig>(configFile, new RotorgunDetectorConfig());
                _config.Save();
            }

            Instance = this;
        }
    }
}
