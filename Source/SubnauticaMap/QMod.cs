using Harmony;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SubnauticaMap
{
	public class QMod
	{
		public static void Load()
		{
			try
			{
				HarmonyInstance.Create("subnautica.subnauticamap.mod").PatchAll(Assembly.GetExecutingAssembly());
				SceneManager.sceneLoaded += OnSceneLoaded;
				Logger.Write("Patched");
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
		}

		private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
		{
			if (scene.name == "Main")
			{
				Controller.Load();
			}
		}
	}
}
