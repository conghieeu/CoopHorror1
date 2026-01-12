using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

namespace FusionImpostor
{
	/// <summary>
	/// Interactable that starts tasks for the local player.
	/// </summary>
	public class TaskStation : Interactable
	{
		public TaskUI taskUI;

		

		[Networked, Capacity(10)]
		public NetworkLinkedList<PlayerRef> allowedPlayers => default;

		public override bool CanInteract(PlayerMovement player)
		{
			return !player.IsSuspect && player.tasks.Contains(this);
		}

		public override void Interact(NetworkObject interactor)
		{
			if (!interactor.HasInputAuthority)
				return;

			taskUI.Begin();
			Debug.Log("Began task");
		}
	}
}