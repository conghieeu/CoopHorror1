using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

namespace FusionImpostor
{
	/// <summary>
	/// Forces the Fusion Bootstrap to closedown if the player tries to start a game but one cannot be found.
	/// </summary>
	public class NetFailHandler : MonoBehaviour
	{
		public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
		{
			if (shutdownReason == ShutdownReason.Ok) return;
			
			if (shutdownReason == ShutdownReason.GameNotFound)
			{
				FindObjectOfType<FusionBootstrap>().ShutdownAll();
			}
			
		}
	}
}