using Harmony;
using System;
using UnityEngine;

namespace SubnauticaMap
{
	[HarmonyPatch(typeof(SaveLoadManager), "SaveToTemporaryStorageAsync", new Type[]
	{
		typeof(Texture2D)
	})]
	internal class SaveLoadManager_SaveToTemporaryStorageAsync_Patch
	{
		private static void Postfix(SaveLoadManager __instance)
		{
			Controller.Instance?.OnSave();
		}
	}
}
