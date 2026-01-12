using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FusionImpostor
{
	/// <summary>
	/// Class that handles pressing the HUD buttons and attempting different actions.
	/// This new refers to the player input instance and tries to assign input through Fusion's input system instead of locally.
	/// </summary>
	public class HUDBridge : MonoBehaviour
	{
		public void Use()
		{
			Debug.Log("Attempting to use nearest interactable.");
			PlayerInputBehaviour.Instance?.PressInteractUI();
		}

		public void Report()
		{
			Debug.Log("Attempting to report body.");
			PlayerInputBehaviour.Instance?.PressReportUI();
		}

		public void Kill()
		{
			Debug.Log("Attempting to kill player.");
			PlayerInputBehaviour.Instance?.PressKillUI();
		}
	}
}