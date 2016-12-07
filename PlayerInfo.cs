namespace AdvancedWarpplate
{
	public class PlayerInfo
	{
		public int TimeStandingOnWarp { get; set; }
		public bool WarpingEnabled { get; set; }
		public int Cooldown { get; set; }

		public PlayerInfo()
		{
			TimeStandingOnWarp = 0;
			WarpingEnabled = true;
			Cooldown = 0;
		}
	}
}
