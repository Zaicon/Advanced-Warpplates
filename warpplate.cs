/*
 * "Created by DarkunderdoG, modified by 2.0"
 * 
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace PluginTemplate
{
    [ApiVersion(1, 24)]
    public class WarpplatePlugin : TerrariaPlugin
    {
        public static List<Player> Players = new List<Player>();
        public static WarpplateManager Warpplates;

        public override string Name
        {
            get { return "Warpplate"; }
        }
        public override string Author
        {
            get { return "Maintained by Zaicon"; }
        }
        public override string Description
        {
            get { return "Warpplate"; }
        }
        public override Version Version
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version; }
        }

        public override void Initialize()
        {
            Warpplates = new WarpplateManager(TShock.DB);
            ServerApi.Hooks.GamePostInitialize.Register(this, OnPostInit);
            ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);
            ServerApi.Hooks.GameUpdate.Register(this, OnUpdate);
            ServerApi.Hooks.NetGreetPlayer.Register(this, OnGreetPlayer);
            ServerApi.Hooks.ServerLeave.Register(this, OnLeave);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.GamePostInitialize.Deregister(this, OnPostInit);
                ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
                ServerApi.Hooks.GameUpdate.Deregister(this, OnUpdate);
                ServerApi.Hooks.NetGreetPlayer.Deregister(this, OnGreetPlayer);
                ServerApi.Hooks.ServerLeave.Deregister(this, OnLeave);
            }

            base.Dispose(disposing);
        }

        private void OnPostInit(EventArgs args)
        {
            Warpplates.ReloadAllWarpplates();
        }

        public WarpplatePlugin(Main game)
            : base(game)
        {
            Order = 1;
        }

        public void OnInitialize(EventArgs args)
        {
            Commands.ChatCommands.Add(new Command("warpplate.set", setwarpplate, "swp"));
            Commands.ChatCommands.Add(new Command("warpplate.set", delwarpplate, "dwp"));
            Commands.ChatCommands.Add(new Command("warpplate.set", warpplatedest, "swpd"));
            Commands.ChatCommands.Add(new Command("warpplate.set", removeplatedest, "rwpd"));
            Commands.ChatCommands.Add(new Command("warpplate.set", wpi, "wpi"));
            Commands.ChatCommands.Add(new Command("warpplate.use", warpallow, "wpa"));
            Commands.ChatCommands.Add(new Command("warpplate.set", reloadwarp, "rwp"));
            Commands.ChatCommands.Add(new Command("warpplate.set", setwarpplatedelay, "swpdl"));
            Commands.ChatCommands.Add(new Command("warpplate.set", setwarpplatewidth, "swpw"));
            Commands.ChatCommands.Add(new Command("warpplate.set", setwarpplateheight, "swph"));
            Commands.ChatCommands.Add(new Command("warpplate.set", setwarpplatesize, "swps"));
            Commands.ChatCommands.Add(new Command("warpplate.set", setwarpplatelabel, "swpl"));
        }
        
        public void OnGreetPlayer(GreetPlayerEventArgs args)
        {
            lock (Players)
                Players.Add(new Player(args.Who));
        }

        public class Player
        {
            public int Index { get; set; }
            public TSPlayer TSPlayer { get { return TShock.Players[Index]; } }
            public int warpplatetime { get; set; }
            public bool warpplateuse { get; set; }
            public bool warped { get; set; }
            public int warpcooldown { get; set; }
            public Player(int index)
            {
                Index = index;
                warpplatetime = 0;
                warpplateuse = true;
                warped = false;
                warpcooldown = 0;
            }
        }

        private DateTime LastCheck = DateTime.UtcNow;

        private void OnUpdate(EventArgs args)
        {
            if ((DateTime.UtcNow - LastCheck).TotalSeconds >= 1)
            {
                LastCheck = DateTime.UtcNow;
                lock (Players)
                    foreach (Player player in Players)
                    {
                        if (player != null && player.TSPlayer != null)
                        {
                            if (player.TSPlayer.Group.HasPermission("warpplate.use") && player.warpplateuse)
                            {
                                if (player.warpcooldown != 0)
                                {
                                    player.warpcooldown--;
                                    continue;
                                }
                                string region = Warpplates.InAreaWarpplateName(player.TSPlayer.TileX, player.TSPlayer.TileY);
                                if (region == null || region == "")
                                {
                                    player.warpplatetime = 0;
                                    player.warped = false;
                                }
                                else
                                {
                                    if (player.warped)
                                        continue;
                                    var warpplateinfo = Warpplates.FindWarpplate(region);
                                    var warp = Warpplates.FindWarpplate(warpplateinfo.WarpDest);
                                    if (warp != null)
                                    {
                                        player.warpplatetime++;
                                        if ((warpplateinfo.Delay - player.warpplatetime) > 0)
                                            player.TSPlayer.SendInfoMessage("You will be warped to " + Warpplates.GetLabel(warpplateinfo.WarpDest) + " in " + (warpplateinfo.Delay - player.warpplatetime) + " seconds");
                                        else
                                        {
                                            if (player.TSPlayer.Teleport((int)(warp.WarpplatePos.X * 16) + 2, (int)(warp.WarpplatePos.Y*16) + 3))
                                                player.TSPlayer.SendInfoMessage("You have been warped to " + Warpplates.GetLabel(warpplateinfo.WarpDest) + " via a Warpplate");
                                            player.warpplatetime = 0;
                                            player.warped = true;
                                            player.warpcooldown = 3;
                                        }                                        
                                    }
                                }
                            }
                        }
                    }
            }
        }

        private void OnLeave(LeaveEventArgs args)
        {
            lock (Players)
            {
                for (int i = 0; i < Players.Count; i++)
                {
                    if (Players[i].Index == args.Who)
                    {
                        Players.RemoveAt(i);
                        break; //Found the player, break.
                    }
                }
            }
        }

        private static int GetPlayerIndex(int ply)
        {
            lock (Players)
            {
                int index = -1;
                for (int i = 0; i < Players.Count; i++)
                {
                    if (Players[i].Index == ply)
                        index = i;
                }
                return index;
            }
        }

        private static void setwarpplatedelay(CommandArgs args)
        {
            string region = "";
            if (args.Parameters.Count == 2)
                region = args.Parameters[0];
            else if (args.Parameters.Count == 1)
                region = Warpplates.InAreaWarpplateName(args.Player.TileX, args.Player.TileY);
            else {
                args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /swpdl [<warpplate name>] <delay in seconds>");
                args.Player.SendErrorMessage("Set 0 for immediate warp");
                return;
            }
            Warpplate wp = Warpplates.FindWarpplate(region);
            if (wp == null)
            {
                args.Player.SendErrorMessage("No such warpplate");
                return;
            }
            int Delay;
            if (Int32.TryParse(args.Parameters[args.Parameters.Count - 1], out Delay))
            {
                wp.Delay = Delay + 1;
                if (Warpplates.UpdateWarpplate(wp.Name))
                    args.Player.SendSuccessMessage("Set delay of {0} to {1} seconds", wp.Name, Delay);
                else
                    args.Player.SendErrorMessage("Something went wrong");
            }
            else
                args.Player.SendErrorMessage("Bad number specified");
        }

        private static void setwarpplatewidth(CommandArgs args)
        {
            string region = "";
            if (args.Parameters.Count == 2)
                region = args.Parameters[0];
            else if (args.Parameters.Count == 1)
                region = Warpplates.InAreaWarpplateName(args.Player.TileX, args.Player.TileY);
            else
            {
                args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /swpw [<warpplate name>] <width in blocks>");
                return;
            }
            Warpplate wp = Warpplates.FindWarpplate(region);
            if (wp == null)
            {
                args.Player.SendErrorMessage("No such warpplate");
                return;
            }
            int Width;
            if (Int32.TryParse(args.Parameters[args.Parameters.Count - 1], out Width))
            {
                Rectangle r;
                r = wp.Area;
                r.Width = Width; 
                wp.Area = r;
                if (Warpplates.UpdateWarpplate(wp.Name))
                    args.Player.SendSuccessMessage("Set width of {0} to {1} blocks", wp.Name, Width);
                else
                    args.Player.SendErrorMessage("Something went wrong");
            }
            else
                args.Player.SendErrorMessage("Invalid number: " + args.Parameters[args.Parameters.Count - 1]);
        }

        private static void setwarpplatelabel(CommandArgs args)
        {
            string region = "";
            if (args.Parameters.Count == 2)
                region = args.Parameters[0];
            else if (args.Parameters.Count == 1)
                region = Warpplates.InAreaWarpplateName(args.Player.TileX, args.Player.TileY);
            else
            {
                args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /swpl [<warpplate name>] <label>");
				args.Player.SendErrorMessage("Type /swpl [<warpplate name>] \"\" to set label to default (warpplate name)");
                return;
            }
            Warpplate wp = Warpplates.FindWarpplate(region);
            if (wp == null)
            {
                args.Player.SendErrorMessage("No such warpplate");
                return;
            }
            string label = args.Parameters[args.Parameters.Count - 1];
            wp.Label = label;
            if (Warpplates.UpdateWarpplate(wp.Name))
                args.Player.SendSuccessMessage(String.Format("Set label of {0} to {1}", wp.Name, D(wp)));
            else
                args.Player.SendErrorMessage("Something went wrong");
        }

        private static void setwarpplateheight(CommandArgs args)
        {
            string region = "";
            if (args.Parameters.Count == 2)
                region = args.Parameters[0];
            else if (args.Parameters.Count == 1)
                region = Warpplates.InAreaWarpplateName(args.Player.TileX, args.Player.TileY);
            else
            {
                args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /swph [<warpplate name>] <height in blocks>");
                return;
            }
            Warpplate wp = Warpplates.FindWarpplate(region);
            if (wp == null)
            {
                args.Player.SendErrorMessage("No such warpplate");
                return;
            }
            int Height;
            if (Int32.TryParse(args.Parameters[args.Parameters.Count - 1], out Height))
            {
                Rectangle r;
                r = wp.Area;
                r.Height = Height;
                wp.Area = r;
                Warpplates.UpdateWarpplate(wp.Name);
                if (Warpplates.UpdateWarpplate(wp.Name))
                    args.Player.SendSuccessMessage("Set height of {0} to {1} blocks", wp.Name, Height);
                else
                    args.Player.SendErrorMessage("Something went wrong");
            }
            else
                args.Player.SendErrorMessage("Bad number specified");
        }

        private static void setwarpplatesize(CommandArgs args)
        {
            string region = "";
            if (args.Parameters.Count == 3)
                region = args.Parameters[0];
            else if (args.Parameters.Count == 2)
                region = Warpplates.InAreaWarpplateName(args.Player.TileX, args.Player.TileY);
            else
            {
                args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /swps [<warpplate name>] <width> <height>");
                return;
            }
            Warpplate wp = Warpplates.FindWarpplate(region);
            if (wp == null)
            {
                args.Player.SendErrorMessage("No such warpplate");
                return;
            }
            int Width, Height;
            if (Int32.TryParse(args.Parameters[args.Parameters.Count - 2], out Width) &&
                Int32.TryParse(args.Parameters[args.Parameters.Count - 1], out Height))
            {
                Rectangle r;
                r = wp.Area;
                r.Width = Width;
                r.Height = Height;
                wp.Area = r;
                Warpplates.UpdateWarpplate(wp.Name);
                if (Warpplates.UpdateWarpplate(wp.Name))
                    args.Player.SendSuccessMessage("Set size of {0} to {1}x{2}", wp.Name, Width, Height);
                else
                    args.Player.SendErrorMessage("Something went wrong");
            }
            else
                args.Player.SendErrorMessage("Bad number specified");
        }

        private static void setwarpplate(CommandArgs args)
        {
            if (args.Parameters.Count < 1)
            {
                args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /swp <warpplate name>");
                return;
            }
            if (Warpplates.InAreaWarpplateName(args.Player.TileX, args.Player.TileY) != null)
            {
                args.Player.SendErrorMessage("There is already a Warpplate located here. Find a new place");
                return;
            }
            string regionName = String.Join(" ", args.Parameters);
            var x = ((((int)args.Player.X) / 16) - 1);
            var y = (((int)args.Player.Y) / 16);
            var width = 2;
            var height = 3;
            if (Warpplates.AddWarpplate(x, y, width, height, regionName, "", Main.worldID.ToString()))
            {
                args.Player.SendSuccessMessage("Warpplate created: " + regionName);
                //args.Player.SendMessage("Now Set The Warpplate Destination By Using /swpd", Color.Yellow);
                Warpplates.ReloadAllWarpplates();
            }
            else
            {
                args.Player.SendErrorMessage("Warpplate already created: " + regionName + " already exists");
            }
        }

        private static void delwarpplate(CommandArgs args)
        {
            if (args.Parameters.Count < 1)
            {
                args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /dwp <warpplate name>");
                return;
            }
            string regionName = String.Join(" ", args.Parameters);
            if (Warpplates.DeleteWarpplate(regionName))
            {
                args.Player.SendInfoMessage("Deleted Warpplate: " + regionName);
                Warpplates.ReloadAllWarpplates();
            }
            else
                args.Player.SendErrorMessage("Could not find specified Warpplate");
        }

        private static void warpplatedest(CommandArgs args)
        {
            if (args.Parameters.Count < 2)
            {
                args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /swpd <Warpplate Name> <Name Of Destination Warpplate>");
                return;
            }
            if (Warpplates.adddestination(args.Parameters[0], args.Parameters[1]))
            {
                args.Player.SendInfoMessage("Destination " + args.Parameters[1] + " added to Warpplate " + args.Parameters[0]);
                Warpplates.ReloadAllWarpplates();
            }
            else
                args.Player.SendErrorMessage("Could not find specified Warpplate or destination");
        }

        private static void removeplatedest(CommandArgs args)
        {
            if (args.Parameters.Count < 1)
            {
                args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /rwpd <Warpplate Name>");
                return;
            }
            if (Warpplates.removedestination(args.Parameters[0]))
            {
                args.Player.SendInfoMessage("Removed Destination From Warpplate " + args.Parameters[0]);
                Warpplates.ReloadAllWarpplates();
            }
            else
                args.Player.SendErrorMessage("Could not find specified Warpplate or destination");
        }

        private static string S(string s)
        {
            return String.IsNullOrEmpty(s) ? "(none)" : s;
        }

        private static string D(Warpplate wp)
        {
            return String.IsNullOrEmpty(wp.Label) ? wp.Name + " (default)" : wp.Label;
        }

        private static void wpi(CommandArgs args)
        {
            string region = "";
            if (args.Parameters.Count > 0)
                region = String.Join(" ", args.Parameters);
            else
                region = Warpplates.InAreaWarpplateName(args.Player.TileX, args.Player.TileY);
            var warpplateinfo = Warpplates.FindWarpplate(region);
            if (warpplateinfo == null)
                args.Player.SendErrorMessage("No such Warpplate");
            else
            {
                args.Player.SendMessage("Name: " + warpplateinfo.Name + "; Label: " + D(warpplateinfo) 
                    + "Destination: " + S(warpplateinfo.WarpDest) + ";", Color.HotPink);
                args.Player.SendMessage("X: " + warpplateinfo.WarpplatePos.X + "; Y: " + warpplateinfo.WarpplatePos.Y + 
                    "; W: " + warpplateinfo.Area.Width + "; H: " + warpplateinfo.Area.Height + "; Delay: " + (warpplateinfo.Delay - 1), Color.HotPink);
            }
        }

        private static void reloadwarp(CommandArgs args)
        {
            Warpplates.ReloadAllWarpplates();
        }

        private static void warpallow(CommandArgs args)
        {
            if (!Players[GetPlayerIndex(args.Player.Index)].warpplateuse)
                args.Player.SendSuccessMessage("Warpplates are now turned on for you");
            if (Players[GetPlayerIndex(args.Player.Index)].warpplateuse)
                args.Player.SendSuccessMessage("Warpplates are now turned off for you");
            Players[GetPlayerIndex(args.Player.Index)].warpplateuse = !Players[GetPlayerIndex(args.Player.Index)].warpplateuse;
        }

    }
}