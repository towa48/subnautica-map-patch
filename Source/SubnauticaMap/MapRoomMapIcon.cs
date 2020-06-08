using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SubnauticaMap
{
	public class MapRoomMapIcon
	{
		public Image scanCircle;

		public Image image;

		public MapRoomFunctionality room;

		public int colorIndex;

		public bool visible;

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

		public bool isEnabled => (Controller.Settings.showPingIcons & 0x80) == 128;

		public void SetVisible(bool value)
		{
			if (colorIndex >= PingManager.colorOptions.Length)
			{
				colorIndex = PingManager.colorOptions.Length - 1;
			}
			visible = value;
			image.color = ((!visible) ? Color.white : PingManager.colorOptions[colorIndex]);
		}

		public void ToggleVisible()
		{
			SetVisible(!visible);
		}

		public void SetColor(int index)
		{
			if (visible)
			{
				if (colorIndex >= PingManager.colorOptions.Length)
				{
					colorIndex = PingManager.colorOptions.Length - 1;
				}
				colorIndex = index;
				image.color = PingManager.colorOptions[colorIndex];
			}
		}

		public void ToggleColor()
		{
			SetColor((colorIndex + 1) % PingManager.colorOptions.Length);
		}

		public void Rescale()
		{
			if ((bool)image)
			{
				image.rectTransform.localScale = Vector3.one * Controller.Settings.iconsScale;
			}
		}

		public static IEnumerator Rotate(Image image)
		{
			while (image != null)
			{
				if (image.enabled && image.gameObject.activeInHierarchy && (bool)image.sprite && image.sprite.name == "scan_circle_active")
				{
					image.transform.eulerAngles = new Vector3(0f, 0f, (image.transform.eulerAngles.z - Time.deltaTime * 30f) % 360f);
				}
				yield return null;
			}
		}
	}
}
