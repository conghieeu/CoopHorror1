using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ItemDropship : NetworkBehaviour
{
	public bool deliveringOrder;

	public bool shipLanded;

	public bool shipDoorsOpened;

	public Animator shipAnimator;

	public float shipTimer;

	public bool playersFirstOrder = true;

	private StartOfRound playersManager;

	private Terminal terminalScript;

	private List<int> itemsToDeliver = new List<int>();

	public Transform[] itemSpawnPositions;

	private float noiseInterval;

	private int timesPlayedWithoutTurningOff;

	public InteractTrigger triggerScript;

	private void Start()
	{
		playersManager = Object.FindObjectOfType<StartOfRound>();
		terminalScript = Object.FindObjectOfType<Terminal>();
	}

	private void Update()
	{
		if (!base.IsServer)
		{
			return;
		}
		if (!deliveringOrder)
		{
			if (terminalScript.orderedItemsFromTerminal.Count > 0)
			{
				if (playersManager.shipHasLanded)
				{
					shipTimer += Time.deltaTime;
				}
				if (playersFirstOrder)
				{
					playersFirstOrder = false;
					shipTimer = 20f;
				}
				if (shipTimer > 40f)
				{
					LandShipOnServer();
				}
			}
		}
		else if (shipLanded)
		{
			shipTimer += Time.deltaTime;
			if (shipTimer > 30f)
			{
				timesPlayedWithoutTurningOff = 0;
				shipLanded = false;
				ShipLeaveClientRpc();
			}
			if (noiseInterval <= 0f)
			{
				noiseInterval = 1f;
				timesPlayedWithoutTurningOff++;
				RoundManager.Instance.PlayAudibleNoise(base.transform.position, 60f, 1.3f, timesPlayedWithoutTurningOff, noiseIsInsideClosedShip: false, 94);
			}
			else
			{
				noiseInterval -= Time.deltaTime;
			}
		}
	}

	public void TryOpeningShip()
	{
		if (!shipDoorsOpened)
		{
			if (base.IsServer)
			{
				OpenShipDoorsOnServer();
			}
			else
			{
				OpenShipServerRpc();
			}
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void OpenShipServerRpc()
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Server && (networkManager.IsClient || networkManager.IsHost))
			{
				ServerRpcParams serverRpcParams = default(ServerRpcParams);
				FastBufferWriter bufferWriter = __beginSendServerRpc(638792059u, serverRpcParams, RpcDelivery.Reliable);
				__endSendServerRpc(ref bufferWriter, 638792059u, serverRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Server && (networkManager.IsServer || networkManager.IsHost))
			{
				OpenShipDoorsOnServer();
			}
		}
	}

	private void OpenShipDoorsOnServer()
	{
		if (shipLanded && !shipDoorsOpened)
		{
			int num = 0;
			for (int i = 0; i < itemsToDeliver.Count; i++)
			{
				GameObject obj = Object.Instantiate(terminalScript.buyableItemsList[itemsToDeliver[i]].spawnPrefab, itemSpawnPositions[num].position, Quaternion.identity, playersManager.propsContainer);
				obj.GetComponent<GrabbableObject>().fallTime = 0f;
				obj.GetComponent<NetworkObject>().Spawn();
				num = ((num < 3) ? (num + 1) : 0);
			}
			itemsToDeliver.Clear();
			OpenShipClientRpc();
		}
	}

	[ClientRpc]
	public void OpenShipClientRpc()
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Client && (networkManager.IsServer || networkManager.IsHost))
			{
				ClientRpcParams clientRpcParams = default(ClientRpcParams);
				FastBufferWriter bufferWriter = __beginSendClientRpc(3113622207u, clientRpcParams, RpcDelivery.Reliable);
				__endSendClientRpc(ref bufferWriter, 3113622207u, clientRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Client && (networkManager.IsClient || networkManager.IsHost))
			{
				shipAnimator.SetBool("doorsOpened", value: true);
				shipDoorsOpened = true;
				triggerScript.interactable = false;
			}
		}
	}

	public void ShipLandedAnimationEvent()
	{
		shipLanded = true;
	}

	private void LandShipOnServer()
	{
		shipTimer = 0f;
		itemsToDeliver.Clear();
		itemsToDeliver.AddRange(terminalScript.orderedItemsFromTerminal);
		terminalScript.orderedItemsFromTerminal.Clear();
		playersFirstOrder = false;
		deliveringOrder = true;
		LandShipClientRpc();
	}

	[ClientRpc]
	public void LandShipClientRpc()
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Client && (networkManager.IsServer || networkManager.IsHost))
			{
				ClientRpcParams clientRpcParams = default(ClientRpcParams);
				FastBufferWriter bufferWriter = __beginSendClientRpc(1496861823u, clientRpcParams, RpcDelivery.Reliable);
				__endSendClientRpc(ref bufferWriter, 1496861823u, clientRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Client && (networkManager.IsClient || networkManager.IsHost))
			{
				Object.FindObjectOfType<Terminal>().numberOfItemsInDropship = 0;
				shipAnimator.SetBool("landing", value: true);
				triggerScript.interactable = true;
			}
		}
	}

	[ClientRpc]
	public void ShipLeaveClientRpc()
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Client && (networkManager.IsServer || networkManager.IsHost))
			{
				ClientRpcParams clientRpcParams = default(ClientRpcParams);
				FastBufferWriter bufferWriter = __beginSendClientRpc(343429303u, clientRpcParams, RpcDelivery.Reliable);
				__endSendClientRpc(ref bufferWriter, 343429303u, clientRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Client && (networkManager.IsClient || networkManager.IsHost))
			{
				ShipLeave();
			}
		}
	}

	public void ShipLeave()
	{
		shipDoorsOpened = false;
		shipAnimator.SetBool("doorsOpened", value: false);
		shipLanded = false;
		shipAnimator.SetBool("landing", value: false);
		deliveringOrder = false;
		if (itemsToDeliver.Count > 0)
		{
			HUDManager.Instance.DisplayTip("Items missed!", "The vehicle returned with your purchased items. Our delivery fee cannot be refunded.");
		}
		Object.FindObjectOfType<Terminal>().numberOfItemsInDropship = 0;
		itemsToDeliver.Clear();
	}

	public void ShipLandedInAnimation()
	{
		shipLanded = true;
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	[RuntimeInitializeOnLoadMethod]
	internal static void InitializeRPCS_ItemDropship()
	{
		NetworkManager.__rpc_func_table.Add(638792059u, __rpc_handler_638792059);
		NetworkManager.__rpc_func_table.Add(3113622207u, __rpc_handler_3113622207);
		NetworkManager.__rpc_func_table.Add(1496861823u, __rpc_handler_1496861823);
		NetworkManager.__rpc_func_table.Add(343429303u, __rpc_handler_343429303);
	}

	private static void __rpc_handler_638792059(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Server;
			((ItemDropship)target).OpenShipServerRpc();
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	private static void __rpc_handler_3113622207(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Client;
			((ItemDropship)target).OpenShipClientRpc();
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	private static void __rpc_handler_1496861823(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Client;
			((ItemDropship)target).LandShipClientRpc();
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	private static void __rpc_handler_343429303(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Client;
			((ItemDropship)target).ShipLeaveClientRpc();
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	protected internal override string __getTypeName()
	{
		return "ItemDropship";
	}
}
