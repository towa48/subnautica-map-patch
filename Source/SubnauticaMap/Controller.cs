using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UWE;

namespace SubnauticaMap
{
	public class Controller : MonoBehaviour
	{
		private static Settings settings = new Settings();

		private bool isStarted;

		private Dictionary<int, PingMapIcon> pingMapIconList = new Dictionary<int, PingMapIcon>();

		private Dictionary<MapRoomFunctionality, MapRoomMapIcon> mapRoomMapIconList = new Dictionary<MapRoomFunctionality, MapRoomMapIcon>();

		private Dictionary<GameObject, List<TechType>> availableTechTypes = new Dictionary<GameObject, List<TechType>>();

		private Dictionary<TechType, Dictionary<string, TechMapIcon>> techMapIconList = new Dictionary<TechType, Dictionary<string, TechMapIcon>>();

		public TechType[] techMapIconsType = new TechType[3]
		{
			TechType.Wreck,
			TechType.HeatArea,
			TechType.PrecursorPrisonArtifact1
		};

		private Font font;

		private GameObject prefabMapSwitcher;

		private GameObject prefabMapSwitchPanel;

		private GameObject prefabMapSwitchItem;

		private GameObject prefabMapContainer;

		private GameObject prefabScanPanel;

		private GameObject prefabScanPanelItem;

		private GameObject prefabToogleMap;

		private GameObject prefabCoord;

		private RectTransform scanPanel;

		private RectTransform scanList;

		private GameObject mapContainer;

		private ScrollRect scrollView;

		private GameObject iconsContainer;

		private GameObject backgroundContainer;

		private GameObject playerIconGO;

		private Image playerImage;

		private RawImage mapImage;

		private Button toggleMap;

		private Text coord;

		private Button mapSwitcher;

		private RectTransform mapSwitchPanel;

		private int worldSize = 4000;

		private int mapSize = 2000;

		private int mapMinSize = 1000;

		private int mapMaxSize = 3000;

		private float mapScale = 2f;

		private AssetBundle assets;

		private Texture2D brush;

		private Material materialFogdecal;

		private Material materialMapColored;

		public Sprite[] allSprites = new Sprite[0];

		private Vector3 lastPositionForCheck = Vector2.zero;

		private Map currentMap;

		public Map lastDefaultMap;

		private static List<UnityEngine.Object> destroyOnUnload = new List<UnityEngine.Object>();

        private static string savesPath = Path.Combine(SNUtils.applicationRootDir, "SNAppData/SavedGames");

        private string debugMessage1 = "";

		private string debugMessage2 = "";

        public static Settings Settings => settings;

		public static string Dir => $"{Environment.CurrentDirectory}\\QMods\\SubnauticaMap\\";

		public static string SaveDir => Path.Combine(savesPath, SaveLoadManager.main.GetCurrentSlot());

		public static Controller Instance
		{
			get;
			private set;
		}

		public AssetBundle Assets => assets;

		public Map CurrentMap => currentMap;

		public static void Load()
		{
			Unload();
			settings.Load();
			destroyOnUnload.Add(new GameObject("SubnauticaMap.Controller").AddComponent<Controller>().gameObject);
			Logger.Write("Loaded");
			GameInput.SetBinding(GameInput.Device.Keyboard, GameInput.Button.UIUp, GameInput.BindingSet.Primary, "UpArrow");
			GameInput.SetBinding(GameInput.Device.Keyboard, GameInput.Button.UIDown, GameInput.BindingSet.Primary, "DownArrow");
		}

		public static void Unload()
		{
			while (destroyOnUnload.Count > 0)
			{
				UnityEngine.Object.Destroy(destroyOnUnload[0]);
				destroyOnUnload.RemoveAt(0);
			}
			if ((bool)uGUI_PDA.main)
			{
				uGUI_PingTab uGUI_PingTab = (uGUI_PingTab)uGUI_PDA.main.GetTab(PDATab.Ping);
				if ((bool)uGUI_PingTab)
				{
					uGUI_PingTab.content = uGUI_PingTab.transform.Find("Content").gameObject;
				}
			}
		}

		private void Awake()
		{
			Instance = this;
		}

		private IEnumerator Start()
		{
			yield return new WaitForSeconds(1f);
			while (!uGUI_SceneLoading.IsLoadingScreenFinished || !uGUI.main || uGUI.main.loading.IsLoading)
			{
				yield return null;
			}
			try
			{
				Run();
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
				Logger.Show("The mod cannot be started");
				Unload();
			}
		}

		private void OnDestroy()
		{
			Map.All().Clear();
			Fogmap.Release();
			Fogmap.All().Clear();
		}

		private void OnApplicationQuit()
		{
		}

		public void OnSave()
		{
			if (isStarted)
			{
				Fogmap.SaveAll();
				SaveMapIcons();
				Logger.Write("Save");
			}
		}

		private void LoadAssetBundle()
		{
			assets = (from x in Resources.FindObjectsOfTypeAll<AssetBundle>()
				where ((UnityEngine.Object)(object)x).name == "subnauticamap"
				select x).FirstOrDefault();
			if ((UnityEngine.Object)(object)assets == null)
			{
				assets = AssetBundle.LoadFromFile(Path.Combine(Dir, "subnauticamap"));
			}
			if ((UnityEngine.Object)(object)assets == null)
			{
				Logger.Write("Failed to load assets");
			}
		}

		private void LoadAssets()
		{
			materialMapColored = assets.LoadAsset<Material>("MapColored");
			materialFogdecal = assets.LoadAsset<Material>("Fogdecal");
			prefabMapSwitcher = assets.LoadAsset<GameObject>("MapSwitcher");
			prefabMapSwitchPanel = assets.LoadAsset<GameObject>("MapSwitchPanel");
			prefabMapSwitchItem = assets.LoadAsset<GameObject>("MapSwitchItem");
			prefabCoord = assets.LoadAsset<GameObject>("Coord");
			prefabToogleMap = assets.LoadAsset<GameObject>("ToggleMap");
			prefabMapContainer = assets.LoadAsset<GameObject>("MapContainer");
			prefabScanPanel = assets.LoadAsset<GameObject>("ScanPanel");
			prefabScanPanelItem = assets.LoadAsset<GameObject>("ScanPanelItem");
			allSprites = assets.LoadAllAssets<Sprite>();
			brush = assets.LoadAsset<Texture2D>("brush");
		}

		public bool MapIsOpened()
		{
			PDA pDA = Player.main.GetPDA();
			if ((bool)pDA && pDA.state == PDA.State.Opened && pDA.ui.currentTabType == PDATab.Ping && mapContainer.activeSelf)
			{
				return true;
			}
			return false;
		}

		public bool PingIsOpened()
		{
			PDA pDA = Player.main.GetPDA();
			if ((bool)pDA && pDA.state == PDA.State.Opened && pDA.ui.currentTabType == PDATab.Ping && !mapContainer.activeSelf)
			{
				return true;
			}
			return false;
		}

		private void Update()
		{
			if (!isStarted || uGUI.isIntro || LaunchRocket.isLaunching)
			{
				return;
			}
			MapRoomCamera camera = uGUI_CameraDrone.main.GetCamera();
			if (camera != null)
			{
				if ((camera.transform.position - lastPositionForCheck).sqrMagnitude > 100f)
				{
					Map.CheckBiome(autoSwitchMap: true);
					UpdateFogCameraDrone(camera.transform.position);
				}
				return;
			}
			if (currentMap != null && (Player.main.transform.position - lastPositionForCheck).sqrMagnitude > 100f)
			{
				lastPositionForCheck = Player.main.transform.position;
				Map.CheckBiome(autoSwitchMap: true);
				UpdateFog();
				techMapIconsType.ForEach(delegate(TechType x)
				{
					ScanResources(x);
				});
			}
			if (Settings.showCoordinates && mapContainer.activeSelf && CursorManager.lastRaycast.isValid && CursorManager.lastRaycast.gameObject.transform.IsChildOf(mapContainer.transform))
			{
				RectTransformUtility.ScreenPointToLocalPointInRectangle(scrollView.content, (Vector2)Input.mousePosition, CursorManager.lastRaycast.module.eventCamera, out Vector2 a);
				if (CurrentMap != null && CurrentMap.mapId == "default_biome")
				{
					Vector2 vector = a * mapScale;
					coord.text = $"{LargeWorld.main.GetBiome(new Vector3(vector.x, 0f, vector.y)).ToUpper()}";
				}
				else
				{
					coord.text = $"You {Player.main.transform.position.XZ()}  Map {a * mapScale}";
				}
			}
			bool buttonDown;
			Vector2 uIDirection = GameInput.GetUIDirection(out buttonDown);
			if (mapSwitchPanel.gameObject.activeSelf && ((Input.anyKeyDown && !Input.GetKeyDown(KeyCode.JoystickButton0) && !Input.GetKeyDown(KeyCode.JoystickButton3) && CursorManager.lastRaycast.isValid && !CursorManager.lastRaycast.gameObject.transform.IsChildOf(mapSwitchPanel.transform) && !CursorManager.lastRaycast.gameObject.transform.IsChildOf(mapSwitcher.transform)) || GameInput.GetUIScrollDelta() != 0f || uIDirection != Vector2.zero))
			{
				mapSwitchPanel.gameObject.SetActive(value: false);
			}
			PDA pDA = Player.main.GetPDA();
			uGUI_PingTab uGUI_PingTab = pDA ? (pDA.ui.GetTab(PDATab.Ping) as uGUI_PingTab) : null;
			if (!pDA || !mapContainer || !uGUI_PingTab || pDA.state == PDA.State.Closing || pDA.state == PDA.State.Opening || pDA.ui.currentTabType == PDATab.TimeCapsule || (!pDA.isOpen && !UWE.Utils.lockCursor) || IngameMenu.main.gameObject.activeInHierarchy)
			{
				return;
			}
			if (GameInput.GetControllerEnabled() && pDA.isOpen && mapContainer.activeSelf && currentMap != null && mapSwitchPanel != null)
			{
				if (Input.GetKeyDown(KeyCode.JoystickButton3))
				{
					mapSwitchPanel.gameObject.SetActive(value: true);
					Button[] componentsInChildren = mapSwitchPanel.GetComponentsInChildren<Button>();
					List<Map> list = Map.All();
					int num = list.FindIndex((Map x) => x == currentMap);
					num = (int)Mathf.Repeat(num - 1, list.Count);
					componentsInChildren[num].onClick.Invoke();
					componentsInChildren[num].Select();
				}
				else if (Input.GetKeyDown(KeyCode.JoystickButton0))
				{
					mapSwitchPanel.gameObject.SetActive(value: true);
					Button[] componentsInChildren2 = mapSwitchPanel.GetComponentsInChildren<Button>();
					List<Map> list2 = Map.All();
					int num2 = list2.FindIndex((Map x) => x == currentMap);
					num2 = (int)Mathf.Repeat(num2 + 1, list2.Count);
					componentsInChildren2[num2].onClick.Invoke();
					componentsInChildren2[num2].Select();
				}
			}
			bool flag = Input.GetKeyDown(Settings.mapKeybinding);
			bool flag2 = Input.GetKeyDown(Settings.pingKeybinding);
			if (!flag && !flag2 && GameInput.GetControllerEnabled() && pDA.isOpen)
			{
				flag = (Input.GetKeyDown(KeyCode.JoystickButton2) && !mapContainer.activeSelf);
				flag2 = (Input.GetKeyDown(KeyCode.JoystickButton2) && mapContainer.activeSelf);
			}
			float num3 = (Input.mouseScrollDelta.y != 0f) ? Input.mouseScrollDelta.y : (GameInput.GetControllerEnabled() ? (GameInput.GetUIScrollDelta() * 0.1f) : 0f);
			if (num3 != 0f && ((CursorManager.lastRaycast.isValid && (CursorManager.lastRaycast.gameObject.transform.IsChildOf(scanPanel) || CursorManager.lastRaycast.gameObject.transform.IsChildOf(iconsContainer.transform))) || !mapContainer.gameObject.activeSelf))
			{
				num3 = 0f;
			}
			if (!flag && !flag2 && num3 == 0f && uIDirection == Vector2.zero)
			{
				return;
			}
			if (flag || flag2)
			{
				GameObject gameObject = uGUI_PingTab.transform.Find("Content").gameObject;
				GameObject gameObject2 = flag ? mapContainer : gameObject;
				if (pDA.state == PDA.State.Closed)
				{
					pDA.Open(PDATab.Ping);
				}
				else if (pDA.ui.currentTabType != PDATab.Ping)
				{
					pDA.ui.OpenTab(PDATab.Ping);
				}
				else if (!gameObject2.activeSelf)
				{
					toggleMap.onClick.Invoke();
				}
				else
				{
					pDA.Close();
				}
				if (uGUI_PingTab.content != gameObject2 && pDA.state != PDA.State.Closing)
				{
					toggleMap.onClick.Invoke();
				}
			}
			if (num3 != 0f)
			{
				Zoom(num3, GameInput.GetControllerEnabled() && GameInput.GetUIScrollDelta() != 0f);
			}
			if (uIDirection != Vector2.zero)
			{
				uIDirection.y = 0f - uIDirection.y;
				scrollView.normalizedPosition = (scrollView.normalizedPosition + uIDirection * 0.05f * Mathf.Lerp(1f, 0.25f, (scrollView.content.sizeDelta.x - (float)mapMinSize) / (float)(mapMaxSize - mapMinSize))).Clamp(Vector2.zero, Vector2.one);
			}
		}

		private void Zoom(float scroll, bool controller)
		{
			Vector2 vector = Vector2.one * ((!controller) ? 500f : 50f);
			scrollView.content.sizeDelta = (scrollView.content.sizeDelta + ((scroll > 0f) ? vector : (-vector))).Clamp(Vector2.one * mapMinSize, Vector2.one * mapMaxSize);
			scrollView.normalizedPosition = scrollView.normalizedPosition.Clamp(Vector2.zero, Vector2.one);
			mapScale = (float)worldSize / scrollView.content.sizeDelta.x;
			UpdateIcons();
		}

		public void RefreshFog()
		{
			if (isStarted)
			{
				UpdateFog();
				Fogmap fogmap = (from x in Fogmap.All()
					where x.id == "world"
					select x).FirstOrDefault();
				foreach (MapRoomMapIcon value in mapRoomMapIconList.Values)
				{
					PaintFog(fogmap, brush, value.room.transform.position, value.room.GetScanRange());
				}
			}
		}

		private void UpdateFog()
		{
			float num = 100f;
			num = ((!Map.BiomeIsCave(Player.main.GetBiomeString())) ? (num * Mathf.Lerp(0.5f, 1f, DayNightCycle.main.GetLightScalar())) : (num * 0.75f));
			foreach (Fogmap item in Fogmap.All())
			{
				if (item.isActive)
				{
					PaintFog(item, brush, Player.main.transform.position, num);
				}
			}
			if (MapIsOpened())
			{
				playerIconGO.transform.localPosition = Player.main.transform.position.XZ() / mapScale;
			}
		}

		private void UpdateFogCameraDrone(Vector3 pos)
		{
			float brushSize = 100f * Mathf.Lerp(0.5f, 1f, DayNightCycle.main.GetLightScalar());
			foreach (Fogmap item in Fogmap.All())
			{
				if (item.isActive)
				{
					PaintFog(item, brush, pos, brushSize);
				}
			}
		}

		private void PaintFog(Fogmap fogmap, Texture2D brush, Vector3 pos, float brushSize)
		{
			if (fogmap != null && !(fogmap.rt == null))
			{
				Vector2 vector = pos.XZ();
				RenderTexture rt = fogmap.rt;
				int width = rt.width;
				float num = (float)mapSize / brushSize;
				int num2 = Mathf.RoundToInt((float)width * num);
				float num3 = (float)worldSize / (float)width;
				materialFogdecal.SetTexture("_RT", rt);
				materialFogdecal.SetFloat("_Scale", (float)mapSize / brushSize);
				materialFogdecal.SetVector("_DepthUV", (new Vector2(vector.x, vector.y) - Vector2.one * brushSize + Vector2.one * ((float)worldSize / 2f)) / worldSize);
				if (fogmap.depth == null)
				{
					Map map = (from x in Map.All()
						where x.fogmap == fogmap
						select x).First();
					materialFogdecal.SetInt("_Flat", 1);
					materialFogdecal.SetTexture("_TextureDepth", map.main);
				}
				else
				{
					materialFogdecal.SetInt("_Flat", 0);
					materialFogdecal.SetTexture("_TextureDepth", fogmap.depth);
					materialFogdecal.SetFloat("_YPosNormalized", 1f - Mathf.Clamp01(Mathf.Abs(pos.y - 150f) / 750f));
					materialFogdecal.SetFloat("_YOffset", 0.1f * Mathf.Lerp(0.5f, 1f, DayNightCycle.main.GetLightScalar()));
				}
				vector.x = Mathf.Round((vector.x / num3 + (float)(width / 2)) * num - (float)(width / 2));
				vector.y = Mathf.Round(((float)width - (vector.y / num3 + (float)(width / 2))) * num - (float)(width / 2));
				RenderTexture.active = rt;
				GL.PushMatrix();
				GL.LoadPixelMatrix(0f, num2, num2, 0f);
				Graphics.DrawTexture(new Rect(vector.x, vector.y, width, width), brush, materialFogdecal);
				GL.PopMatrix();
				RenderTexture.active = null;
			}
		}

		public void AlignToPlayer()
		{
			Vector2 vector = Player.main.transform.position.XZ();
			scrollView.normalizedPosition = new Vector2((vector.x + (float)worldSize * 0.5f) / (float)worldSize, (vector.y + (float)worldSize * 0.5f) / (float)worldSize);
		}

		private void Run()
		{
			PDA pDA = Player.main.GetPDA();
			if (!pDA)
			{
				return;
			}
			uGUI_OptionsPanel uGUI_OptionsPanel = (from x in IngameMenu.main.GetComponentsInChildren<uGUI_OptionsPanel>(includeInactive: true)
				where x.name == "Options"
				select x).FirstOrDefault();
			if ((bool)uGUI_OptionsPanel)
			{
				Options item = uGUI_OptionsPanel.gameObject.GetComponent<Options>() ?? uGUI_OptionsPanel.gameObject.AddComponent<Options>();
				destroyOnUnload.Add(item);
			}
			pDA.Open();
			uGUI_PingTab pingTab = pDA.ui.GetTab(PDATab.Ping) as uGUI_PingTab;
			pDA.Close();
			UWE.Utils.lockCursor = true;
			if ((bool)pingTab)
			{
				LoadAssetBundle();
				LoadAssets();
				font = pDA.ui.backButtonText.font;
				GameObject pingContainer = pingTab.transform.Find("Content").gameObject;
				pingContainer.SetActive(value: false);
				mapContainer = UnityEngine.Object.Instantiate(prefabMapContainer);
				destroyOnUnload.Add(mapContainer);
				mapContainer.transform.SetParent(pingTab.transform, worldPositionStays: false);
				mapContainer.SetActive(value: false);
				mapContainer.AddComponent<Container>().controller = this;
				pingTab.content = mapContainer;
				Text component = UnityEngine.Object.Instantiate(pingTab.pingManagerLabel.gameObject).GetComponent<Text>();
				component.text = "MAP";
				component.transform.SetParent(mapContainer.transform, worldPositionStays: false);
				toggleMap = UnityEngine.Object.Instantiate(prefabToogleMap).GetComponent<Button>();
				destroyOnUnload.Add(toggleMap.gameObject);
				toggleMap.transform.SetParent(pingTab.content.transform, worldPositionStays: false);
				toggleMap.transform.localPosition = new Vector2(490f, 325f);
				toggleMap.image.sprite = allSprites.Where((Sprite x) => x.name == "togglemap_on").FirstOrDefault();
				toggleMap.onClick.AddListener(delegate
				{
					if (toggleMap.image.sprite.name == "togglemap_on")
					{
						pingTab.content.gameObject.SetActive(value: false);
						pingTab.content = pingContainer;
						pingTab.content.gameObject.SetActive(value: true);
						toggleMap.transform.SetParent(pingTab.content.transform, worldPositionStays: false);
						toggleMap.image.sprite = allSprites.Where((Sprite x) => x.name == "togglemap_off").FirstOrDefault();
					}
					else
					{
						pingTab.content.gameObject.SetActive(value: false);
						pingTab.content = mapContainer;
						pingTab.content.gameObject.SetActive(value: true);
						toggleMap.transform.SetParent(pingTab.content.transform, worldPositionStays: false);
						toggleMap.image.sprite = allSprites.Where((Sprite x) => x.name == "togglemap_on").FirstOrDefault();
					}
				});
				GameObject gameObject = UnityEngine.Object.Instantiate(prefabMapSwitcher);
				gameObject.transform.SetParent(mapContainer.transform, worldPositionStays: false);
				gameObject.transform.localPosition = new Vector2(-525f, 310f);
				mapSwitcher = gameObject.GetComponent<Button>();
				mapSwitcher.gameObject.SetActive(value: false);
				GameObject gameObject2 = UnityEngine.Object.Instantiate(prefabMapSwitchPanel);
				gameObject2.transform.SetParent(mapContainer.transform, worldPositionStays: false);
				gameObject2.transform.localPosition = new Vector2(-525f, 290f);
				mapSwitchPanel = gameObject2.GetComponent<RectTransform>();
				mapSwitchPanel.gameObject.SetActive(value: false);
				GameObject gameObject3 = UnityEngine.Object.Instantiate(prefabCoord);
				gameObject3.transform.SetParent(mapContainer.transform, worldPositionStays: false);
				gameObject3.transform.localPosition = new Vector2(-225f, 310f);
				coord = gameObject3.GetComponentInChildren<Text>();
				coord.font = font;
				coord.fontSize = 14;
				scrollView = mapContainer.GetComponentInChildren<ScrollRect>();
				RectTransform component2 = scrollView.gameObject.GetComponent<RectTransform>();
				component2.sizeDelta = new Vector2(1050f, 700f);
				component2.localPosition = new Vector2(0f, -70f);
				ResetAndResizeTransform(scrollView.content, new Vector2(mapSize, mapSize));
				scrollView.normalizedPosition = new Vector2(0.5f, 0.5f);
				mapImage = scrollView.content.GetComponent<RawImage>();
				iconsContainer = scrollView.content.Find("Icons").gameObject;
				backgroundContainer = scrollView.content.Find("Background").gameObject;
				playerIconGO = new GameObject("IconPlayer");
				playerIconGO.transform.SetParent(iconsContainer.transform, worldPositionStays: false);
				playerImage = playerIconGO.AddComponent<Image>();
				playerImage.sprite = allSprites.Where((Sprite x) => x.name == "icon_player").FirstOrDefault();
				RectTransform rectTransform = playerImage.rectTransform;
				ResetAndResizeTransform(rectTransform, new Vector2(24f, 24f));
				rectTransform.localPosition = Player.main.transform.position.XZ() / mapScale;
				SimpleTooltip simpleTooltip = playerIconGO.AddComponent<SimpleTooltip>();
				simpleTooltip.translate = true;
				simpleTooltip.text = "Player";
				GameObject gameObject4 = UnityEngine.Object.Instantiate(prefabScanPanel);
				gameObject4.gameObject.SetActive(value: false);
				gameObject4.transform.SetParent(scrollView.content, worldPositionStays: false);
				scanPanel = gameObject4.GetComponent<RectTransform>();
				scanList = scanPanel.GetComponentInChildren<ScrollRect>().content;
				EventTrigger eventTrigger = scanPanel.gameObject.AddComponent<EventTrigger>();
				EventTrigger.Entry entry = new EventTrigger.Entry();
				entry = new EventTrigger.Entry();
				entry.eventID = EventTriggerType.PointerExit;
				entry.callback.AddListener(delegate
				{
					scanPanel.gameObject.SetActive(value: false);
				});
				eventTrigger.triggers.Add(entry);
				pingMapIconList.Clear();
				Dictionary<int, PingInstance>.Enumerator enumerator = PingManager.GetEnumerator();
				while (enumerator.MoveNext())
				{
					int key = enumerator.Current.Key;
					PingInstance value = enumerator.Current.Value;
					pingMapIconList.Add(key, CreatePingMapIcon(value));
				}
				LoadMapIcons();
				ReloadMaps();
				ApplySettings();
				isStarted = true;
				Logger.Write("Started");
			}
		}

		public void RescaleIcons()
		{
			if (isStarted && (bool)iconsContainer)
			{
				playerIconGO.transform.localScale = Vector3.one * Settings.iconsScale;
				mapRoomMapIconList.ForEach(delegate(KeyValuePair<MapRoomFunctionality, MapRoomMapIcon> x)
				{
					x.Value.Rescale();
				});
				pingMapIconList.ForEach(delegate(KeyValuePair<int, PingMapIcon> x)
				{
					x.Value.Rescale();
				});
				foreach (TechType key in techMapIconList.Keys)
				{
					techMapIconList[key].Values.ForEach(delegate(TechMapIcon x)
					{
						x.Rescale();
					});
				}
			}
		}

		public void UpdateIcons()
		{
			if (!isStarted || !iconsContainer)
			{
				return;
			}
			if (Inventory.main.equipment.GetCount(TechType.Compass) > 0)
			{
				playerImage.transform.eulerAngles = new Vector3(0f, 0f, 0f - Player.main.camRoot.transform.rotation.eulerAngles.y);
				if (playerImage.sprite.name != "icon_player_compass")
				{
					playerImage.sprite = allSprites.Where((Sprite x) => x.name == "icon_player_compass").FirstOrDefault();
				}
			}
			else
			{
				playerImage.transform.localEulerAngles = Vector3.zero;
				if (playerImage.sprite.name != "icon_player")
				{
					playerImage.sprite = allSprites.Where((Sprite x) => x.name == "icon_player").FirstOrDefault();
				}
			}
			playerIconGO.transform.localPosition = Player.main.transform.position.XZ() / mapScale;
			playerIconGO.transform.SetAsLastSibling();
			availableTechTypes.Clear();
			MapRoomFunctionality[] array = UnityEngine.Object.FindObjectsOfType<MapRoomFunctionality>();
			Dictionary<MapRoomFunctionality, MapRoomMapIcon> dictionary = new Dictionary<MapRoomFunctionality, MapRoomMapIcon>(array.Length);
			MapRoomFunctionality[] array2 = array;
			foreach (MapRoomFunctionality mapRoomFunctionality in array2)
			{
				if (!mapRoomMapIconList.ContainsKey(mapRoomFunctionality))
				{
					dictionary.Add(mapRoomFunctionality, CreateMapRoomMapIcon(mapRoomFunctionality));
				}
				else
				{
					dictionary.Add(mapRoomFunctionality, mapRoomMapIconList[mapRoomFunctionality]);
				}
				MapRoomMapIcon mapRoomMapIcon = dictionary[mapRoomFunctionality];
				if (!mapRoomMapIcon.isEnabled)
				{
					mapRoomMapIcon.active = false;
					continue;
				}
				mapRoomMapIcon.active = true;
				float scanRange = mapRoomFunctionality.GetScanRange();
				bool flag = scanRange > (Player.main.transform.position - mapRoomFunctionality.transform.position).magnitude;
				ResetAndResizeTransform(mapRoomMapIcon.scanCircle.rectTransform, Vector2.one * (scanRange / mapScale * 2f));
				if (mapRoomFunctionality.typeToScan != TechType.None && flag)
				{
					mapRoomMapIcon.scanCircle.enabled = true;
					mapRoomMapIcon.scanCircle.sprite = allSprites.Where((Sprite x) => x.name == "scan_circle_active").FirstOrDefault();
				}
				else
				{
					mapRoomMapIcon.scanCircle.enabled = false;
				}
				mapRoomMapIcon.scanCircle.transform.localPosition = mapRoomFunctionality.transform.position.XZ() / mapScale;
				mapRoomMapIcon.image.transform.localPosition = mapRoomFunctionality.transform.position.XZ() / mapScale;
			}
			foreach (MapRoomFunctionality key2 in mapRoomMapIconList.Keys)
			{
				if (!dictionary.ContainsKey(key2))
				{
					UnityEngine.Object.Destroy(mapRoomMapIconList[key2].image.gameObject);
					UnityEngine.Object.Destroy(mapRoomMapIconList[key2].scanCircle.gameObject);
				}
			}
			mapRoomMapIconList = dictionary;
			Dictionary<int, PingMapIcon> dictionary2 = new Dictionary<int, PingMapIcon>(pingMapIconList.Count);
			Dictionary<int, PingInstance>.Enumerator enumerator2 = PingManager.GetEnumerator();
			while (enumerator2.MoveNext())
			{
				int key = enumerator2.Current.Key;
				PingInstance value = enumerator2.Current.Value;
				PingMapIcon pingMapIcon;
				if (!pingMapIconList.ContainsKey(key))
				{
					dictionary2.Add(key, CreatePingMapIcon(value));
					pingMapIcon = dictionary2[key];
				}
				else
				{
					dictionary2.Add(key, pingMapIconList[key]);
					pingMapIcon = dictionary2[key];
					pingMapIcon.image.transform.localPosition = value.origin.position.XZ() / mapScale;
					pingMapIcon.image.color = ((!value.visible) ? Color.white : PingManager.colorOptions[value.colorIndex]);
					pingMapIcon.tooltip.text = value.GetLabel();
					pingMapIcon.Refresh(force: false);
				}
				if ((value.origin.transform.position - Player.main.transform.position).sqrMagnitude < 400f || !pingMapIcon.isEnabled)
				{
					pingMapIcon.active = false;
				}
				else if (value.pingType == PingType.MapRoomCamera)
				{
					if (!value.visible)
					{
						pingMapIcon.active = false;
						continue;
					}
					bool flag2 = false;
					array2 = array;
					foreach (MapRoomFunctionality mapRoomFunctionality2 in array2)
					{
						if ((value.origin.transform.position - mapRoomFunctionality2.transform.position).sqrMagnitude < 400f)
						{
							flag2 = true;
							pingMapIcon.active = false;
							break;
						}
					}
					if (!flag2)
					{
						pingMapIcon.active = true;
					}
				}
				else
				{
					pingMapIcon.active = true;
				}
			}
			foreach (int key3 in pingMapIconList.Keys)
			{
				if (!dictionary2.ContainsKey(key3))
				{
					UnityEngine.Object.Destroy(pingMapIconList[key3].image.gameObject);
				}
			}
			pingMapIconList = dictionary2;
			foreach (Dictionary<string, TechMapIcon> value2 in techMapIconList.Values)
			{
				foreach (TechMapIcon mapIcon in value2.Values)
				{
					if (!mapIcon.isEnabled || !Map.BiomeIsCurrentInMaps(mapIcon.biome))
					{
						mapIcon.active = false;
					}
					else
					{
						IEnumerable<TechMapIcon> source = value2.Values.Where((TechMapIcon x) => x != mapIcon && (x.position - mapIcon.position).sqrMagnitude < 625f);
						if (source.Count() > 0)
						{
							if (source.Where((TechMapIcon x) => x.image.gameObject.activeSelf).Count() > 0)
							{
								mapIcon.active = false;
							}
							else
							{
								mapIcon.active = true;
								mapIcon.image.transform.localPosition = mapIcon.position.XZ() / mapScale;
							}
						}
						else
						{
							mapIcon.active = true;
							mapIcon.image.transform.localPosition = mapIcon.position.XZ() / mapScale;
						}
					}
				}
			}
		}

		private void ScanResources(TechType type)
		{
			if (!techMapIconList.TryGetValue(type, out Dictionary<string, TechMapIcon> value))
			{
				return;
			}
			float num = 75f;
			float num2 = num * num;
			if (type == TechType.PrecursorPrisonArtifact1)
			{
				PrecursorVentBase[] array = UnityEngine.Object.FindObjectsOfType<PrecursorVentBase>();
				if (array == null)
				{
					return;
				}
				PrecursorVentBase[] array2 = array;
				foreach (PrecursorVentBase precursorVentBase in array2)
				{
					if ((Player.main.transform.position - precursorVentBase.transform.position).sqrMagnitude <= num2 && !value.ContainsKey(precursorVentBase.transform.position.ToString()))
					{
						TechMapIcon techMapIcon = new TechMapIcon
						{
							colorIndex = 0,
							type = type,
							uniqueId = precursorVentBase.transform.position.ToString(),
							position = precursorVentBase.transform.position,
							visible = false,
							biome = LargeWorld.main.GetBiome(precursorVentBase.transform.position)
						};
						value.Add(techMapIcon.uniqueId, CreateTechMapIcon(techMapIcon));
					}
				}
				foreach (string item in value.Keys.ToList())
				{
					TechMapIcon mapIcon2 = value[item];
					if ((Player.main.transform.position - mapIcon2.position).sqrMagnitude <= num2 * 0.5f && (array.Where((PrecursorVentBase x) => x.transform.position == mapIcon2.position).Count() == 0 || mapIcon2.biome == ""))
					{
						mapIcon2.Destroy();
						value.Remove(item);
					}
				}
			}
			else
			{
				List<ResourceTracker.ResourceInfo> list = new List<ResourceTracker.ResourceInfo>();
				ResourceTracker.GetNodes(Player.main.transform.position, num, type, list);
				if (list != null)
				{
					foreach (ResourceTracker.ResourceInfo item2 in list)
					{
						if (!value.ContainsKey(item2.uniqueId))
						{
							TechMapIcon techMapIcon2 = new TechMapIcon
							{
								colorIndex = 0,
								type = item2.techType,
								uniqueId = item2.uniqueId,
								position = item2.position,
								visible = true,
								biome = LargeWorld.main.GetBiome(item2.position)
							};
							value.Add(item2.uniqueId, CreateTechMapIcon(techMapIcon2));
						}
						else if (item2.position != value[item2.uniqueId].position)
						{
							value[item2.uniqueId].position = item2.position;
						}
					}
					foreach (string item3 in value.Keys.ToList())
					{
						TechMapIcon mapIcon = value[item3];
						if ((Player.main.transform.position - mapIcon.position).sqrMagnitude <= num2 * 0.5f && (list.Where((ResourceTracker.ResourceInfo x) => x.uniqueId == mapIcon.uniqueId).Count() == 0 || mapIcon.biome == ""))
						{
							mapIcon.Destroy();
							value.Remove(item3);
						}
					}
				}
			}
		}

		private void SaveMapIcons()
		{
			string path = Path.Combine(SaveDir, "mapicons.bin");
			try
			{
				using (BinaryWriter binaryWriter = new BinaryWriter(File.Open(path, FileMode.Create)))
				{
					Dictionary<string, TechMapIcon> dictionary = new Dictionary<string, TechMapIcon>();
					TechType[] array = techMapIconsType;
					foreach (TechType key in array)
					{
						if (techMapIconList.TryGetValue(key, out Dictionary<string, TechMapIcon> value))
						{
							foreach (KeyValuePair<string, TechMapIcon> item in value)
							{
								dictionary.Add(item.Key, item.Value);
							}
						}
					}
					if (dictionary.Count > 0)
					{
						binaryWriter.Write(2);
						binaryWriter.Write(dictionary.Count);
						foreach (TechMapIcon value2 in dictionary.Values)
						{
							binaryWriter.Write(value2.uniqueId);
							binaryWriter.Write(value2.position);
							binaryWriter.Write((int)value2.type);
							binaryWriter.Write(value2.colorIndex);
							binaryWriter.Write(value2.visible);
							binaryWriter.Write(value2.biome);
						}
					}
				}
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
				Logger.Show("Error saving map icons");
			}
		}

		private void LoadMapIcons()
		{
			foreach (TechType key2 in techMapIconList.Keys)
			{
				techMapIconList[key2].Values.ForEach(delegate(TechMapIcon x)
				{
					x.Destroy();
				});
			}
			techMapIconList.Clear();
			TechType[] array = techMapIconsType;
			foreach (TechType key in array)
			{
				techMapIconList.Add(key, new Dictionary<string, TechMapIcon>());
			}
			Dictionary<string, TechMapIcon> dictionary = new Dictionary<string, TechMapIcon>();
			string text = Path.Combine(SaveDir, "mapicons.bin");
			if (File.Exists(text) && new FileInfo(text).Length > 8)
			{
				try
				{
					using (BinaryReader binaryReader = new BinaryReader(File.Open(text, FileMode.Open)))
					{
						int num = binaryReader.ReadInt32();
						int num2 = binaryReader.ReadInt32();
						for (int j = 0; j < num2; j++)
						{
							TechMapIcon techMapIcon = null;
							switch (num)
							{
							case 1:
								techMapIcon = new TechMapIcon
								{
									uniqueId = binaryReader.ReadString(),
									position = binaryReader.ReadVector3(),
									type = (TechType)binaryReader.ReadInt32(),
									colorIndex = binaryReader.ReadInt32(),
									visible = binaryReader.ReadBoolean()
								};
								break;
							case 2:
								techMapIcon = new TechMapIcon
								{
									uniqueId = binaryReader.ReadString(),
									position = binaryReader.ReadVector3(),
									type = (TechType)binaryReader.ReadInt32(),
									colorIndex = binaryReader.ReadInt32(),
									visible = binaryReader.ReadBoolean(),
									biome = binaryReader.ReadString()
								};
								break;
							}
							if (techMapIcon != null)
							{
								dictionary.Add(techMapIcon.uniqueId, techMapIcon);
							}
						}
						array = techMapIconsType;
						foreach (TechType techType in array)
						{
							foreach (TechMapIcon value in dictionary.Values)
							{
								if (value.type == techType)
								{
									CreateTechMapIcon(value);
									techMapIconList[techType].Add(value.uniqueId, value);
								}
							}
						}
					}
				}
				catch (Exception exception)
				{
					Debug.LogException(exception);
					Logger.Show("Error loading map icons");
				}
			}
		}

		private TechMapIcon CreateTechMapIcon(TechMapIcon techMapIcon)
		{
			if (!techMapIcon.image)
			{
				GameObject gameObject = new GameObject("TechMapIcon");
				gameObject.transform.SetParent(backgroundContainer.transform, worldPositionStays: false);
				Image image = gameObject.AddComponent<Image>();
				image.sprite = allSprites.Where((Sprite x) => x.name == techMapIcon.type.AsString()).FirstOrDefault();
				RectTransform rectTransform = image.rectTransform;
				if ((bool)image.sprite)
				{
					ResetAndResizeTransform(rectTransform, new Vector2(image.sprite.rect.width, image.sprite.rect.height) / (Mathf.Max(image.sprite.rect.width, image.sprite.rect.height) / 30f));
				}
				rectTransform.localPosition = techMapIcon.position.XZ() / mapScale;
				techMapIcon.image = image;
				techMapIcon.Rescale();
				EventTrigger eventTrigger = gameObject.AddComponent<EventTrigger>();
				EventTrigger.Entry entry = new EventTrigger.Entry();
				entry.eventID = EventTriggerType.PointerClick;
				entry.callback.AddListener(delegate(BaseEventData data)
				{
					PointerEventData pointerEventData = data as PointerEventData;
					if (pointerEventData.button == PointerEventData.InputButton.Left)
					{
						techMapIcon.ToggleVisible();
					}
					else if (pointerEventData.button == PointerEventData.InputButton.Right)
					{
						techMapIcon.ToggleColor();
					}
				});
				eventTrigger.triggers.Add(entry);
				SimpleTooltip simpleTooltip = gameObject.AddComponent<SimpleTooltip>();
				simpleTooltip.translate = false;
				if (techMapIcon.type == TechType.PrecursorPrisonArtifact1)
				{
					simpleTooltip.text = Language.main.Get("PrecursorSurfacePipe");
				}
				else
				{
					simpleTooltip.text = Language.main.Get(techMapIcon.type);
				}
				simpleTooltip.text += ((techMapIcon.position.y < 0f) ? $" ({Mathf.Abs(Mathf.RoundToInt(techMapIcon.position.y))} m)" : "");
			}
			techMapIcon.SetColor(techMapIcon.colorIndex);
			techMapIcon.SetVisible(techMapIcon.visible);
			return techMapIcon;
		}

		private PingMapIcon CreatePingMapIcon(PingInstance ping)
		{
			ping.SetColor(ping.colorIndex);
			GameObject pingGo = new GameObject($"PingMapIcon{ping.GetInstanceID()}");
			pingGo.transform.SetParent(iconsContainer.transform, worldPositionStays: false);
			Image image = pingGo.AddComponent<Image>();
			image.sprite = allSprites.Where((Sprite x) => x.name == "icon_background").FirstOrDefault();
			image.color = PingManager.colorOptions[ping.colorIndex];
			image.raycastTarget = false;
			RectTransform rectTransform = image.rectTransform;
			ResetAndResizeTransform(rectTransform, new Vector2(42f, 42f));
			rectTransform.localPosition = ping.origin.position.XZ() / mapScale;
			SimpleTooltip simpleTooltip = pingGo.AddComponent<SimpleTooltip>();
			simpleTooltip.translate = false;
			simpleTooltip.text = ping.GetLabel();
			GameObject gameObject = new GameObject("Icon");
			gameObject.transform.SetParent(pingGo.transform, worldPositionStays: false);
			uGUI_Icon icon = gameObject.AddComponent<uGUI_Icon>();
			PingMapIcon pingMapIcon = new PingMapIcon
			{
				ping = ping,
				image = image,
				tooltip = simpleTooltip,
				icon = icon
			};
			pingMapIcon.Rescale();
			pingMapIcon.Refresh();
			EventTrigger eventTrigger = pingGo.AddComponent<EventTrigger>();
			EventTrigger.Entry entry = new EventTrigger.Entry();
			entry.eventID = EventTriggerType.PointerEnter;
			entry.callback.AddListener(delegate
			{
				pingGo.transform.SetAsLastSibling();
			});
			eventTrigger.triggers.Add(entry);
			entry = new EventTrigger.Entry();
			entry.eventID = EventTriggerType.PointerClick;
			entry.callback.AddListener(delegate(BaseEventData data)
			{
				PointerEventData pointerEventData = data as PointerEventData;
				if (pointerEventData.button == PointerEventData.InputButton.Left)
				{
					pingMapIcon.ToggleVisible();
				}
				else if (pointerEventData.button == PointerEventData.InputButton.Right)
				{
					pingMapIcon.ToggleColor();
				}
			});
			eventTrigger.triggers.Add(entry);
			return pingMapIcon;
		}

		private MapRoomMapIcon CreateMapRoomMapIcon(MapRoomFunctionality room)
		{
			Sprite itemBg = allSprites.Where((Sprite x) => x.name == "scannerroomUI_listbg").FirstOrDefault();
			Sprite itemBgHover = allSprites.Where((Sprite x) => x.name == "scannerroomUI_listbgMouseOver").FirstOrDefault();
			GameObject scanCircleGO = new GameObject("ScanCircle");
			scanCircleGO.transform.SetParent(backgroundContainer.transform, worldPositionStays: false);
			Image scanCircleImage = scanCircleGO.AddComponent<Image>();
			scanCircleImage.color = new Color(0.7f, 1f, 1f, 0.3f);
			scanCircleImage.raycastTarget = false;
			scanCircleImage.enabled = false;
			StartCoroutine(MapRoomMapIcon.Rotate(scanCircleImage));
			GameObject scannerIconGO = new GameObject("MapRoomMapIcon");
			scannerIconGO.transform.SetParent(iconsContainer.transform, worldPositionStays: false);
			Image image = scannerIconGO.AddComponent<Image>();
			image.sprite = allSprites.Where((Sprite x) => x.name == "icon_background").FirstOrDefault();
			image.raycastTarget = false;
			ResetAndResizeTransform(image.rectTransform, new Vector2(42f, 42f));
			MapRoomMapIcon mapRoomMapIcon = new MapRoomMapIcon
			{
				image = image,
				scanCircle = scanCircleImage,
				room = room
			};
			mapRoomMapIcon.Rescale();
			PingInstance pingInstance = null;
			float num = float.MaxValue;
			float scanRange = room.GetScanRange();
			Dictionary<int, PingInstance>.Enumerator enumerator = PingManager.GetEnumerator();
			while (enumerator.MoveNext())
			{
				_ = enumerator.Current.Key;
				PingInstance value = enumerator.Current.Value;
				if (value.pingType == PingType.MapRoomCamera)
				{
					float magnitude = (value.origin.transform.position - room.transform.position).magnitude;
					if (magnitude < scanRange && magnitude < num)
					{
						num = magnitude;
						pingInstance = value;
					}
				}
			}
			if ((bool)pingInstance)
			{
				mapRoomMapIcon.SetVisible(pingInstance.visible);
				mapRoomMapIcon.SetColor(pingInstance.colorIndex);
			}
			EventTrigger eventTrigger = scannerIconGO.AddComponent<EventTrigger>();
			EventTrigger.Entry entry = new EventTrigger.Entry();
			entry.eventID = EventTriggerType.PointerEnter;
			entry.callback.AddListener(delegate
			{
				if ((bool)room)
				{
					bool flag3 = room.GetScanRange() > (Player.main.transform.position - room.transform.position).magnitude;
					scannerIconGO.transform.SetAsLastSibling();
					scanCircleGO.transform.SetAsLastSibling();
					if (room.typeToScan == TechType.None || !flag3)
					{
						scanCircleImage.enabled = true;
						scanCircleImage.sprite = allSprites.Where((Sprite x) => x.name == "scan_circle").FirstOrDefault();
					}
				}
			});
			eventTrigger.triggers.Add(entry);
			entry = new EventTrigger.Entry();
			entry.eventID = EventTriggerType.PointerExit;
			entry.callback.AddListener(delegate
			{
				if ((bool)room)
				{
					bool flag2 = room.GetScanRange() > (Player.main.transform.position - room.transform.position).magnitude;
					if (room.typeToScan == TechType.None || !flag2)
					{
						scanCircleImage.enabled = false;
					}
				}
			});
			eventTrigger.triggers.Add(entry);
			Action action = delegate
			{
				if (MapIsOpened() && CursorManager.lastRaycast.isValid && CursorManager.lastRaycast.gameObject.transform.IsChildOf(scannerIconGO.transform))
				{
					scanList.transform.DestroyChildren();
					if ((bool)room)
					{
						bool _inRange = room.GetScanRange() > (Player.main.transform.position - room.transform.position).magnitude;
						if (_inRange && Inventory.Get().equipment.GetCount(TechType.MapRoomHUDChip) != 0)
						{
							scanPanel.gameObject.SetActive(value: true);
							scanPanel.transform.localPosition = room.transform.position.XZ() / mapScale;
							if (!availableTechTypes.ContainsKey(room.gameObject))
							{
								HashSet<TechType> hashSet = new HashSet<TechType>();
								ResourceTracker.GetTechTypesInRange(room.transform.position, room.GetScanRange(), hashSet);
								if (room.typeToScan != 0)
								{
									hashSet.Add(room.typeToScan);
								}
								List<TechType> list = hashSet.ToList();
								list.Sort(CompareByName);
								availableTechTypes.Add(room.gameObject, list);
							}
							VerticalLayoutGroup component = scanList.GetComponent<VerticalLayoutGroup>();
							int num4 = Mathf.Max(5, availableTechTypes[room.gameObject].Count);
							scanList.sizeDelta = new Vector2(scanList.sizeDelta.x, (float)num4 * prefabScanPanelItem.GetComponent<LayoutElement>().minHeight + (float)(num4 - 1) * component.spacing + (float)component.padding.top + (float)component.padding.bottom);
							Dictionary<TechType, Button> scanButtonList = new Dictionary<TechType, Button>();
							foreach (TechType techType in availableTechTypes[room.gameObject])
							{
								GameObject gameObject2 = UnityEngine.Object.Instantiate(prefabScanPanelItem);
								gameObject2.transform.SetParent(scanList, worldPositionStays: false);
								Text componentInChildren = gameObject2.GetComponentInChildren<Text>();
								componentInChildren.text = Language.main.Get(techType.AsString());
								componentInChildren.font = font;
								Button component2 = gameObject2.GetComponent<Button>();
								component2.image.sprite = ((techType == room.typeToScan) ? itemBgHover : itemBg);
								component2.spriteState = new SpriteState
								{
									highlightedSprite = ((techType == room.typeToScan) ? null : itemBgHover)
								};
								component2.onClick.AddListener(delegate
								{
									room.StartScanning((room.typeToScan == TechType.None || room.typeToScan != techType) ? techType : TechType.None);
									UpdateGUIState((from x in UnityEngine.Object.FindObjectsOfType<uGUI_MapRoomScanner>()
										where x.mapRoom == room
										select x).FirstOrDefault());
									if (room.typeToScan != TechType.None && _inRange)
									{
										scanCircleImage.enabled = true;
										scanCircleImage.sprite = allSprites.Where((Sprite x) => x.name == "scan_circle_active").FirstOrDefault();
									}
									else
									{
										scanCircleImage.enabled = false;
									}
									foreach (KeyValuePair<TechType, Button> item in scanButtonList)
									{
										bool flag = item.Key == room.typeToScan;
										item.Value.image.sprite = (flag ? itemBgHover : itemBg);
										item.Value.spriteState = new SpriteState
										{
											highlightedSprite = (flag ? null : itemBgHover)
										};
									}
								});
								scanButtonList.Add(techType, component2);
							}
						}
					}
				}
			};
			Keybinding keybinding = scannerIconGO.AddComponent<Keybinding>();
			keybinding.key = (() => settings.scanningKeybinding);
			keybinding.action = action;
			entry = new EventTrigger.Entry();
			entry.eventID = EventTriggerType.PointerClick;
			entry.callback.AddListener(delegate(BaseEventData data)
			{
				PointerEventData pointerEventData = data as PointerEventData;
				if (pointerEventData.button != PointerEventData.InputButton.Middle)
				{
					if (pointerEventData.button == PointerEventData.InputButton.Left)
					{
						mapRoomMapIcon.ToggleVisible();
						Dictionary<int, PingInstance>.Enumerator enumerator2 = PingManager.GetEnumerator();
						while (enumerator2.MoveNext())
						{
							int key = enumerator2.Current.Key;
							PingInstance value2 = enumerator2.Current.Value;
							if (value2.pingType == PingType.MapRoomCamera)
							{
								MapRoomMapIcon mapRoomMapIcon2 = null;
								float num2 = float.MaxValue;
								foreach (MapRoomMapIcon value6 in mapRoomMapIconList.Values)
								{
									float magnitude2 = (value2.origin.transform.position - value6.room.transform.position).magnitude;
									if (magnitude2 < num2 && magnitude2 < value6.room.GetScanRange())
									{
										mapRoomMapIcon2 = value6;
										num2 = magnitude2;
									}
								}
								if (mapRoomMapIcon2 == mapRoomMapIcon && pingMapIconList.TryGetValue(key, out PingMapIcon value3))
								{
									value3?.SetVisible(mapRoomMapIcon.visible);
									UpdateIcons();
								}
							}
						}
					}
					else if (pointerEventData.button == PointerEventData.InputButton.Right)
					{
						mapRoomMapIcon.ToggleColor();
						Dictionary<int, PingInstance>.Enumerator enumerator4 = PingManager.GetEnumerator();
						while (enumerator4.MoveNext())
						{
							int key2 = enumerator4.Current.Key;
							PingInstance value4 = enumerator4.Current.Value;
							if (value4.pingType == PingType.MapRoomCamera)
							{
								MapRoomMapIcon mapRoomMapIcon3 = null;
								float num3 = float.MaxValue;
								foreach (MapRoomMapIcon value7 in mapRoomMapIconList.Values)
								{
									float magnitude3 = (value4.origin.transform.position - value7.room.transform.position).magnitude;
									if (magnitude3 < num3 && magnitude3 < value7.room.GetScanRange())
									{
										mapRoomMapIcon3 = value7;
										num3 = magnitude3;
									}
								}
								if (mapRoomMapIcon3 == mapRoomMapIcon && pingMapIconList.TryGetValue(key2, out PingMapIcon value5))
								{
									value5?.SetColor(mapRoomMapIcon.colorIndex);
								}
							}
						}
					}
				}
			});
			eventTrigger.triggers.Add(entry);
			GameObject gameObject = new GameObject("Icon");
			gameObject.transform.SetParent(scannerIconGO.transform, worldPositionStays: false);
			Image image2 = gameObject.AddComponent<Image>();
			image2.sprite = allSprites.Where((Sprite x) => x.name == "MapRoom").FirstOrDefault();
			image2.color = Color.black;
			RectTransform rectTransform = image2.rectTransform;
			if ((bool)image2.sprite)
			{
				rectTransform.sizeDelta = new Vector2(image2.sprite.rect.width, image2.sprite.rect.height) / (Mathf.Max(image2.sprite.rect.width, image2.sprite.rect.height) / 24f);
			}
			rectTransform.localPosition = Vector3.zero;
			SimpleTooltip simpleTooltip = gameObject.AddComponent<SimpleTooltip>();
			simpleTooltip.translate = true;
			simpleTooltip.text = "BaseMapRoom";
			return mapRoomMapIcon;
		}

		private static int CompareByName(TechType a, TechType b)
		{
			Language main = Language.main;
			string strA = main.Get(a.AsString());
			string strB = main.Get(b.AsString());
			return string.Compare(strA, strB, StringComparison.OrdinalIgnoreCase);
		}

		private void ResetAndResizeTransform(RectTransform rect, Vector2 size)
		{
			rect.pivot = new Vector2(0.5f, 0.5f);
			rect.anchorMin = new Vector2(0.5f, 0.5f);
			rect.anchorMax = new Vector2(0.5f, 0.5f);
			rect.sizeDelta = size;
			rect.localPosition = Vector2.zero;
		}

		private void OnGUI()
		{
		}

		private void UpdateGUIState(uGUI_MapRoomScanner scanner)
		{
			if (!scanner)
			{
				return;
			}
			TechType activeTechType = scanner.mapRoom.GetActiveTechType();
			scanner.resourceListRoot.SetActive(activeTechType == TechType.None);
			scanner.scanningRoot.SetActive(activeTechType != TechType.None);
			if (activeTechType != 0)
			{
				Atlas.Sprite withNoDefault = SpriteManager.GetWithNoDefault(activeTechType);
				if (withNoDefault != null)
				{
					scanner.scanningIcon.sprite = withNoDefault;
					scanner.scanningIcon.enabled = true;
				}
				else
				{
					scanner.scanningIcon.enabled = false;
				}
				scanner.activeTechTypeLabel.text = Language.main.Get(activeTechType.AsString());
				Utils.PlayFMODAsset(scanner.startScanningSound, Player.main.transform);
			}
			else
			{
				Utils.PlayFMODAsset(scanner.cancelScanningSound, Player.main.transform);
			}
		}

		public void CheckFogmapTexture()
		{
			Material material = materialMapColored;
			if (currentMap.fogmap != null)
			{
				material.SetTexture("_MainTex", currentMap.fogmap.isActive ? ((Texture)currentMap.fogmap.rt) : ((Texture)currentMap.fogmap.temp));
			}
			else
			{
				material.SetTexture("_MainTex", null);
			}
		}

		public void ApplySettings()
		{
			Map.CheckBiome();
			CheckFogmapTexture();
			Material material = materialMapColored;
			material.SetTexture("_TextureMap", currentMap.main);
			material.SetTexture("_TextureOverlay", currentMap.overlay);
			material.SetTexture("_TextureDepth", currentMap.depth);
			material.SetTexture("_TexturePalette", currentMap.palette);
			material.SetFloat("_Transparency", settings.mapTransparency);
			material.SetFloat("_TransparencyFog", settings.fogTransparency);
			if (Settings.fogStyle == FogStyle.Off || currentMap.fogmap == null)
			{
				material.SetInt("_DisableFog", 1);
			}
			else
			{
				material.SetInt("_DisableFog", 0);
			}
			mapImage.enabled = false;
			mapImage.material = material;
			mapImage.enabled = true;
			RescaleIcons();
			if (coord.transform.parent.gameObject.activeSelf != Settings.showCoordinates)
			{
				coord.transform.parent.gameObject.SetActive(Settings.showCoordinates);
			}
		}

		public void ReloadMaps()
		{
			mapSwitcher.GetComponentInChildren<Text>().font = font;
			Map.LoadMaps();
			Map map = currentMap = Map.All().First();
			Map.CheckBiome(autoSwitchMap: true);
			if (currentMap == map)
			{
				SwitchMap(map);
			}
			if (Map.All().Count > 1)
			{
				mapSwitcher.onClick.RemoveAllListeners();
				mapSwitcher.onClick.AddListener(delegate
				{
					VerticalLayoutGroup component2 = mapSwitchPanel.GetComponent<VerticalLayoutGroup>();
					int count = Map.All().Count;
					mapSwitchPanel.sizeDelta = new Vector2(mapSwitchPanel.sizeDelta.x, (float)count * prefabMapSwitchItem.GetComponent<LayoutElement>().minHeight + (float)(count - 1) * component2.spacing + (float)component2.padding.top + (float)component2.padding.bottom);
					mapSwitchPanel.gameObject.SetActive(!mapSwitchPanel.gameObject.activeSelf);
					Button[] componentsInChildren = mapSwitchPanel.GetComponentsInChildren<Button>();
					if (componentsInChildren.Length != 0 && currentMap != null)
					{
						componentsInChildren[Map.All().FindIndex((Map x) => x == currentMap)].Select();
					}
				});
				mapSwitchPanel.DestroyChildren();
				foreach (Map o in Map.All())
				{
					GameObject gameObject = UnityEngine.Object.Instantiate(prefabMapSwitchItem);
					gameObject.transform.SetParent(mapSwitchPanel, worldPositionStays: false);
					Button component = gameObject.GetComponent<Button>();
					component.onClick.AddListener(delegate
					{
						SwitchMap(o);
					});
					Text componentInChildren = component.GetComponentInChildren<Text>();
					componentInChildren.font = font;
					componentInChildren.text = o.name;
				}
				mapSwitcher.gameObject.SetActive(value: true);
			}
			else
			{
				mapSwitcher.gameObject.SetActive(value: false);
				mapSwitchPanel.gameObject.SetActive(value: false);
			}
		}

		public void SwitchMap(Map map)
		{
			currentMap = map;
			mapSwitcher.GetComponentInChildren<Text>().text = map.name;
			ApplySettings();
			if (MapIsOpened())
			{
				UpdateIcons();
			}
			Logger.Write($"Map switched to {map.name}");
			if (map.fogmapId == "world")
			{
				lastDefaultMap = map;
			}
		}
	}
}
