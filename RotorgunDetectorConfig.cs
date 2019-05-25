using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torch;

namespace ALE_Rotorgun_Detection {
    public class RotorgunDetectorConfig : ViewModel {

        private int _detachCooldown = 60 * 5; //5 Minutes
        private int _loggingCooldown = 10; //10 Seconds
        private int _minRotorGridCount = 4; //4 Rotors

        public int DetachCooldown { get => _detachCooldown; set => SetValue(ref _detachCooldown, value); }

        public int LoggingCooldown { get => _loggingCooldown; set => SetValue(ref _loggingCooldown, value); }

        public int MinRotorGridCount { get => _minRotorGridCount; set => SetValue(ref _minRotorGridCount, value); }
    }
}
