using System;
using System.IO;
using UnityEngine;

namespace SubnauticaMap
{
	public static class ImageUtils
	{
		public static Sprite LoadSprite(string FilePath, float PixelsPerUnit = 100f, SpriteMeshType spriteType = SpriteMeshType.Tight)
		{
			Texture2D texture2D = LoadTexture(FilePath);
			if (!texture2D)
			{
				return null;
			}
			return Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(0f, 0f), PixelsPerUnit, 0u, spriteType);
		}

		public static Texture2D LoadTexture(string path, TextureFormat format = TextureFormat.DXT5)
		{
			try
			{
				Texture2D texture2D = new Texture2D(2, 2, format, mipChain: false);
				texture2D.LoadRawTextureData(File.ReadAllBytes(path));
				texture2D.wrapMode = TextureWrapMode.Clamp;
				return texture2D;
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
				Logger.Write($"Can't read file '{Path.GetFileName(path)}'");
			}
			return null;
		}

		public static Texture2D Render2Texture(RenderTexture rt, TextureFormat format = TextureFormat.DXT5)
		{
			RenderTexture.active = rt;
			Texture2D texture2D = new Texture2D(rt.width, rt.height, format, mipChain: false);
			texture2D.ReadPixels(new Rect(0f, 0f, rt.width, rt.height), 0, 0, recalculateMipMaps: false);
			texture2D.Apply();
			RenderTexture.active = null;
			return texture2D;
		}

		public static Sprite Texture2Sprite(Texture2D tex)
		{
			return Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0f, 0f), 100f, 0u, SpriteMeshType.Tight);
		}
	}
}
