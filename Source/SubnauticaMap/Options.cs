using Harmony;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SubnauticaMap
{
	public class Options : MonoBehaviour
	{
		public static Options instance;

		private uGUI_OptionsPanel optionsPanel;

		private void Awake()
		{
			instance = this;
			optionsPanel = GetComponent<uGUI_OptionsPanel>();
		}

		private void OnEnable()
		{
			AddTab();
		}

		private void OnDisable()
		{
			Controller.Instance.ApplySettings();
			Controller.Settings.Save();
		}

		private void OnDestroy()
		{
			instance = null;
		}

		private void AddTab()
		{
			int num = 0;
			IEnumerable value = Traverse.Create(optionsPanel).Field("tabs").GetValue<IEnumerable>();
			if (value != null)
			{
				int num2 = 0;
				foreach (object item in value)
				{
					if (((GameObject)item.GetType().GetField("tab").GetValue(item))?.GetComponentInChildren<Text>().text == "Mods")
					{
						num = num2;
						break;
					}
					num2++;
				}
			}
			if (num == 0)
			{
				num = optionsPanel.AddTab("Mods");
			}
			optionsPanel.AddHeading(num, "SubnauticaMap");
			optionsPanel.AddChoiceOption(num, "Fog", Enum.GetNames(typeof(FogStyle)), (int)Controller.Settings.fogStyle, OnStyleChanged);
			optionsPanel.AddSliderOption(num, "Fog Transparency", Controller.Settings.fogTransparency, 0f, 1f, 0f, OnFogTransparencyChanged);
			optionsPanel.AddSliderOption(num, "Map Transparency", Controller.Settings.mapTransparency, 0.3f, 1f, 0.8f, OnMapTransparencyChanged);
			optionsPanel.AddSliderOption(num, "Icons Scale", Controller.Settings.iconsScale * 100f, 50f, 150f, 100f, OnIconsScaleChanged);
			optionsPanel.AddToggleOption(num, "Coordinates and Biome", Controller.Settings.showCoordinates, OnShowCoordinatesChanged);
			Array values = Enum.GetValues(typeof(PingTypeFlags));
			int num3 = 0;
			string[] names = Enum.GetNames(typeof(PingTypeFlags));
			foreach (string key in names)
			{
				uint flag2 = (uint)values.GetValue(num3);
				optionsPanel.AddToggleOption(num, "Display " + Language.main.Get(key), (Controller.Settings.showPingIcons & flag2) == flag2, delegate
				{
					Controller.Settings.showPingIcons ^= flag2;
					if (Controller.Instance.MapIsOpened())
					{
						Controller.Instance.UpdateIcons();
					}
				});
				num3++;
			}
			values = Enum.GetValues(typeof(MapIconFlags));
			num3 = 0;
			names = Enum.GetNames(typeof(MapIconFlags));
			foreach (string key2 in names)
			{
				uint flag = (uint)values.GetValue(num3);
				optionsPanel.AddToggleOption(num, "Display " + Language.main.Get(key2), (Controller.Settings.showMapIcons & flag) == flag, delegate
				{
					Controller.Settings.showMapIcons ^= flag;
					if (Controller.Instance.MapIsOpened())
					{
						Controller.Instance.UpdateIcons();
					}
				});
				num3++;
			}
			AddBinding(num, "Scanner room", KeyCodeToKeyName(Controller.Settings.scanningKeybinding), delegate(string str)
			{
				Controller.Settings.scanningKeybinding = KeyNameToKeyCode(str);
			});
			AddBinding(num, "Map tab", KeyCodeToKeyName(Controller.Settings.mapKeybinding), delegate(string str)
			{
				Controller.Settings.mapKeybinding = KeyNameToKeyCode(str);
			});
			AddBinding(num, "Ping tab", KeyCodeToKeyName(Controller.Settings.pingKeybinding), delegate(string str)
			{
				Controller.Settings.pingKeybinding = KeyNameToKeyCode(str);
			});
		}

		private void OnReloadMaps()
		{
			Controller.Instance.ReloadMaps();
			IngameMenu.main.Close();
		}

		private void OnStyleChanged(int index)
		{
			Controller.Settings.fogStyle = (FogStyle)index;
			ApplySettings();
		}

		private void OnFogTransparencyChanged(float value)
		{
			Controller.Settings.fogTransparency = value;
			ApplySettings();
		}

		private void OnMapTransparencyChanged(float value)
		{
			Controller.Settings.mapTransparency = value;
			ApplySettings();
		}

		private void OnIconsScaleChanged(float value)
		{
			Controller.Settings.iconsScale = value / 100f;
			ApplySettings();
		}

		private void OnShowCoordinatesChanged(bool value)
		{
			Controller.Settings.showCoordinates = value;
			ApplySettings();
		}

		private void ApplySettings()
		{
			if (Controller.Instance.MapIsOpened())
			{
				Controller.Instance.ApplySettings();
			}
		}

		private void AddBinding(int tabIndex, string label, string value, UnityAction<string> action)
		{
			GameObject gameObject = optionsPanel.AddItem(tabIndex, optionsPanel.bindingOptionPrefab);
			Text componentInChildren = gameObject.GetComponentInChildren<Text>();
			if (componentInChildren != null)
			{
				gameObject.GetComponentInChildren<TranslationLiveUpdate>().translationKey = label;
				componentInChildren.text = Language.main.Get(label);
			}
			uGUI_Bindings componentInChildren2 = gameObject.GetComponentInChildren<uGUI_Bindings>();
			uGUI_Binding uGUI_Binding = componentInChildren2.bindings.First();
			UnityEngine.Object.Destroy(componentInChildren2.bindings.Last().gameObject);
			UnityEngine.Object.Destroy(componentInChildren2);
			uGUI_Binding.value = value;
			uGUI_Binding.onValueChanged.RemoveAllListeners();
			uGUI_Binding.onValueChanged.AddListener(action);
		}

		private KeyCode KeyNameToKeyCode(string str)
		{
			IEnumerable value = Traverse.Create<GameInput>().Field("inputs").GetValue<IEnumerable>();
			if (value != null)
			{
				foreach (object item in value)
				{
					if (Traverse.Create(item).Field("name").GetValue<string>() == str)
					{
						return Traverse.Create(item).Field("keyCode").GetValue<KeyCode>();
					}
				}
			}
			return KeyCode.None;
		}

		private string KeyCodeToKeyName(KeyCode key)
		{
			IEnumerable value = Traverse.Create<GameInput>().Field("inputs").GetValue<IEnumerable>();
			if (value != null)
			{
				foreach (object item in value)
				{
					if (Traverse.Create(item).Field("keyCode").GetValue<KeyCode>() == key)
					{
						return Traverse.Create(item).Field("name").GetValue<string>();
					}
				}
			}
			return "";
		}
	}
}
