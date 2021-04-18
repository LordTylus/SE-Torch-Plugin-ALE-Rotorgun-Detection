using ALE_Core.Utils;
using ALE_Rotorgun_Detection.Patch;
using NLog;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Text;
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

            List<string> args = Context.Args;

            bool gps = false;

            for (int i = 0; i < args.Count; i++) {

                if (args[i] == "-gps")
                    gps = true;
            }

            StringBuilder sb = new StringBuilder();

            foreach (var group in MyCubeGridGroups.Static.Physical.Groups) {

                int gridsWithRotorCount = CheckGroup(out MyCubeGrid biggestGrid, group);

                if (biggestGrid != null && gridsWithRotorCount >= Plugin.MinRotorGridCount) {

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

                    if (gps && Context.Player != null) {

                        var gridGPS = MyAPIGateway.Session?.GPS.Create("--" + biggestGrid.DisplayName, ($"{biggestGrid.DisplayName} - {biggestGrid.GridSizeEnum} - {biggestGrid.BlocksCount} blocks"), position, true);

                        MyAPIGateway.Session?.GPS.AddGps(Context.Player.IdentityId, gridGPS);
                    }
                }
            }

            if (Context.Player == null) {

                Context.Respond($"Potential Rotorguns");
                Context.Respond(sb.ToString());

            } else {

                ModCommunication.SendMessageTo(new DialogMessage("Potential Rotorguns", $"At least " + Plugin.MinRotorGridCount + " rotors on different subgrids.", sb.ToString()), Context.Player.SteamUserId);
            }
        }

        public static int CheckGroup(out MyCubeGrid biggestGrid, MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group group) {

            biggestGrid = null;

            try { 

                double num = 0.0;

                Dictionary<MyCubeGrid, List<TopPart>> connectionMap = new Dictionary<MyCubeGrid, List<TopPart>>();

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

                        if (cubeBlock is MyMotorStator rotor) {

                            if (MyMechanicalConnectionBlockBasePatch.IsIrrelevantType(rotor))
                                continue;

                            MyCubeGrid top = rotor.TopGrid;
                            MyCubeGrid bottom = cubeGrid;

                            List<TopPart> connections;

                            if (top != null && !connectionMap.ContainsKey(top))
                                connectionMap.Add(top, new List<TopPart>());

                            if (!connectionMap.ContainsKey(bottom)) {

                                connections = new List<TopPart>();
                                connectionMap.Add(bottom, connections);

                            } else {

                                connections = connectionMap[bottom];
                            }

                            connections.Add(new TopPart(top));
                        }
                    }
                }

                int maxCount = 0;

                foreach (MyCubeGrid grid in new List<MyCubeGrid>(connectionMap.Keys))
                    maxCount = Math.Max(maxCount, GetMaxTiefe(grid, connectionMap));

                return maxCount;

            } catch(Exception e) {
                Log.Error(e, "Error while checking grid!");
                return 0;
            }
        }

        private static int GetMaxTiefe(MyCubeGrid grid, Dictionary<MyCubeGrid, List<TopPart>> connectionMap) {

            int count = 1;

            if (!connectionMap.ContainsKey(grid))
                return 0;

            List<TopPart> connections = connectionMap[grid];
            connectionMap.Remove(grid);

            int childCount = 0;

            foreach (TopPart child in connections) {

                if(child.top == null) {
                    childCount = Math.Max(childCount, 1);
                    continue;
                }

                childCount = Math.Max(childCount, GetMaxTiefe(child.top, connectionMap));
            }
                
            return count + childCount;
        }

        private class TopPart {

            public readonly MyCubeGrid top;

            public TopPart(MyCubeGrid top) {
                this.top = top;
            }
        }
    }
}
