using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class EntranceTeleport : NetworkBehaviour
{
	public bool isEntranceToBuilding;

	public Transform entrancePoint;

	private Transform exitPoint;

	public int entranceId;

	public StartOfRound playersManager;

	private bool initializedVariables;

	public int audioReverbPreset = -1;

	public AudioSource entrancePointAudio;

	private AudioSource exitPointAudio;

	public AudioClip[] doorAudios;

	public AudioClip firstTimeAudio;

	public int dungeonFlowId = -1;

	private InteractTrigger triggerScript;

	private float checkForEnemiesInterval;

	private bool enemyNearLastCheck;

	private bool gotExitPoint;

	private bool checkedForFirstTime;

	private void Awake()
	{
		playersManager = Object.FindObjectOfType<StartOfRound>();
		triggerScript = base.gameObject.GetComponent<InteractTrigger>();
		checkForEnemiesInterval = 10f;
	}

	public bool FindExitPoint()
	{
		EntranceTeleport[] array = Object.FindObjectsOfType<EntranceTeleport>();
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].isEntranceToBuilding != isEntranceToBuilding && array[i].entranceId == entranceId)
			{
				if (array[i].entrancePointAudio != null)
				{
					exitPointAudio = array[i].entrancePointAudio;
				}
				exitPoint = array[i].entrancePoint;
			}
		}
		if (exitPoint == null)
		{
			return false;
		}
		return true;
	}

	public void TeleportPlayer()
	{
		bool flag = false;
		if (!FindExitPoint())
		{
			flag = true;
		}
		if (flag)
		{
			HUDManager.Instance.DisplayTip("???", "The entrance appears to be blocked.");
			return;
		}
		Transform thisPlayerBody = GameNetworkManager.Instance.localPlayerController.thisPlayerBody;
		GameNetworkManager.Instance.localPlayerController.TeleportPlayer(exitPoint.position);
		GameNetworkManager.Instance.localPlayerController.isInElevator = false;
		GameNetworkManager.Instance.localPlayerController.isInHangarShipRoom = false;
		thisPlayerBody.eulerAngles = new Vector3(thisPlayerBody.eulerAngles.x, exitPoint.eulerAngles.y, thisPlayerBody.eulerAngles.z);
		SetAudioPreset((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
		if (!checkedForFirstTime)
		{
			checkedForFirstTime = true;
			if (firstTimeAudio != null && dungeonFlowId != -1 && !ES3.Load($"PlayedDungeonEntrance{dungeonFlowId}", "LCGeneralSaveData", defaultValue: false))
			{
				StartCoroutine(playMusicOnDelay());
			}
		}
		for (int i = 0; i < GameNetworkManager.Instance.localPlayerController.ItemSlots.Length; i++)
		{
			if (GameNetworkManager.Instance.localPlayerController.ItemSlots[i] != null)
			{
				GameNetworkManager.Instance.localPlayerController.ItemSlots[i].isInFactory = isEntranceToBuilding;
			}
		}
		TeleportPlayerServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
		GameNetworkManager.Instance.localPlayerController.isInsideFactory = isEntranceToBuilding;
	}

	private IEnumerator playMusicOnDelay()
	{
		yield return new WaitForSeconds(0.6f);
		ES3.Save($"PlayedDungeonEntrance{dungeonFlowId}", value: true, "LCGeneralSaveData");
		HUDManager.Instance.UIAudio.PlayOneShot(firstTimeAudio);
	}

	[ServerRpc(RequireOwnership = false)]
	public void TeleportPlayerServerRpc(int playerObj)
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Server && (networkManager.IsClient || networkManager.IsHost))
			{
				ServerRpcParams serverRpcParams = default(ServerRpcParams);
				FastBufferWriter bufferWriter = __beginSendServerRpc(4279190381u, serverRpcParams, RpcDelivery.Reliable);
				BytePacker.WriteValueBitPacked(bufferWriter, playerObj);
				__endSendServerRpc(ref bufferWriter, 4279190381u, serverRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Server && (networkManager.IsServer || networkManager.IsHost))
			{
				TeleportPlayerClientRpc(playerObj);
			}
		}
	}

	[ClientRpc]
	public void TeleportPlayerClientRpc(int playerObj)
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Client && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default(ClientRpcParams);
			FastBufferWriter bufferWriter = __beginSendClientRpc(3168414823u, clientRpcParams, RpcDelivery.Reliable);
			BytePacker.WriteValueBitPacked(bufferWriter, playerObj);
			__endSendClientRpc(ref bufferWriter, 3168414823u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage != __RpcExecStage.Client || (!networkManager.IsClient && !networkManager.IsHost) || playersManager.allPlayerScripts[playerObj] == GameNetworkManager.Instance.localPlayerController)
		{
			return;
		}
		FindExitPoint();
		playersManager.allPlayerScripts[playerObj].TeleportPlayer(exitPoint.position, withRotation: true, exitPoint.eulerAngles.y);
		playersManager.allPlayerScripts[playerObj].isInElevator = false;
		playersManager.allPlayerScripts[playerObj].isInHangarShipRoom = false;
		PlayAudioAtTeleportPositions();
		playersManager.allPlayerScripts[playerObj].isInsideFactory = isEntranceToBuilding;
		for (int i = 0; i < playersManager.allPlayerScripts[playerObj].ItemSlots.Length; i++)
		{
			if (playersManager.allPlayerScripts[playerObj].ItemSlots[i] != null)
			{
				playersManager.allPlayerScripts[playerObj].ItemSlots[i].isInFactory = isEntranceToBuilding;
			}
		}
		if (GameNetworkManager.Instance.localPlayerController.isPlayerDead && playersManager.allPlayerScripts[playerObj] == GameNetworkManager.Instance.localPlayerController.spectatedPlayerScript)
		{
			SetAudioPreset(playerObj);
		}
	}

	private void SetAudioPreset(int playerObj)
	{
		if (audioReverbPreset != -1)
		{
			Object.FindObjectOfType<AudioReverbPresets>().audioPresets[audioReverbPreset].ChangeAudioReverbForPlayer(StartOfRound.Instance.allPlayerScripts[playerObj]);
			if (entrancePointAudio != null)
			{
				PlayAudioAtTeleportPositions();
			}
		}
	}

	public void PlayAudioAtTeleportPositions()
	{
		if (doorAudios.Length != 0)
		{
			entrancePointAudio.PlayOneShot(doorAudios[Random.Range(0, doorAudios.Length)]);
			exitPointAudio.PlayOneShot(doorAudios[Random.Range(0, doorAudios.Length)]);
		}
	}

	private void Update()
	{
		if (triggerScript == null || !isEntranceToBuilding)
		{
			return;
		}
		if (checkForEnemiesInterval <= 0f)
		{
			if (!gotExitPoint)
			{
				if (FindExitPoint())
				{
					gotExitPoint = true;
				}
				return;
			}
			checkForEnemiesInterval = 1f;
			bool flag = false;
			for (int i = 0; i < RoundManager.Instance.SpawnedEnemies.Count; i++)
			{
				if (Vector3.Distance(RoundManager.Instance.SpawnedEnemies[i].transform.position, exitPoint.transform.position) < 7.7f)
				{
					flag = true;
					break;
				}
			}
			if (flag && !enemyNearLastCheck)
			{
				enemyNearLastCheck = true;
				triggerScript.hoverTip = "[Near activity detected!]";
			}
			else if (enemyNearLastCheck)
			{
				enemyNearLastCheck = false;
				triggerScript.hoverTip = "Enter: [LMB]";
			}
		}
		else
		{
			checkForEnemiesInterval -= Time.deltaTime;
		}
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	[RuntimeInitializeOnLoadMethod]
	internal static void InitializeRPCS_EntranceTeleport()
	{
		NetworkManager.__rpc_func_table.Add(4279190381u, __rpc_handler_4279190381);
		NetworkManager.__rpc_func_table.Add(3168414823u, __rpc_handler_3168414823);
	}

	private static void __rpc_handler_4279190381(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			target.__rpc_exec_stage = __RpcExecStage.Server;
			((EntranceTeleport)target).TeleportPlayerServerRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	private static void __rpc_handler_3168414823(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			target.__rpc_exec_stage = __RpcExecStage.Client;
			((EntranceTeleport)target).TeleportPlayerClientRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	protected internal override string __getTypeName()
	{
		return "EntranceTeleport";
	}
}
