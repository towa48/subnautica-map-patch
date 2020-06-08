using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace SubnauticaMap
{
	public class Settings
	{
		public FogStyle fogStyle;

		public float mapTransparency = 0.8f;

		public float fogTransparency;

		public float iconsScale = 1f;

		public bool showCoordinates;

		public uint showMapIcons = 7u;

		public uint showPingIcons = 511u;

		public KeyCode scanningKeybinding = KeyCode.Mouse2;

		public KeyCode mapKeybinding = KeyCode.M;

		public KeyCode pingKeybinding = KeyCode.N;

		public void Save()
		{
			string path = Path.Combine(Controller.Dir, "settings.json");
			string contents = JsonUtility.ToJson((object)this, true);
			File.WriteAllText(path, contents);
		}

		public void Load()
		{
			Settings settings = new Settings();
			string path = Path.Combine(Controller.Dir, "settings.json");
			if (File.Exists(path))
			{
				try
				{
					settings = JsonUtility.FromJson<Settings>(File.ReadAllText(path));
				}
				catch (Exception exception)
				{
					Debug.LogException(exception);
					settings.Save();
				}
			}
			FieldInfo[] fields = typeof(Settings).GetFields();
			foreach (FieldInfo fieldInfo in fields)
			{
				if (fieldInfo.IsPublic)
				{
					fieldInfo.SetValue(this, fieldInfo.GetValue(settings));
				}
			}
		}
	}
}
