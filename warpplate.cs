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
using TShockAPI.Hooks;

namespace AdvancedWarpplate
{
	[ApiVersion(1, 26)]
	public class WarpplatePlugin : TerrariaPlugin
	{
		#region Plugin Information
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
		#endregion

		#region Initialize/Dispose
		public override void Initialize()
		{
			ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);
			ServerApi.Hooks.GameUpdate.Register(this, OnUpdate);
			ServerApi.Hooks.NetGreetPlayer.Register(this, OnGreetPlayer);
			GeneralHooks.ReloadEvent += ReloadWarp;
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
				ServerApi.Hooks.GameUpdate.Deregister(this, OnUpdate);
				ServerApi.Hooks.NetGreetPlayer.Deregister(this, OnGreetPlayer);
				GeneralHooks.ReloadEvent -= ReloadWarp;
			}

			base.Dispose(disposing);
		}
		#endregion

		public WarpplatePlugin(Main game)
			: base(game)
		{
			Order = 1;
		}

		private const string dataString = "AdvancedWarpplates.PlayerInfo";
		private DateTime lastCheck = DateTime.UtcNow;

		#region Hooks
		public void OnInitialize(EventArgs args)
		{
			DB.Connect();

			Commands.ChatCommands.Add(new Command("warpplate.set", WarpplateCommands, "warpplate"));
			Commands.ChatCommands.Add(new Command("warpplate.use", ToggleWarping, "togglewarpplates"));
		}

		public void OnGreetPlayer(GreetPlayerEventArgs args)
		{
			TSPlayer player = TShock.Players[args.Who];

			//Ignore non-players
			if (player == null || !player.RealPlayer)
				return;

			player.SetData(dataString, new PlayerInfo());
		}

		private void OnUpdate(EventArgs args)
		{
			//Only update every second.
			if ((DateTime.UtcNow - lastCheck).TotalSeconds >= 1)
			{
				lastCheck = DateTime.UtcNow;

				foreach (TSPlayer player in TShock.Players)
				{
					if (player == null || !player.RealPlayer || !player.HasPermission("warpplate.use"))
						continue;

					PlayerInfo playerInfo = player.GetData<PlayerInfo>(dataString);

					//If player doesn't want to be warped, ignore this player
					if (!playerInfo.WarpingEnabled)
						continue;

					//If player last warped within 3 seconds, ignore this player
					if (playerInfo.Cooldown > 0)
					{
						playerInfo.Cooldown--;
						player.SetData(dataString, playerInfo);
						continue;
					}

					//If player is not near a warpplate, ignore this player
					Warpplate fromWarpPlate = Utils.GetNearbyWarpplates(player.TileX, player.TileY);
					if (fromWarpPlate == null)
						continue;

					//If player is near a warpplate but that warpplate has no destination, ignore this player
					if (string.IsNullOrEmpty(fromWarpPlate.DestinationWarpplate))
						continue;

					Warpplate toWarpPlate = Utils.GetWarpplateByName(fromWarpPlate.DestinationWarpplate);

					//If player is near a warpplate but hasn't been near it for Delay seconds, ignore this player
					if ((fromWarpPlate.Delay - playerInfo.TimeStandingOnWarp) > 0)
					{
						playerInfo.TimeStandingOnWarp++;
						player.SetData(dataString, playerInfo);
						continue;
					}

					//Finally, teleport player if all previous conditions were met
					if (player.Teleport((int)(toWarpPlate.Area.X * 16) + 2, (int)(toWarpPlate.Area.Y * 16) + 3))
						player.SendInfoMessage("You have been warped to " + toWarpPlate.Name + " via a warpplate");

					//Reset info
					playerInfo.TimeStandingOnWarp = 0;
					playerInfo.Cooldown = 3; //TODO: Config!
					player.SetData(dataString, playerInfo);
				}
			}
		}
		#endregion

		private void WarpplateCommands(CommandArgs args)
		{
			TSPlayer player = args.Player;

			if (args.Parameters.Count < 1 || args.Parameters.Count > 3)
			{
				sendInvalidSyntaxError();
				return;
			}

			string baseCmd = args.Parameters[0].ToLower();
			string specifier = args.Silent ? TShock.Config.CommandSilentSpecifier : TShock.Config.CommandSpecifier;

			switch (baseCmd)
			{
				//warpplate add <name>
				case "add":
					if (args.Parameters.Count < 2)
					{
						args.Player.SendErrorMessage($"Invalid syntax: {specifier}warpplate add <warpplate name>");
						return;
					}
					if (Utils.GetNearbyWarpplates(args.Player.TileX, args.Player.TileY) != null)
					{
						args.Player.SendErrorMessage($"There is already a warpplate here!");
						return;
					}
					Warpplate newWarpplate = new Warpplate(string.Join(" ", args.Parameters.GetRange(1, args.Parameters.Count - 1)), args.Player.TileX, args.Player.TileY);
					DB.AddWarpplate(newWarpplate);
					args.Player.SendSuccessMessage($"Added warpplate {string.Join(" ", args.Parameters.GetRange(1, args.Parameters.Count - 1))} at your current location.");
					break;

				//warpplate del <name>
				case "del":
				case "delete":
					if (args.Parameters.Count < 2)
					{
						args.Player.SendErrorMessage($"Invalid syntax: {specifier}warpplate del <warpplate name>");
						return;
					}
					Warpplate warpplate = Utils.GetWarpplateByName(string.Join(" ", args.Parameters.GetRange(1, args.Parameters.Count - 1)));
					if (warpplate == null)
					{
						args.Player.SendErrorMessage($"There is no warpplate by the name of {string.Join(" ", args.Parameters.GetRange(1, args.Parameters.Count - 1))}!");
						return;
					}
					DB.RemoveWarpplate(warpplate);
					args.Player.SendSuccessMessage($"Removed warpplate {string.Join(" ", args.Parameters.GetRange(1, args.Parameters.Count - 1))}.");
					break;

				//warpplate mod <name> <type> <value>
				case "mod":
				case "modify":
					if (args.Parameters.Count != 4)
					{
						args.Player.SendErrorMessage($"Invalid syntax: {specifier}warpplate mod <name> <type> <value>");
						return;
					}
					//Throwing this to separate method for cleaner code
					processModification();
					break;

				//warpplate info <name>
				case "info":
					if (args.Parameters.Count < 2)
					{
						args.Player.SendErrorMessage($"Invalid syntax: {specifier}warpplate del <warpplate name>");
						return;
					}
					Warpplate warpplateInfo = Utils.GetWarpplateByName(string.Join(" ", args.Parameters.GetRange(1, args.Parameters.Count - 1)));
					if (warpplateInfo == null)
					{
						args.Player.SendErrorMessage($"There is no warpplate by the name of {string.Join(" ", args.Parameters.GetRange(1, args.Parameters.Count - 1))}!");
						return;
					}
					args.Player.SendInfoMessage($"Warpplate Name: {warpplateInfo.Name} ({warpplateInfo.Area.X}, {warpplateInfo.Area.Y})");
					args.Player.SendInfoMessage($"Destination Warpplate Name: {warpplateInfo.DestinationWarpplate}");
					args.Player.SendInfoMessage($"Delay: {warpplateInfo.Delay} | Width: {warpplateInfo.Area.Width} | Height: {warpplateInfo.Area}");
					break;
				default:
					sendInvalidSyntaxError();
					break;
			}

			//Sends paginated list of commands.
			void sendInvalidSyntaxError()
			{
				args.Player.SendErrorMessage("Invalid syntax!");
				List<string> listOfData = new List<string>() {
					"/warpplate add <name>",
					"/warpplate del <name>",
					"/warpplate info <name>",
					"/warpplate mod <name> <type> <value> - Type '/warpplate help mod 1' for mod sub-commands."
					};
				PaginationTools.Settings settings = new PaginationTools.Settings()
				{
					HeaderFormat = "Warpplate Sub-Commands ({0}/{1}):",
					FooterFormat = $"Type {specifier}warpplate help {{0}} for more sub-commands."
				};

				if (args.Parameters.Count == 2 && args.Parameters[0].ToLower() == "help" && int.TryParse(args.Parameters[1], out int pageNumber))
				{
					PaginationTools.SendPage(args.Player, pageNumber, listOfData, settings);
					return;
				}
				else if (args.Parameters.Count == 3 && args.Parameters[0].ToLower() == "help" && args.Parameters[1].ToLower() == "mod" && int.TryParse(args.Parameters[2], out int pageNumber2))
				{
					List<string> listOfData2 = new List<string>()
					{
						"/warpplate mod <name> name <new name>",
						"/warpplate mod <name> size <w,h>",
						"/warppalte mod <name> delay <delay in seconds>",
						"/warppalte mod <name> destination <destination warpplate name>"
					};
					PaginationTools.Settings settings2 = new PaginationTools.Settings()
					{
						HeaderFormat = "Warpplate Mod Sub-Commands ({0}/{1}):",
						FooterFormat = $"Type {specifier}warpplate help mod {{0}} for more sub-commands."
					};

					PaginationTools.SendPage(args.Player, pageNumber2, listOfData2, settings2);
					return;
				}

				PaginationTools.SendPage(args.Player, 1, listOfData, settings);
			}

			//Processes warpplate mod commands
			void processModification()
			{
				Warpplate warpplate = Utils.GetWarpplateByName(args.Parameters[1]);

				if (warpplate == null)
				{
					args.Player.SendErrorMessage($"No warpplate found by the name {warpplate.Name}!");
					return;
				}

				string subcmd = args.Parameters[2].ToLower();

				switch (subcmd)
				{
					//warpplate mod <name> dest <new destination>
					case "dest":
					case "destination":
						string destinationWarpplateName = string.Join(" ", args.Parameters.GetRange(3, args.Parameters.Count - 3));
						if (destinationWarpplateName == "-n")
						{
							warpplate.DestinationWarpplate = null;
							DB.UpdateWarpplate(warpplate, DB.UpdateType.Destination);
							args.Player.SendSuccessMessage("Removed destination warpplate.");
							return;
						}
						if (Utils.GetWarpplateByName(destinationWarpplateName) == null)
						{
							args.Player.SendErrorMessage($"No warpplate found by the name {destinationWarpplateName}!");
							return;
						}
						warpplate.DestinationWarpplate = destinationWarpplateName;
						DB.UpdateWarpplate(warpplate, DB.UpdateType.Destination);
						args.Player.SendSuccessMessage($"Set destination warpplate to {destinationWarpplateName}.");
						break;
					//warpplate mod <name> delay <delay>
					case "delay":
						if (int.TryParse(args.Parameters[3], out int newDelay))
						{
							warpplate.Delay = newDelay;
							DB.UpdateWarpplate(warpplate, DB.UpdateType.Delay);
							args.Player.SendSuccessMessage($"Updated delay time to {newDelay} for warpplate {warpplate.Name}!");
						}
						else
						{
							args.Player.SendErrorMessage("Invalid delay value!");
						}
						break;
					//warpplate mod <name> size w,h
					case "size":
						string size = args.Parameters[3];
						if (!size.Contains(",") || size.Split(',').Length != 2)
						{
							args.Player.SendErrorMessage("Invalid syntax: {specifier}warpplate mod <name> size w,h");
							return;
						}
						if (int.TryParse(size.Split(',')[0], out int width) && (int.TryParse(size.Split(',')[1], out int height)))
						{
							//TODO: Set config for max width/height
							if (width < 1 || height < 1)
							{
								args.Player.SendErrorMessage("Invalid size!");
								return;
							}
							warpplate.Area = new Rectangle(warpplate.Area.X, warpplate.Area.Y, width, height);
							DB.UpdateWarpplate(warpplate, DB.UpdateType.Size);
							args.Player.SendSuccessMessage($"Updated size of warpplate {warpplate.Name}!");

						}
						else
						{
							args.Player.SendErrorMessage("Invalid size values!");
							return;
						}
						break;
					//warpplate mod <name> name <new name>
					case "name":
					case "label":
						string name = string.Join(" ", args.Parameters.GetRange(3, args.Parameters.Count - 3));
						string oldName = warpplate.Name;
						warpplate.Name = name;
						DB.UpdateWarpplate(warpplate, DB.UpdateType.Name, oldName);
						args.Player.SendSuccessMessage($"Updated name of warpplate to {warpplate.Name}!");
						break;
				}
			}
		}

		//Runs when /reload is used
		private static void ReloadWarp(ReloadEventArgs args)
		{
			DB.ReloadWarpplates();
		}

		private static void ToggleWarping(CommandArgs args)
		{
			PlayerInfo playerInfo = args.Player.GetData<PlayerInfo>(dataString);

			playerInfo.WarpingEnabled = !playerInfo.WarpingEnabled;

			args.Player.SendSuccessMessage($"Warping via warpplates is now {(playerInfo.WarpingEnabled ? "en" : "dis")}abled.");
		}

	}
}