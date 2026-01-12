using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FusionImpostor
{
	/// <summary>
	/// Class with button events used to navigate the main menus.
	/// </summary>
	public class ManagerBridge : MonoBehaviour
	{
		public void BackToMenu()
		{
			UIScreen.Focus(GameManager.im.mainMenuScreen);
		}

		public void QuitGame()
		{
			GameManager.QuitGame();
		}
	}
}