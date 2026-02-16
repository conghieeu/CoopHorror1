using Unity.Netcode;
using UnityEngine;

public class ShipLights : NetworkBehaviour
{
	public bool areLightsOn = true;

	public Animator shipLightsAnimator;

	[ServerRpc(RequireOwnership = false)]
	public void SetShipLightsServerRpc(bool setLightsOn)
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Server && (networkManager.IsClient || networkManager.IsHost))
			{
				ServerRpcParams serverRpcParams = default(ServerRpcParams);
				FastBufferWriter bufferWriter = __beginSendServerRpc(1625678258u, serverRpcParams, RpcDelivery.Reliable);
				bufferWriter.WriteValueSafe(in setLightsOn, default(FastBufferWriter.ForPrimitives));
				__endSendServerRpc(ref bufferWriter, 1625678258u, serverRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Server && (networkManager.IsServer || networkManager.IsHost))
			{
				SetShipLightsClientRpc(setLightsOn);
			}
		}
	}

	[ClientRpc]
	public void SetShipLightsClientRpc(bool setLightsOn)
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Client && (networkManager.IsServer || networkManager.IsHost))
			{
				ClientRpcParams clientRpcParams = default(ClientRpcParams);
				FastBufferWriter bufferWriter = __beginSendClientRpc(1484401029u, clientRpcParams, RpcDelivery.Reliable);
				bufferWriter.WriteValueSafe(in setLightsOn, default(FastBufferWriter.ForPrimitives));
				__endSendClientRpc(ref bufferWriter, 1484401029u, clientRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Client && (networkManager.IsClient || networkManager.IsHost))
			{
				areLightsOn = setLightsOn;
				shipLightsAnimator.SetBool("lightsOn", areLightsOn);
				Debug.Log($"Received set ship lights RPC. Lights on?: {areLightsOn}");
			}
		}
	}

	public void ToggleShipLights()
	{
		areLightsOn = !areLightsOn;
		shipLightsAnimator.SetBool("lightsOn", areLightsOn);
		SetShipLightsServerRpc(areLightsOn);
		Debug.Log($"Toggling ship lights RPC. lights now: {areLightsOn}");
	}

	public void SetShipLightsBoolean(bool setLights)
	{
		areLightsOn = setLights;
		shipLightsAnimator.SetBool("lightsOn", areLightsOn);
		SetShipLightsServerRpc(areLightsOn);
		Debug.Log($"Calling ship lights boolean RPC: {areLightsOn}");
	}

	public void ToggleShipLightsOnLocalClientOnly()
	{
		areLightsOn = !areLightsOn;
		shipLightsAnimator.SetBool("lightsOn", areLightsOn);
		Debug.Log($"Set ship lights on client only: {areLightsOn}");
	}

	public void SetShipLightsOnLocalClientOnly(bool setLightsOn)
	{
		areLightsOn = setLightsOn;
		shipLightsAnimator.SetBool("lightsOn", areLightsOn);
		Debug.Log($"Set ship lights on client only: {areLightsOn}");
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	[RuntimeInitializeOnLoadMethod]
	internal static void InitializeRPCS_ShipLights()
	{
		NetworkManager.__rpc_func_table.Add(1625678258u, __rpc_handler_1625678258);
		NetworkManager.__rpc_func_table.Add(1484401029u, __rpc_handler_1484401029);
	}

	private static void __rpc_handler_1625678258(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out bool value, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Server;
			((ShipLights)target).SetShipLightsServerRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	private static void __rpc_handler_1484401029(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out bool value, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Client;
			((ShipLights)target).SetShipLightsClientRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	protected internal override string __getTypeName()
	{
		return "ShipLights";
	}
}
