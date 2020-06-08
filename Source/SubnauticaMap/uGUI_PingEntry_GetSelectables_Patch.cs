using Harmony;
using System.Collections.Generic;
using UnityEngine.UI;

namespace SubnauticaMap
{
	[HarmonyPatch(typeof(uGUI_PingEntry), "GetSelectables", null)]
	internal static class uGUI_PingEntry_GetSelectables_Patch
	{
		private static bool Prefix(uGUI_PingEntry __instance, List<ISelectable> toFill)
		{
			List<ISelectable> list = Traverse.Create(__instance).Field("selectables").GetValue<List<ISelectable>>();
			if (list == null)
			{
				list = new List<ISelectable>();
				list.Add(new SelectableWrapper(__instance.visibility, delegate(GameInput.Button button)
				{
					if (button == GameInput.Button.LeftHand)
					{
						if (Controller.Instance != null && Controller.Instance.MapIsOpened())
						{
							return false;
						}
						__instance.visibility.isOn = !__instance.visibility.isOn;
						return true;
					}
					return false;
				}));
				int i = 0;
				for (int num = __instance.colorSelectors.Length; i < num; i++)
				{
					Toggle selectable = __instance.colorSelectors[i];
					list.Add(new SelectableWrapper(selectable, delegate(GameInput.Button button)
					{
						if (button == GameInput.Button.LeftHand)
						{
							if (Controller.Instance != null && Controller.Instance.MapIsOpened())
							{
								return false;
							}
							Toggle obj = ((SelectableWrapper)UISelection.selected).selectable as Toggle;
							obj.isOn = !obj.isOn;
							return true;
						}
						return false;
					}));
				}
				Traverse.Create(__instance).Field("selectables").SetValue(list);
			}
			toFill.AddRange(list);
			return false;
		}
	}
}
