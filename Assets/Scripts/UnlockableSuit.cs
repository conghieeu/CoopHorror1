using System;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

public class UnlockableSuit : NetworkBehaviour
{
	public NetworkVariable<int> syncedSuitID = new NetworkVariable<int>(-1);

	public int suitID = -1;

	public Material suitMaterial;

	public SkinnedMeshRenderer suitRenderer;

	private void Update()
	{
		if (!(GameNetworkManager.Instance == null) && !(NetworkManager.Singleton == null) && !NetworkManager.Singleton.ShutdownInProgress && suitID != syncedSuitID.Value)
		{
			suitID = syncedSuitID.Value;
			suitMaterial = StartOfRound.Instance.unlockablesList.unlockables[suitID].suitMaterial;
			suitRenderer.material = suitMaterial;
			base.gameObject.GetComponent<InteractTrigger>().hoverTip = "Change: " + StartOfRound.Instance.unlockablesList.unlockables[suitID].unlockableName;
		}
	}

	public void SwitchSuitToThis(PlayerControllerB playerWhoTriggered = null)
	{
		if (playerWhoTriggered == null)
		{
			playerWhoTriggered = GameNetworkManager.Instance.localPlayerController;
		}
		if (playerWhoTriggered.currentSuitID != suitID)
		{
			SwitchSuitForPlayer(playerWhoTriggered, suitID);
			SwitchSuitServerRpc((int)playerWhoTriggered.playerClientId);
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void SwitchSuitServerRpc(int playerID)
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Server && (networkManager.IsClient || networkManager.IsHost))
			{
				ServerRpcParams serverRpcParams = default(ServerRpcParams);
				FastBufferWriter bufferWriter = __beginSendServerRpc(3672046368u, serverRpcParams, RpcDelivery.Reliable);
				BytePacker.WriteValueBitPacked(bufferWriter, playerID);
				__endSendServerRpc(ref bufferWriter, 3672046368u, serverRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Server && (networkManager.IsServer || networkManager.IsHost))
			{
				SwitchSuitClientRpc(playerID);
			}
		}
	}

	[ClientRpc]
	public void SwitchSuitClientRpc(int playerID)
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Client && (networkManager.IsServer || networkManager.IsHost))
			{
				ClientRpcParams clientRpcParams = default(ClientRpcParams);
				FastBufferWriter bufferWriter = __beginSendClientRpc(2137061089u, clientRpcParams, RpcDelivery.Reliable);
				BytePacker.WriteValueBitPacked(bufferWriter, playerID);
				__endSendClientRpc(ref bufferWriter, 2137061089u, clientRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Client && (networkManager.IsClient || networkManager.IsHost) && (int)GameNetworkManager.Instance.localPlayerController.playerClientId != playerID)
			{
				SwitchSuitForPlayer(StartOfRound.Instance.allPlayerScripts[playerID], suitID);
			}
		}
	}

	public static void SwitchSuitForAllPlayers(int suitID, bool playAudio = false)
	{
		for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
		{
			Material material = StartOfRound.Instance.unlockablesList.unlockables[suitID].suitMaterial;
			PlayerControllerB playerControllerB = StartOfRound.Instance.allPlayerScripts[i];
			if (playAudio && playerControllerB.currentSuitID != suitID)
			{
				playerControllerB.movementAudio.PlayOneShot(StartOfRound.Instance.changeSuitSFX);
			}
			playerControllerB.thisPlayerModel.material = material;
			playerControllerB.thisPlayerModelLOD1.material = material;
			playerControllerB.thisPlayerModelLOD2.material = material;
			playerControllerB.thisPlayerModelArms.material = material;
			playerControllerB.currentSuitID = suitID;
		}
	}

	public static void SwitchSuitForPlayer(PlayerControllerB player, int suitID, bool playAudio = true)
	{
		if (playAudio && player.currentSuitID != suitID)
		{
			player.movementAudio.PlayOneShot(StartOfRound.Instance.changeSuitSFX);
		}
		Material material = StartOfRound.Instance.unlockablesList.unlockables[suitID].suitMaterial;
		player.thisPlayerModel.material = material;
		player.thisPlayerModelLOD1.material = material;
		player.thisPlayerModelLOD2.material = material;
		player.thisPlayerModelArms.material = material;
		player.currentSuitID = suitID;
	}

	protected override void __initializeVariables()
	{
		if (syncedSuitID == null)
		{
			throw new Exception("UnlockableSuit.syncedSuitID cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		syncedSuitID.Initialize(this);
		__nameNetworkVariable(syncedSuitID, "syncedSuitID");
		NetworkVariableFields.Add(syncedSuitID);
		base.__initializeVariables();
	}

	[RuntimeInitializeOnLoadMethod]
	internal static void InitializeRPCS_UnlockableSuit()
	{
		NetworkManager.__rpc_func_table.Add(3672046368u, __rpc_handler_3672046368);
		NetworkManager.__rpc_func_table.Add(2137061089u, __rpc_handler_2137061089);
	}

	private static void __rpc_handler_3672046368(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			target.__rpc_exec_stage = __RpcExecStage.Server;
			((UnlockableSuit)target).SwitchSuitServerRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	private static void __rpc_handler_2137061089(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			target.__rpc_exec_stage = __RpcExecStage.Client;
			((UnlockableSuit)target).SwitchSuitClientRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	protected internal override string __getTypeName()
	{
		return "UnlockableSuit";
	}
}
