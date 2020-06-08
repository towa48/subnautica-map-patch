using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SubnauticaMap
{
	public class TechMapIcon
	{
		public static Color[] colors = new Color[5]
		{
			new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue),
			new Color32(byte.MaxValue, byte.MaxValue, 0, byte.MaxValue),
			new Color32(byte.MaxValue, 150, 150, byte.MaxValue),
			new Color32(byte.MaxValue, 170, byte.MaxValue, byte.MaxValue),
			new Color32(100, byte.MaxValue, byte.MaxValue, byte.MaxValue)
		};

		public Image image;

		public Vector3 position = Vector3.zero;

		public int colorIndex;

		public bool visible = true;

		public TechType type = TechType.Wreck;

		public string uniqueId;

		public string biome = "";

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
				Array values = Enum.GetValues(typeof(MapIconFlags));
				int index = Radical.FindIndex<TechType>((IEnumerable<TechType>)Controller.Instance.techMapIconsType, (Func<TechType, bool>)((TechType x) => x == type));
				uint num = (uint)values.GetValue(index);
				return (Controller.Settings.showMapIcons & num) == num;
			}
		}

		public void SetVisible(bool value)
		{
			visible = value;
			Color color = (!visible) ? new Color(0.5f, 0.5f, 0.5f, 0.8f) : colors[colorIndex];
			image.color = color;
		}

		public void ToggleVisible()
		{
			SetVisible(!visible);
		}

		public void SetColor(int index)
		{
			if (visible)
			{
				colorIndex = index;
				Color color = colors[colorIndex];
				image.color = color;
			}
		}

		public void ToggleColor()
		{
			SetColor((colorIndex + 1) % 5);
		}

		public void Destroy()
		{
			UnityEngine.Object.Destroy(image.gameObject);
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
