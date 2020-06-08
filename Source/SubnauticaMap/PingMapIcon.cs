using Harmony;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SubnauticaMap
{
	public class PingMapIcon
	{
		public PingInstance ping;

		public Image image;

		public uGUI_Icon icon;

		public SimpleTooltip tooltip;

		private PingType cachedPingType;

		public bool active
		{
			get
			{
				return image.gameObject.activeSelf;
			}
			set
			{
				if (image.gameObject.activeSelf != value)
				{
					image.gameObject.SetActive(value);
				}
			}
		}

		public bool isEnabled
		{
			get
			{
				if (ping.pingType == PingType.None)
				{
					return false;
				}
				if (ping.pingType > PingType.Sunbeam)
				{
					return true;
				}
				uint num = (uint)Enum.GetValues(typeof(PingTypeFlags)).GetValue((int)(ping.pingType - 1));
				return (Controller.Settings.showPingIcons & num) == num;
			}
		}

		public void Refresh(bool force = true)
		{
			if (force || cachedPingType != ping.pingType)
			{
				string name = Enum.GetName(typeof(PingType), ping.pingType);
				icon.sprite = SpriteManager.Get(SpriteManager.Group.Pings, name);
				icon.color = Color.black;
				RectTransform rectTransform = icon.rectTransform;
				rectTransform.sizeDelta = Vector2.one * 28f;
				rectTransform.localPosition = Vector3.zero;
				cachedPingType = ping.pingType;
			}
		}

		public uGUI_PingEntry GetUIPingEntry()
		{
			if (!ping)
			{
				return null;
			}
			uGUI_PingTab uGUI_PingTab = (uGUI_PingTab)Player.main.GetPDA().ui.GetTab(PDATab.Ping);
			if ((bool)uGUI_PingTab)
			{
				foreach (KeyValuePair<int, uGUI_PingEntry> item in Traverse.Create(uGUI_PingTab).Field("entries").GetValue<Dictionary<int, uGUI_PingEntry>>())
				{
					if (Traverse.Create(item.Value).Field("id").GetValue<int>() == ping.GetInstanceID())
					{
						return item.Value;
					}
				}
			}
			return null;
		}

		public void SetVisible(bool visible)
		{
			if ((bool)ping)
			{
				ping.SetVisible(visible);
				image.color = ((!ping.visible) ? Color.white : PingManager.colorOptions[ping.colorIndex]);
				uGUI_PingEntry uIPingEntry = GetUIPingEntry();
				if ((bool)uIPingEntry)
				{
					uIPingEntry.visibility.isOn = ping.visible;
				}
			}
		}

		public void ToggleVisible()
		{
			if ((bool)ping)
			{
				SetVisible(!ping.visible);
			}
		}

		public void SetColor(int colorIndex)
		{
			if ((bool)ping && ping.visible)
			{
				ping.SetColor(colorIndex);
				image.color = PingManager.colorOptions[ping.colorIndex];
				uGUI_PingEntry uIPingEntry = GetUIPingEntry();
				if ((bool)uIPingEntry)
				{
					Traverse.Create(uIPingEntry).Method("SetColor", new Type[1]
					{
						typeof(int)
					}, new object[1]
					{
						ping.colorIndex
					}).GetValue();
				}
			}
		}

		public void ToggleColor()
		{
			if ((bool)ping)
			{
				SetColor((ping.colorIndex + 1) % PingManager.colorOptions.Length);
			}
		}

		public void Rescale()
		{
			if ((bool)image)
			{
				image.rectTransform.localScale = Vector3.one * Controller.Settings.iconsScale;
			}
		}
	}
}
