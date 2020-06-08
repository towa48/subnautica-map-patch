using System;
using UnityEngine;

namespace SubnauticaMap
{
	public class Keybinding : MonoBehaviour
	{
		public Func<KeyCode> key;

		public Action action;

		private void Update()
		{
			try
			{
				if (key != null && action != null && Input.GetKeyDown(key()))
				{
					action();
				}
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
		}
	}
}
