﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ALE_Rotorgun_Detection {

    public class CurrentCooldown {

        private long _startTime;
        private long _currentCooldown;

        private string command;

        public CurrentCooldown(long cooldown) {
            _currentCooldown = cooldown;
        }

        public void startCooldown(string command) {
            this.command = command;
            this._startTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        public long getRemainingSeconds(string command) {

            if (this.command != command)
                return 0;

            long elapsedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - _startTime;

            if (elapsedTime >= _currentCooldown)
                return 0;

            return (_currentCooldown - elapsedTime) / 1000;
        }
    }
}
