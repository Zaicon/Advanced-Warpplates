using Mono.Data.Sqlite;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using Terraria;
using TShockAPI;
using TShockAPI.DB;

namespace AdvancedWarpplate
{
	/// <summary>
	/// The static class used for database queries.
	/// </summary>
	public static class DB
	{
		/// <summary>
		/// The list of warpplates in the current map.
		/// </summary>
		public static List<Warpplate> warpplateList = new List<Warpplate>();
		private static IDbConnection db;

		/// <summary>
		/// Adds a new warpplate to the database.
		/// </summary>
		/// <param name="_newWarpplate">The warpplate to be added.</param>
		public static void AddWarpplate(Warpplate _newWarpplate)
		{
			string query = $"INSERT INTO `Warpplates` (`warpname`, `x`, `y`, `width`, `height`, `delay`, `destination`, `worldid`) " +
							$"VALUES ('{_newWarpplate.Name}', " +
							$"{_newWarpplate.Area.X}, " +
							$"{_newWarpplate.Area.Y}, " +
							$"{_newWarpplate.Area.Width}, " +
							$"{_newWarpplate.Area.Height}, " +
							$"{_newWarpplate.Delay}, " +
							$"'{_newWarpplate.DestinationWarpplate}', " +
							$"{Main.worldID});";

			int result = db.Query(query);
			if (result != 1)
				TShock.Log.ConsoleError("Failed to add warpplate to database.");
			else
				warpplateList.Add(_newWarpplate);
		}

		/// <summary>
		/// Removes a specified warpplate from the database.
		/// </summary>
		/// <param name="_warpplateToRemove">The warpplate to be removed.</param>
		public static void RemoveWarpplate(Warpplate _warpplateToRemove)
		{
			string query = $"DELETE FROM `Warpplates` WHERE `warpname` = '{_warpplateToRemove.Name}';";
			int result = db.Query(query);
			if (result != 1)
				TShock.Log.ConsoleError("Failed to remove warpplate from database.");
			else
				warpplateList.Remove(_warpplateToRemove);
		}

		/// <summary>
		/// Updates a specified warpplate in the database.
		/// </summary>
		/// <param name="_warpplateToUpdate">The warpplate to update.</param>
		/// <param name="_type">The type of information being updated.</param>
		/// <param name="_oldName">If the name is being updated, the previous name of the warpplate.</param>
		public static void UpdateWarpplate(Warpplate _warpplateToUpdate, UpdateType _type, string _oldName = "")
		{
			string oldName = _warpplateToUpdate.Name;
			string query = "UPDATE `Warpplates` SET `";
			switch (_type)
			{
				case UpdateType.Delay:
					query += $"delay` = {_warpplateToUpdate.Delay}";
					break;
				case UpdateType.Destination:
					query += $"destination` = '{_warpplateToUpdate.DestinationWarpplate}'";
					break;
				case UpdateType.Name:
					query += $"warpname` = '{_warpplateToUpdate.Name}'";
					oldName = _oldName;
					break;
				case UpdateType.Size:
					query += $"width` = {_warpplateToUpdate.Area.Width} AND `height` = {_warpplateToUpdate.Area.Height}";
					break;
			}
			query += $" WHERE `warpname` = '{oldName}'";

			int result = db.Query(query);
			if (result != 1)
				TShock.Log.ConsoleError("Failed to update warpplate information in database.");
		}

		/// <summary>
		/// Reloads the warpplates from the database.
		/// </summary>
		public static void ReloadWarpplates()
		{
			warpplateList.Clear();
			string query = $"SELECT * FROM `Warpplates` WHERE `worldid` = {Main.worldID};";
			using (var reader = db.QueryReader(query))
			{
				while (reader.Read())
				{
					Warpplate warpplate = new Warpplate(
						reader.Get<string>("warpname"),
						reader.Get<int>("x"),
						reader.Get<int>("y"),
						reader.Get<int>("width"),
						reader.Get<int>("height"),
						reader.Get<int>("delay"),
						reader.Get<string>("destination"));
					warpplateList.Add(warpplate);
				}
			}
			TShock.Log.Info("Reloaded warpplates from database.");
		}

		/// <summary>
		/// Connects to the database.
		/// </summary>
		public static void Connect()
		{
			switch (TShock.Config.StorageType.ToLower())
			{
				case "mysql":
					string[] dbHost = TShock.Config.MySqlHost.Split(':');
					db = new MySqlConnection()
					{
						ConnectionString = string.Format("Server={0}; Port={1}; Database={2}; Uid={3}; Pwd={4};",
							dbHost[0],
							dbHost.Length == 1 ? "3306" : dbHost[1],
							TShock.Config.MySqlDbName,
							TShock.Config.MySqlUsername,
							TShock.Config.MySqlPassword)

					};
					break;

				case "sqlite":
					string sql = Path.Combine(TShock.SavePath, "Warpplates.sqlite");
					db = new SqliteConnection(string.Format("uri=file://{0},Version=3", sql));
					break;

			}

			SqlTableCreator sqlcreator = new SqlTableCreator(db, db.GetSqlType() == SqlType.Sqlite ? (IQueryBuilder)new SqliteQueryCreator() : new MysqlQueryCreator());

			sqlcreator.EnsureTableStructure(new SqlTable("Warpplates",
				new SqlColumn("warpname", MySqlDbType.VarChar) { Primary = true, Unique = true, Length = 30 },
				new SqlColumn("x", MySqlDbType.Int32) { Length = 5 },
				new SqlColumn("y", MySqlDbType.Int32) { Length = 5 },
				new SqlColumn("width", MySqlDbType.Int32) { Length = 5 },
				new SqlColumn("height", MySqlDbType.Int32) { Length = 5 },
				new SqlColumn("delay", MySqlDbType.Int32) { Length = 3 },
				new SqlColumn("destination", MySqlDbType.VarChar) { Length = 30 },
				new SqlColumn("worldid", MySqlDbType.Int32) { Length = 15 }));
		}

		/// <summary>
		/// The type of information being updated.
		/// </summary>
		public enum UpdateType
		{
			Name,
			Size,
			Destination,
			Delay
		}
	}
}
