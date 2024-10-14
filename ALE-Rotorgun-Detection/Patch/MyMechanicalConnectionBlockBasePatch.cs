using ALE_Core.Cooldown;
using ALE_Core.Utils;
using NLog;
using NLog.Config;
using NLog.Targets;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
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

        public static void ApplyLogging() {

            var rules = LogManager.Configuration.LoggingRules;

            for (int i = rules.Count - 1; i >= 0; i--) {

                var rule = rules[i];

                if (rule.LoggerNamePattern == "RotorgunDetectorPlugin")
                    rules.RemoveAt(i);
            }

            var config = RotorgunDetectorPlugin.Instance.Config;

            var logTarget = new FileTarget {
                FileName = "Logs/" + config.LoggingFileName,
                Layout = "${var:logStamp} ${var:logContent}"
            };

            var logRule = new LoggingRule("RotorgunDetectorPlugin", LogLevel.Debug, logTarget) {
                Final = true
            };

            rules.Insert(0, logRule);

            LogManager.Configuration.Reload();
        }

        public static void Patch(PatchContext ctx) {

            MethodInfo detach = typeof(MyMechanicalConnectionBlockBase).GetMethod("CreateTopPartAndAttach", 
                BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[] { typeof(long), typeof(MyMechanicalConnectionBlockBase.MyTopBlockSize), typeof(bool) }, null);

            ctx.GetPattern(detach).Prefixes.Add(detachDetection);

            Log.Debug("Patched MyMotorStator!");
        }

        public static bool IsIrrelevantType(MyMechanicalConnectionBlockBase block) {

            if (!(block is MyMotorStator stator))
                return true;

            if(stator.BlockDefinition is MyMotorStatorDefinition definition)
                if (definition.RotorType != MyRotorType.Rotor)
                    return true;

            return false;
        }

        public static bool DetachDetection(MyMechanicalConnectionBlockBase __instance) {
            
            if (IsIrrelevantType(__instance))
                return true;

            var motor = __instance as MyMotorBase;

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
