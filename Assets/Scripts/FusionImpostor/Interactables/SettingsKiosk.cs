using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FusionImpostor
{
	/// <summary>
	/// Interactable that handles updating the game settings while in the pregame state.
	/// </summary>
	public class SettingsKiosk : Interactable
	{
		public SettingsUI settingsUI;
		public override bool CanInteract(PlayerMovement player) => Object.HasStateAuthority;
		public override void Interact(NetworkObject interactor)
		{
			if (!interactor.HasInputAuthority)
				return;

			settingsUI.Open();
		}
	}
}