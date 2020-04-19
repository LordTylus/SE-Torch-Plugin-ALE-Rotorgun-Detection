using ALE_Core.Cooldown;
using ALE_Rotorgun_Detection.Patch;
using NLog;
using System;
using System.IO;
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

        public long DetachCooldown { get { return Config.DetachCooldown * 1000; } }
        public long LoggingCooldown { get { return Config.LoggingCooldown * 1000; } }
        public int MinRotorGridCount { get { return Config.MinRotorGridCount; } }

        private Control _control;
        public UserControl GetControl() => _control ?? (_control = new Control(this));

        public override void Init(ITorchBase torch) {

            base.Init(torch);

            Instance = this;

            SetUpConfig();

            MyMechanicalConnectionBlockBasePatch.ApplyLogging();
        }

        public void Save() {
            _config.Save();
            MyMechanicalConnectionBlockBasePatch.ApplyLogging();
        }

        private void SetUpConfig() {

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
        }
    }
}
