using System.Linq;
using UnityEngine;
using Fusion;

namespace FusionImpostor
{
	/// <summary>
	/// Handles the initial start of the game where players can host or join rooms.
	/// </summary>
	public class NetworkStartBridge : MonoBehaviour
	{
		public FusionBootstrap starter;
		public TMPro.TMP_InputField codeField;

		private void OnEnable()
		{
			codeField.text = starter.DefaultRoomName;
		}

		public void SetCode(string code)
		{
			starter.DefaultRoomName = code;
		}
		
		public void StartHost()
		{
			if (string.IsNullOrWhiteSpace(starter.DefaultRoomName))
				starter.DefaultRoomName = RoomCode.Create();
			starter.StartHost();
		}

		public void StartClient()
		{
			starter.StartClient();
		}

		public void Shutdown()
		{
			foreach (var runner in NetworkRunner.Instances.ToList())
			{
				runner.Shutdown();
			}
		}
	}
}