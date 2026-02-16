using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class ItemCharger : NetworkBehaviour
{
	public InteractTrigger triggerScript;

	public Animator chargeStationAnimator;

	private Coroutine chargeItemCoroutine;

	public AudioSource zapAudio;

	private float updateInterval;

	public void ChargeItem()
	{
		GrabbableObject currentlyHeldObjectServer = GameNetworkManager.Instance.localPlayerController.currentlyHeldObjectServer;
		if (!(currentlyHeldObjectServer == null) && currentlyHeldObjectServer.itemProperties.requiresBattery)
		{
			PlayChargeItemEffectServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
			if (chargeItemCoroutine != null)
			{
				StopCoroutine(chargeItemCoroutine);
			}
			chargeItemCoroutine = StartCoroutine(chargeItemDelayed(currentlyHeldObjectServer));
		}
	}

	private void Update()
	{
		if (NetworkManager.Singleton == null)
		{
			return;
		}
		if (updateInterval > 1f)
		{
			updateInterval = 0f;
			if (GameNetworkManager.Instance != null && GameNetworkManager.Instance.localPlayerController != null)
			{
				triggerScript.interactable = GameNetworkManager.Instance.localPlayerController.currentlyHeldObjectServer != null && GameNetworkManager.Instance.localPlayerController.currentlyHeldObjectServer.itemProperties.requiresBattery;
			}
		}
		else
		{
			updateInterval += Time.deltaTime;
		}
	}

	private IEnumerator chargeItemDelayed(GrabbableObject itemToCharge)
	{
		zapAudio.Play();
		yield return new WaitForSeconds(0.75f);
		chargeStationAnimator.SetTrigger("zap");
		if (itemToCharge != null)
		{
			itemToCharge.insertedBattery = new Battery(isEmpty: false, 1f);
			itemToCharge.SyncBatteryServerRpc(100);
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void PlayChargeItemEffectServerRpc(int playerChargingItem)
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Server && (networkManager.IsClient || networkManager.IsHost))
			{
				ServerRpcParams serverRpcParams = default(ServerRpcParams);
				FastBufferWriter bufferWriter = __beginSendServerRpc(1188697655u, serverRpcParams, RpcDelivery.Reliable);
				BytePacker.WriteValueBitPacked(bufferWriter, playerChargingItem);
				__endSendServerRpc(ref bufferWriter, 1188697655u, serverRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Server && (networkManager.IsServer || networkManager.IsHost))
			{
				PlayChargeItemEffectClientRpc(playerChargingItem);
			}
		}
	}

	[ClientRpc]
	public void PlayChargeItemEffectClientRpc(int playerChargingItem)
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Client && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default(ClientRpcParams);
			FastBufferWriter bufferWriter = __beginSendClientRpc(3542355993u, clientRpcParams, RpcDelivery.Reliable);
			BytePacker.WriteValueBitPacked(bufferWriter, playerChargingItem);
			__endSendClientRpc(ref bufferWriter, 3542355993u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Client && (networkManager.IsClient || networkManager.IsHost) && !(GameNetworkManager.Instance.localPlayerController == null) && (int)GameNetworkManager.Instance.localPlayerController.playerClientId != playerChargingItem)
		{
			if (chargeItemCoroutine != null)
			{
				StopCoroutine(chargeItemCoroutine);
			}
			chargeItemCoroutine = StartCoroutine(chargeItemDelayed(null));
		}
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	[RuntimeInitializeOnLoadMethod]
	internal static void InitializeRPCS_ItemCharger()
	{
		NetworkManager.__rpc_func_table.Add(1188697655u, __rpc_handler_1188697655);
		NetworkManager.__rpc_func_table.Add(3542355993u, __rpc_handler_3542355993);
	}

	private static void __rpc_handler_1188697655(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			target.__rpc_exec_stage = __RpcExecStage.Server;
			((ItemCharger)target).PlayChargeItemEffectServerRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	private static void __rpc_handler_3542355993(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			target.__rpc_exec_stage = __RpcExecStage.Client;
			((ItemCharger)target).PlayChargeItemEffectClientRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	protected internal override string __getTypeName()
	{
		return "ItemCharger";
	}
}
