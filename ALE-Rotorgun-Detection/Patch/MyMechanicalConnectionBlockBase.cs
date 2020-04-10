using ALE_PcuTransferrer.Utils;
using NLog;
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

        public static readonly Logger Log = LogManager.GetLogger("RotorgunDetectorPlugin");

        [ReflectedMethodInfo(typeof(MyMechanicalConnectionBlockBasePatch), "DetachDetection")]
        private static readonly MethodInfo detachDetection;

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

            var cooldowns = plugin.DetachCooldowns;

            var grid = motor.CubeGrid;

            if (!IsPossibleRotorgun(grid, plugin.MinRotorGridCount))
                return true;


            if (cooldowns.TryGetValue(grid.EntityId, out CurrentCooldown cooldown)) {

                long remainingSeconds = cooldown.getRemainingSeconds("detach");

                if (remainingSeconds != 0) {

                    long ownerId = motor.OwnerId;

                    if (ownerId != 0)
                        MyVisualScriptLogicProvider.SendChatMessage("Rotor Head cannot be placed for an other " + remainingSeconds + " seconds.", "Server", ownerId, "Red");

                    DoLogging(grid);

                    return false;
                }

                cooldown.startCooldown("detach");

            } else {

                cooldown = new CurrentCooldown(plugin.DetachCooldown);
                cooldowns.TryAdd(grid.EntityId, cooldown);

                cooldown.startCooldown("detach");
            }

            return true;
        }

        private static bool IsPossibleRotorgun(MyCubeGrid grid, int minRotorCount) {
            return Commands.checkGroup(out _, MyCubeGridGroups.Static.Physical.GetGroup(grid)) >= minRotorCount;
        }

        private static void DoLogging(MyCubeGrid grid) {

            RotorgunDetectorPlugin plugin = RotorgunDetectorPlugin.Instance;

            var cooldowns = plugin.LoggingCooldowns;

            if (cooldowns.TryGetValue(grid.EntityId, out CurrentCooldown cooldown)) {

                long remainingSeconds = cooldown.getRemainingSeconds("logging");

                if (remainingSeconds != 0) {
                    return;
                }

                cooldown.startCooldown("logging");

            } else {

                cooldown = new CurrentCooldown(plugin.LoggingCooldown);
                cooldowns.TryAdd(grid.EntityId, cooldown);

                cooldown.startCooldown("logging");
            }

            if (grid == null)
                return;

            grid = grid.GetBiggestGridInGroup();

            var gridOwnerList = grid.BigOwners;
            var ownerCnt = gridOwnerList.Count;
            var gridOwner = 0L;

            if (ownerCnt > 0 && gridOwnerList[0] != 0)
                gridOwner = gridOwnerList[0];
            else if (ownerCnt > 1)
                gridOwner = gridOwnerList[1];

            Log.Warn("Possible Rotorgun found on grid " + grid.DisplayName + " owned by " + PlayerUtils.GetPlayerNameById(gridOwner));
        }
    }
}
