using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SubnauticaMap
{
	public class Fogmap
	{
		public string id = "";

		public bool isActive;

		public RenderTexture rt;

		public Texture2D temp;

		public Texture2D depth;

		private static List<Fogmap> fogmaps = new List<Fogmap>();

		public static List<Fogmap> All()
		{
			return fogmaps;
		}

		private static RenderTexture CreateRT()
		{
			return new RenderTexture(512, 512, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default)
			{
				filterMode = FilterMode.Bilinear,
				wrapMode = TextureWrapMode.Clamp,
				anisoLevel = 1,
				antiAliasing = 1,
				useMipMap = false
			};
		}

		public static Fogmap Create(Map map)
		{
			if (map.fogmapId == "")
			{
				return null;
			}
			Fogmap fogmap = fogmaps.Where((Fogmap x) => x.id == map.fogmapId).FirstOrDefault();
			if (fogmap == null)
			{
				fogmap = new Fogmap
				{
					id = map.fogmapId,
					depth = map.depth
				};
				fogmaps.Add(fogmap);
				fogmap.Load();
			}
			return fogmap;
		}

		public static void CheckState()
		{
			List<Map> source = Map.All();
			foreach (Fogmap fogmap in fogmaps)
			{
				if (source.Where((Map x) => x.fogmap == fogmap && x.isActive).Count() > 0)
				{
					if (!fogmap.isActive)
					{
						fogmap.LoadFromTemp();
						fogmap.isActive = true;
					}
				}
				else if (fogmap.isActive)
				{
					fogmap.SaveToTemp();
					fogmap.isActive = false;
				}
			}
		}

		private void LoadFromTemp()
		{
			rt = CreateRT();
			RenderTexture.active = rt;
			Graphics.Blit(temp, rt);
			RenderTexture.active = null;
		}

		private void Load()
		{
			temp = null;
			string path = Path.Combine(Controller.SaveDir, (id == "world") ? "fogmap.bin" : $"fogmap_{id}.bin");
			if (File.Exists(path))
			{
				try
				{
					temp = ImageUtils.LoadTexture(path, TextureFormat.ARGB32);
				}
				catch (Exception exception)
				{
					Debug.LogException(exception);
					Logger.Show($"Error loading fogmap '{id}'");
				}
			}
			if (temp == null)
			{
				temp = new Texture2D(2, 2, TextureFormat.ARGB32, mipChain: false);
				temp.SetPixels(new Color[4]
				{
					Color.white,
					Color.white,
					Color.white,
					Color.white
				});
				temp.Apply();
			}
		}

		private void SaveToTemp()
		{
			temp = ImageUtils.Render2Texture(rt, TextureFormat.ARGB32);
			rt.Release();
			rt = null;
		}

		private void Save()
		{
			try
			{
				if (!Directory.Exists(Controller.SaveDir))
				{
					Directory.CreateDirectory(Controller.SaveDir);
				}
				((rt != null) ? ImageUtils.Render2Texture(rt, TextureFormat.ARGB32) : temp).SavePNG(Path.Combine(Controller.SaveDir, (id == "world") ? "fogmap.bin" : $"fogmap_{id}.bin"));
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
				Logger.Show($"Error saving fogmap '{id}'");
			}
		}

		public static void SaveAll()
		{
			fogmaps.ForEach(delegate(Fogmap x)
			{
				x.Save();
			});
		}

		public static void Release()
		{
			foreach (Fogmap fogmap in fogmaps)
			{
				if (fogmap.isActive)
				{
					fogmap.rt.Release();
				}
			}
		}
	}
}
