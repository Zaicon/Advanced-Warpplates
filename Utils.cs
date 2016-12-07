using Microsoft.Xna.Framework;
using System.Linq;

namespace AdvancedWarpplate
{
	public static class Utils
	{
		public static Warpplate GetNearbyWarpplates(int tileX, int tileY)
		{
			//Returns first (or null) warpplate found with an Area that contains the specified X,Y location.
			return DB.warpplateList.FirstOrDefault(e => e.Area.Contains(tileX, tileY));
		}

		public static Warpplate GetWarpplateByName(string warpplateName)
		{
			//Returns first (or null) warpplate found by the specified name (exact match only).
			return DB.warpplateList.FirstOrDefault(e => e.Name == warpplateName);
		}
	}

	public class Warpplate
	{
		public Rectangle Area { get; set; }
		public string Name { get; set; }
		public string DestinationWarpplate { get; set; }
		public int Delay { get; set; }
		
		//Used for new warpplates
		public Warpplate(string _name, int _x, int _y)
		{
			Area = new Rectangle(_x, _y, 2, 3);
			Name = _name;
			DestinationWarpplate = null;
			Delay = 3;
		}
		
		//Used for existing warpplates (when reloading from database)
		public Warpplate(string _name, int _x, int _y, int _width, int _height, int _delay, string _destination)
		{
			Name = _name;
			DestinationWarpplate = _destination;
			Delay = _delay;
			Area = new Rectangle(_x, _y, _width, _height);
		}
	}
}
