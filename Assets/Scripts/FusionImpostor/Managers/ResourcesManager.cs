using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

namespace FusionImpostor
{
	/// <summary>
	/// Holds references to various assets that are references throughout the game as an alternative to loading them through Resources.
	/// </summary>
	public class ResourcesManager : MonoBehaviour
	{
		public Color[] playerColours = new Color[12];
		public Material playerBodyMaterial;
		public Material playerTrimMaterial;
		public Material playerVisorMaterial;
		public Shader ghostShader;

		public WorldCanvasNickname worldCanvasNicknamePrefab;
		public TaskMapIcon taskMapIconPrefab;
		public PlayerMapIcon playerMapIconPrefab;
		public DeadPlayer deadPlayer;

		readonly List<NetworkObject> managedObjects = new List<NetworkObject>();

		public void Manage(NetworkObject obj)
		{
			managedObjects.Add(obj);
		}

		public void Purge()
		{
			foreach (var obj in managedObjects)
			{
				if (obj) GameManager.Instance.Runner.Despawn(obj);
			}
			managedObjects.Clear();
		}
	}
}