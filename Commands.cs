using ALE_PcuTransferrer.Utils;
using NLog;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torch.Commands;
using Torch.Commands.Permissions;
using Torch.Mod;
using Torch.Mod.Messages;
using VRage.Game.ModAPI;
using VRage.Groups;

namespace ALE_Rotorgun_Detection {

    public class Commands : CommandModule {

        public static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public RotorgunDetectorPlugin Plugin => (RotorgunDetectorPlugin)Context.Plugin;

        [Command("findrotorgun", "Looks for rotorguns on the server!")]
        [Permission(MyPromoteLevel.Moderator)]
        public void DetectRotorguns() {

            StringBuilder sb = new StringBuilder();

            foreach (var group in MyCubeGridGroups.Static.Physical.Groups) {

                MyCubeGrid biggestGrid = null;

                int gridsWithRotorCount = checkGroup(out biggestGrid, group);

                if(gridsWithRotorCount >= Plugin.MinRotorGridCount) {

                    var gridOwnerList = biggestGrid.BigOwners;
                    var ownerCnt = gridOwnerList.Count;
                    var gridOwner = 0L;

                    if (ownerCnt > 0 && gridOwnerList[0] != 0)
                        gridOwner = gridOwnerList[0];
                    else if (ownerCnt > 1)
                        gridOwner = gridOwnerList[1];

                    var position = biggestGrid.PositionComp.GetPosition();

                    sb.AppendLine($"{biggestGrid.DisplayName}");
                    sb.AppendLine($"   Owned by {PlayerUtils.GetPlayerNameById(gridOwner)}");
                    sb.AppendLine($"   Location: X: {position.X.ToString("#,##0.00")}, Y: {position.Y.ToString("#,##0.00")}, Z: {position.Z.ToString("#,##0.00")}");
                }
            }

            if (Context.Player == null) {

                Context.Respond($"Potential Rotorguns");
                Context.Respond(sb.ToString());

            } else {

                ModCommunication.SendMessageTo(new DialogMessage("Potential Rotorguns", $"At least 3 rotors on different subgrids.", sb.ToString()), Context.Player.SteamUserId);
            }
        }

        public static int checkGroup(out MyCubeGrid biggestGrid, MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group group) {

            HashSet<long> gridIdsWithRotor = new HashSet<long>();

            double num = 0.0;
            biggestGrid = null;

            foreach (MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Node groupNodes in group.Nodes) {

                MyCubeGrid cubeGrid = groupNodes.NodeData;

                if (cubeGrid == null)
                    continue;

                if (cubeGrid.Physics == null)
                    continue;

                double volume = cubeGrid.PositionComp.WorldAABB.Size.Volume;
                if (volume > num) {
                    num = volume;
                    biggestGrid = cubeGrid;
                }

                HashSet<MySlimBlock> blocks = new HashSet<MySlimBlock>(cubeGrid.GetBlocks());
                foreach (MySlimBlock block in blocks) {

                    if (block == null || block.CubeGrid == null || block.IsDestroyed)
                        continue;

                    MyCubeBlock cubeBlock = block.FatBlock;

                    if (cubeBlock == null)
                        continue;

                    MyMotorStator rotor = cubeBlock as MyMotorStator;

                    if (rotor != null) {
                        gridIdsWithRotor.Add(cubeGrid.EntityId);
                        break;
                    }
                }
            }

            return gridIdsWithRotor.Count;
        }
    }
}
