using UnityEngine;

namespace SubnauticaMap
{
	public class Container : MonoBehaviour
	{
		public Controller controller;

		private uGUI_Ping[] pings = new uGUI_Ping[0];

		private void OnEnable()
		{
			//IL_0046: Unknown result type (might be due to invalid IL or missing references)
			//IL_0050: Expected O, but got Unknown
			if ((bool)controller)
			{
				Map.CheckBiome(autoSwitchMap: true);
				controller.UpdateIcons();
				controller.RefreshFog();
				controller.CheckFogmapTexture();
				controller.AlignToPlayer();
			}
			Canvas.add_willRenderCanvases((WillRenderCanvases)(object)new WillRenderCanvases(OnWillRenderCanvases));
			uGUI_Pings uGUI_Pings = Object.FindObjectOfType<uGUI_Pings>();
			if ((bool)uGUI_Pings)
			{
				pings = uGUI_Pings.GetComponentsInChildren<uGUI_Ping>();
			}
		}

		private void OnDisable()
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_0011: Expected O, but got Unknown
			Canvas.remove_willRenderCanvases((WillRenderCanvases)(object)new WillRenderCanvases(OnWillRenderCanvases));
		}

		private void OnWillRenderCanvases()
		{
			uGUI_Ping[] array = pings;
			foreach (uGUI_Ping obj in array)
			{
				obj.SetIconAlpha(0f);
				obj.SetTextAlpha(0f);
			}
		}
	}
}
