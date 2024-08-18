using System;

namespace GameSystems.Config
{
	public sealed class ConfigManager : Component
	{

		private static ConfigManager _instance;

		public ConfigManager()
		{
			if ( _instance != null )
			{
				Log.Warning( "Only one instance of ConfigManager is allowed." );
			}
			_instance = this;
		}

		public static ConfigManager Instance => _instance;
		public CommandConfig Commands { get; } = new();

		protected override void OnStart()
		{

		}
	}
}