using Harmony;
using System;

namespace SubnauticaMap
{
	public class Logger
	{
		private static string prefix = "[SubnauticaMap] ";

		public static void Write(string text)
		{
			Console.WriteLine(prefix + text);
		}

		public static void Print(string text)
		{
			Write(text);
			ErrorMessage.AddMessage(prefix + text);
		}

		public static void Show(string text, float delay = 1f, float duration = 5f)
		{
			Write(text);
			Traverse.Create(Subtitles.main).Method("AddRawLong", prefix + text, delay, duration).GetValue();
		}
	}
}
