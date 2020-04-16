using ALE_Core.Cooldown;
using ALE_Core.Utils;
using NLog;
using NLog.Config;
using NLog.Targets;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Cube;
using System;
using System.Reflection;
using Torch.API.Session;
using Torch.Managers.PatchManager;
using Torch.Utils;

namespace ALE_Rotorgun_Detection.Patch {

    [PatchShim]
    public static class MyMechanicalConnectionBlockBasePatch {

        public static readonly Logger Log = LogManager.GetCurrentClassLogger();
        public static readonly Logger FILE_LOGGER = LogManager.GetLogger("RotorgunDetectorPlugin");

        [ReflectedMethodInfo(typeof(MyMechanicalConnectionBlockBasePatch), "DetachDetection")]
        private static readonly MethodInfo detachDetection;

        static MyMechanicalConnectionBlockBasePatch() {

            var logTarget = new FileTarget {
                FileName = "Logs/rotorguns-${shortdate}.log",
                Layout = "${var:logStamp} ${var:logContent}"
            };

            LogManager.Configuration.AddTarget("rotorguns", logTarget);

            var logRule = new LoggingRule("RotorgunDetectorPlugin", LogLevel.Debug, logTarget) {
                Final = true
            };

            LogManager.Configuration.LoggingRules.Insert(0, logRule);

            LogManager.Configuration.Reload();
        }

        public static void Patch(PatchContext ctx) {

            MethodInfo detach = typeof(MyMechanicalConnectionBlockBase).GetMethod("CreateTopPartAndAttach", BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[] { typeof(long), typeof(bool), typeof(bool) }, null);

            ctx.GetPattern(detach).Prefixes.Add(detachDetection);

            Log.Debug("Patched MyMotorStator!");
        }

        public static bool DetachDetection(MyMechanicalConnectionBlockBase __instance) {
            
            if (!(__instance is MyMotorBase motor))
                return true;

            var session = RotorgunDetectorPlugin.Instance.Torch.CurrentSession;
            if (session == null || session.State != TorchSessionState.Loaded)
                return true;

            RotorgunDetectorPlugin plugin = RotorgunDetectorPlugin.Instance;

            var grid = motor.CubeGrid;

            if (!IsPossibleRotorgun(grid, plugin.MinRotorGridCount))
                return true;

            var cooldowns = plugin.DetachCooldowns;
            var key = new EntityIdCooldownKey(grid.EntityId);

            if(!cooldowns.CheckCooldown(key, "detach", out long remainingSeconds)) {

                long ownerId = motor.OwnerId;

                if (ownerId != 0)
                    MyVisualScriptLogicProvider.SendChatMessage("Rotor Head cannot be placed for an other " + remainingSeconds + " seconds.", "Server", ownerId, "Red");

                DoLogging(key, grid);

                return false;
            }

            cooldowns.StartCooldown(key, "detach", plugin.DetachCooldown);

            return true;
        }

        private static bool IsPossibleRotorgun(MyCubeGrid grid, int minRotorCount) {
            return Commands.CheckGroup(out _, MyCubeGridGroups.Static.Physical.GetGroup(grid)) >= minRotorCount;
        }

        private static void DoLogging(EntityIdCooldownKey cooldownKey, MyCubeGrid grid) {

            RotorgunDetectorPlugin plugin = RotorgunDetectorPlugin.Instance;

            var cooldowns = plugin.LoggingCooldowns;

            if (!cooldowns.CheckCooldown(cooldownKey, "logging", out _)) 
                return;

            cooldowns.StartCooldown(cooldownKey, "logging", plugin.LoggingCooldown);

            grid = grid.GetBiggestGridInGroup();

            var gridOwnerList = grid.BigOwners;
            var ownerCnt = gridOwnerList.Count;
            var gridOwner = 0L;

            if (ownerCnt > 0 && gridOwnerList[0] != 0)
                gridOwner = gridOwnerList[0];
            else if (ownerCnt > 1)
                gridOwner = gridOwnerList[1];

            FILE_LOGGER.Warn("Possible Rotorgun found on grid " + grid.DisplayName + " owned by " + PlayerUtils.GetPlayerNameById(gridOwner));
        }
    }
}
