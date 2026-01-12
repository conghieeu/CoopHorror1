using UnityEngine;

namespace FusionImpostor
{
	/// <summary>
	/// Class that allows users to toggle the fullscreen state of the game.
	/// </summary>
	public class FullscreenToggle : MonoBehaviour
	{
		public KeyCode fullscreenKey = KeyCode.Escape;

		private void Update()
		{
			if (Input.GetKeyDown(fullscreenKey))
			{
				Screen.fullScreen = !Screen.fullScreen;
			}
		}
	}
}