using Harmony;

namespace SubnauticaMap
{
	[HarmonyPatch(typeof(uGUI_PingTab), "Awake", null)]
	internal static class uGUI_PingTab_Awake_Patch
	{
		private static void Postfix(uGUI_PingTab __instance)
		{
			SelectableWrapper value = new SelectableWrapper(__instance.visibilityToggle, delegate(GameInput.Button button)
			{
				if (button == GameInput.Button.LeftHand)
				{
					if (Controller.Instance != null && Controller.Instance.MapIsOpened())
					{
						return false;
					}
					__instance.visibilityToggle.isOn = !__instance.visibilityToggle.isOn;
					return true;
				}
				return false;
			});
			Traverse.Create(__instance).Field("selectableVisibilityToggle").SetValue(value);
		}
	}
}
