using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SubnauticaMap
{
	public class Map
	{
		public string mapId = "";

		public string fogmapId = "";

		public string filepath = "";

		public string filepath_overlay = "";

		public string filepath_depth = "";

		public string name = "";

		public bool colorize;

		public int[] color = new int[0];

		public bool hideTrackedIcons;

		public string[] biome = new string[0];

		[NonSerialized]
		public Texture2D main;

		[NonSerialized]
		public Texture2D overlay;

		[NonSerialized]
		public Texture2D depth;

		[NonSerialized]
		public bool isActive;

		[NonSerialized]
		public Fogmap fogmap;

		[NonSerialized]
		public Texture2D palette;

		private long filesize;

		private long overlayFilesize;

		private long depthFilesize;

		private static List<Map> maps = new List<Map>();

		public static List<Map> All()
		{
			return maps;
		}

		public bool InBiome(string str)
		{
			if (biome.Length != 0)
			{
				string[] array = biome;
				foreach (string text in array)
				{
					if (str == text || (text.Length > 0 && str.StartsWith(text)))
					{
						return true;
					}
				}
			}
			return false;
		}

		public static bool BiomeIsCurrentInMaps(string str)
		{
			bool flag = false;
			foreach (Map map in maps)
			{
				if (map.biome.Length != 0)
				{
					string[] array = map.biome;
					foreach (string text in array)
					{
						if (str == text || (text.Length > 0 && str.StartsWith(text)))
						{
							flag = true;
							if (map == Controller.Instance.CurrentMap)
							{
								return true;
							}
						}
					}
				}
			}
			if (!flag && Controller.Instance.CurrentMap.biome.Length == 0)
			{
				return true;
			}
			return false;
		}

		public static bool BiomeIsCave(string biome)
		{
			string[] array = new string[8]
			{
				"LostRiver",
				"ILZ",
				"LavaFalls",
				"LavaLakes",
				"LavaPit",
				"Prison",
				"JellyshroomCaves",
				"jellyshroom"
			};
			foreach (string value in array)
			{
				if (biome.StartsWith(value))
				{
					return true;
				}
			}
			return false;
		}

		public static void CheckBiome(bool autoSwitchMap = false)
		{
			List<Map> list = new List<Map>(0);
			List<Map> list2 = new List<Map>(0);
			List<Map> list3 = new List<Map>(0);
			MapRoomCamera camera = uGUI_CameraDrone.main.GetCamera();
			string text = (!(camera != null)) ? Player.main.GetBiomeString() : LargeWorld.main.GetBiome(camera.transform.position);
			bool flag = BiomeIsCave(text);
			foreach (Map map in maps)
			{
				if (map.biome.Length == 0)
				{
					if (!flag)
					{
						if (!map.isActive)
						{
							map.isActive = true;
							if (autoSwitchMap)
							{
								list3.Add(map);
							}
						}
					}
					else if (map.isActive)
					{
						map.isActive = false;
					}
				}
				else
				{
					int num = 0;
					int num2 = 0;
					string[] array = map.biome;
					foreach (string text2 in array)
					{
						if (text == text2)
						{
							num2++;
						}
						else if (text2.Length > 0 && text.StartsWith(text2))
						{
							num++;
						}
					}
					if (num > 0 || num2 > 0)
					{
						if (!map.isActive)
						{
							map.isActive = true;
							if (autoSwitchMap)
							{
								if (num2 > 0)
								{
									list.Add(map);
								}
								else if (num > 0)
								{
									list2.Add(map);
								}
							}
						}
					}
					else if (map.isActive)
					{
						map.isActive = false;
					}
				}
			}
			Fogmap.CheckState();
			if (!autoSwitchMap)
			{
				return;
			}
			if (list.Count > 0)
			{
				if (!list.Contains(Controller.Instance.CurrentMap))
				{
					Controller.Instance.SwitchMap(list.First());
				}
			}
			else if (list2.Count > 0)
			{
				if (!list2.Contains(Controller.Instance.CurrentMap))
				{
					Controller.Instance.SwitchMap(list2.First());
				}
			}
			else if (list3.Count > 0 && !list3.Contains(Controller.Instance.CurrentMap))
			{
				Controller.Instance.SwitchMap(list3.OrderByDescending((Map x) => x == Controller.Instance.lastDefaultMap).First());
			}
		}

		private static List<Map> CreateDefaultMaps()
		{
			List<Map> list = new List<Map>();
			Map map = new Map
			{
				name = "Topographic",
				mapId = "default_world",
				fogmapId = "world",
				main = Controller.Instance.Assets.LoadAsset<Texture2D>("world_base"),
				depth = Controller.Instance.Assets.LoadAsset<Texture2D>("world_depth"),
				palette = Controller.Instance.Assets.LoadAsset<Texture2D>("world_palette")
			};
			map.fogmap = Fogmap.Create(map);
			list.Add(map);
			map = new Map
			{
				name = "Biome",
				mapId = "default_biome",
				fogmapId = "world",
				main = Controller.Instance.Assets.LoadAsset<Texture2D>("biome_base")
			};
			map.fogmap = Fogmap.Create(map);
			list.Add(map);
			Map map2 = new Map();
			map2.name = "Jellyshroom";
			map2.mapId = "default_jellyshroom";
			map2.fogmapId = "jellyshroom";
			map2.main = Controller.Instance.Assets.LoadAsset<Texture2D>("jellyshroom_base");
			map2.overlay = Controller.Instance.Assets.LoadAsset<Texture2D>("jellyshroom_overlay");
			map2.biome = new string[2]
			{
				"JellyshroomCaves",
				"jellyshroom"
			};
			map = map2;
			map.fogmap = Fogmap.Create(map);
			list.Add(map);
			map2 = new Map();
			map2.name = "Lost River";
			map2.mapId = "default_lostriver";
			map2.fogmapId = "lostriver";
			map2.main = Controller.Instance.Assets.LoadAsset<Texture2D>("lostriver_base");
			map2.overlay = Controller.Instance.Assets.LoadAsset<Texture2D>("lostriver_overlay");
			map2.biome = new string[1]
			{
				"LostRiver"
			};
			map = map2;
			map.fogmap = Fogmap.Create(map);
			list.Add(map);
			map2 = new Map();
			map2.name = "Inactive Lava Zone";
			map2.mapId = "default_ilz";
			map2.fogmapId = "ilz";
			map2.main = Controller.Instance.Assets.LoadAsset<Texture2D>("ilz_base");
			map2.overlay = Controller.Instance.Assets.LoadAsset<Texture2D>("ilz_overlay");
			map2.biome = new string[1]
			{
				"ILZ"
			};
			map = map2;
			map.fogmap = Fogmap.Create(map);
			list.Add(map);
			map2 = new Map();
			map2.name = "Active Lava Zone";
			map2.mapId = "default_alz";
			map2.fogmapId = "alz";
			map2.main = Controller.Instance.Assets.LoadAsset<Texture2D>("alz_base");
			map2.overlay = Controller.Instance.Assets.LoadAsset<Texture2D>("alz_overlay");
			map2.biome = new string[4]
			{
				"LavaFalls",
				"LavaLakes",
				"LavaPit",
				"Prison"
			};
			map = map2;
			map.fogmap = Fogmap.Create(map);
			list.Add(map);
			return list;
		}

		public static void LoadMaps()
		{
			List<Map> result = CreateDefaultMaps();
			string text = Path.Combine(Controller.Dir, "maps");
			if (Directory.Exists(text))
			{
				Load(text, result);
				string[] directories = Directory.GetDirectories(text);
				for (int i = 0; i < directories.Length; i++)
				{
					Load(directories[i], result);
				}
			}
			maps = result;
		}

		private static void Load(string dir, List<Map> result)
		{
			if (new DirectoryInfo(dir).Name == "example")
			{
				return;
			}
			string[] files = Directory.GetFiles(dir);
			foreach (string path in files)
			{
				if (Path.GetExtension(path) == ".json")
				{
					Logger.Write($"Found map settings '{Path.GetFileName(path)}'");
					Map map = new Map();
					try
					{
						map = JsonUtility.FromJson<Map>(File.ReadAllText(path));
						string text = Path.Combine(dir, map.filepath);
						if (map.filepath.Length > 0 && File.Exists(text))
						{
							Logger.Write($"Found map texture '{Path.GetFileName(text)}'");
							long length = new FileInfo(text).Length;
							Texture2D textureByFilesize = GetTextureByFilesize(length, result);
							if (textureByFilesize != null)
							{
								map.main = textureByFilesize;
							}
							else
							{
								map.main = ImageUtils.LoadTexture(text, TextureFormat.DXT1);
							}
							map.filesize = length;
						}
						text = Path.Combine(dir, map.filepath_overlay);
						if (map.filepath_overlay.Length > 0 && File.Exists(text))
						{
							long length2 = new FileInfo(text).Length;
							Texture2D textureByFilesize2 = GetTextureByFilesize(length2, result);
							if (textureByFilesize2 != null)
							{
								map.overlay = textureByFilesize2;
							}
							else
							{
								map.overlay = ImageUtils.LoadTexture(text);
							}
							map.overlayFilesize = length2;
						}
						text = Path.Combine(dir, map.filepath_depth);
						if (map.filepath_depth.Length > 0 && File.Exists(text))
						{
							long length3 = new FileInfo(text).Length;
							Texture2D textureByFilesize3 = GetTextureByFilesize(length3, result);
							if (textureByFilesize3 != null)
							{
								map.depth = textureByFilesize3;
							}
							else
							{
								map.depth = ImageUtils.LoadTexture(text, TextureFormat.DXT1);
							}
							map.depthFilesize = length3;
						}
						Map map2 = result.Where((Map x) => map.mapId != "" && x.mapId == map.mapId).FirstOrDefault();
						if (map2 != null)
						{
							if ((bool)map.main)
							{
								map2.main = map.main;
								map2.filesize = map.filesize;
							}
							if ((bool)map.depth)
							{
								map2.depth = map.depth;
								map2.depthFilesize = map.depthFilesize;
							}
							if ((bool)map.overlay)
							{
								map2.overlay = map.overlay;
								map2.overlayFilesize = map.overlayFilesize;
							}
							if (map.name.Length > 0)
							{
								map2.name = map.name;
							}
						}
						else
						{
							if (map.name == "")
							{
								map.name = Path.GetFileNameWithoutExtension(path);
							}
							map.fogmap = Fogmap.Create(map);
							if (map.color.Length >= 3)
							{
								map.palette = CreatePalette(map.color);
							}
							else if (map.colorize && (bool)map.depth)
							{
								map.palette = Controller.Instance.Assets.LoadAsset<Texture2D>("world_palette");
							}
							result.Add(map);
							Logger.Print($"Map '{map.name}' loaded.");
						}
					}
					catch (Exception exception)
					{
						Debug.LogException(exception);
					}
				}
			}
		}

		private static Texture2D CreatePalette(int[] inColor)
		{
			Color32 c = new Color32((byte)inColor[0], (byte)inColor[1], (byte)inColor[2], byte.MaxValue);
			Texture2D texture2D = new Texture2D(2, 2, TextureFormat.RGB24, mipChain: false);
			texture2D.SetPixels(new Color[4]
			{
				c,
				c,
				c,
				c
			});
			texture2D.Apply();
			return texture2D;
		}

		private static Texture2D GetTextureByFilesize(long filesize, List<Map> result = null)
		{
			foreach (Map map in maps)
			{
				if ((bool)map.main && map.filesize == filesize)
				{
					return map.main;
				}
				if ((bool)map.overlay && map.overlayFilesize == filesize)
				{
					return map.overlay;
				}
				if ((bool)map.depth && map.depthFilesize == filesize)
				{
					return map.depth;
				}
			}
			if (result != null)
			{
				foreach (Map item in result)
				{
					if ((bool)item.main && item.filesize == filesize)
					{
						return item.main;
					}
					if ((bool)item.overlay && item.overlayFilesize == filesize)
					{
						return item.overlay;
					}
					if ((bool)item.depth && item.depthFilesize == filesize)
					{
						return item.depth;
					}
				}
			}
			return null;
		}
	}
}
