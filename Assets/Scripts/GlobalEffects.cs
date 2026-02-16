using Unity.Netcode;
using UnityEngine;

public class GlobalEffects : NetworkBehaviour
{
	private StartOfRound playersManager;

	public bool ownedByPlayer;

	public static GlobalEffects Instance { get; private set; }

	private void Awake()
	{
		if (!ownedByPlayer)
		{
			if (!(Instance == null))
			{
				Object.Destroy(base.gameObject);
				return;
			}
			Instance = this;
		}
		playersManager = Object.FindObjectOfType<StartOfRound>();
	}

	public void PlayAnimAndAudioServer(ServerAnimAndAudio serverAnimAndAudio)
	{
		playersManager.allPlayerObjects[playersManager.thisClientPlayerId].GetComponentInChildren<GlobalEffects>().PlayAnimAndAudioServerFromSenderObject(serverAnimAndAudio);
	}

	public void PlayAnimAndAudioServerFromSenderObject(ServerAnimAndAudio serverAnimAndAudio)
	{
		PlayAnimAndAudioServerRpc(serverAnimAndAudio);
	}

	[ServerRpc(RequireOwnership = false)]
	private void PlayAnimAndAudioServerRpc(ServerAnimAndAudio serverAnimAndAudio)
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Server && (networkManager.IsClient || networkManager.IsHost))
			{
				ServerRpcParams serverRpcParams = default(ServerRpcParams);
				FastBufferWriter bufferWriter = __beginSendServerRpc(2259057361u, serverRpcParams, RpcDelivery.Reliable);
				bufferWriter.WriteValueSafe(in serverAnimAndAudio, default(FastBufferWriter.ForNetworkSerializable));
				__endSendServerRpc(ref bufferWriter, 2259057361u, serverRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Server && (networkManager.IsServer || networkManager.IsHost))
			{
				PlayAnimAndAudioClientRpc(serverAnimAndAudio);
			}
		}
	}

	[ClientRpc]
	private void PlayAnimAndAudioClientRpc(ServerAnimAndAudio serverAnimAndAudio)
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Client && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default(ClientRpcParams);
			FastBufferWriter bufferWriter = __beginSendClientRpc(2993461149u, clientRpcParams, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in serverAnimAndAudio, default(FastBufferWriter.ForNetworkSerializable));
			__endSendClientRpc(ref bufferWriter, 2993461149u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Client && (networkManager.IsClient || networkManager.IsHost))
		{
			if (serverAnimAndAudio.animatorObj.TryGet(out var networkObject))
			{
				networkObject.GetComponent<Animator>().SetTrigger(serverAnimAndAudio.animationString);
			}
			if (serverAnimAndAudio.audioObj.TryGet(out var networkObject2))
			{
				networkObject2.GetComponent<AudioSource>().PlayOneShot(networkObject2.GetComponent<AudioSource>().clip);
			}
		}
	}

	public void PlayAnimationServer(ServerAnimation serverAnimation)
	{
		playersManager.allPlayerObjects[playersManager.thisClientPlayerId].GetComponentInChildren<GlobalEffects>().PlayAnimationServerFromSenderObject(serverAnimation);
	}

	public void PlayAnimationServerFromSenderObject(ServerAnimation serverAnimation)
	{
		PlayAnimationServerRpc(serverAnimation);
	}

	[ServerRpc(RequireOwnership = false)]
	private void PlayAnimationServerRpc(ServerAnimation serverAnimation)
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Server && (networkManager.IsClient || networkManager.IsHost))
			{
				ServerRpcParams serverRpcParams = default(ServerRpcParams);
				FastBufferWriter bufferWriter = __beginSendServerRpc(2698736096u, serverRpcParams, RpcDelivery.Reliable);
				bufferWriter.WriteValueSafe(in serverAnimation, default(FastBufferWriter.ForNetworkSerializable));
				__endSendServerRpc(ref bufferWriter, 2698736096u, serverRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Server && (networkManager.IsServer || networkManager.IsHost))
			{
				PlayAnimationClientRpc(serverAnimation);
			}
		}
	}

	[ClientRpc]
	private void PlayAnimationClientRpc(ServerAnimation serverAnimation)
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Client && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default(ClientRpcParams);
			FastBufferWriter bufferWriter = __beginSendClientRpc(780678780u, clientRpcParams, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in serverAnimation, default(FastBufferWriter.ForNetworkSerializable));
			__endSendClientRpc(ref bufferWriter, 780678780u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage != __RpcExecStage.Client || (!networkManager.IsClient && !networkManager.IsHost) || base.IsOwner)
		{
			return;
		}
		if (serverAnimation.animatorObj.TryGet(out var networkObject))
		{
			if (serverAnimation.isTrigger)
			{
				networkObject.GetComponent<Animator>().SetTrigger(serverAnimation.animationString);
			}
			else
			{
				networkObject.GetComponent<Animator>().SetBool(serverAnimation.animationString, serverAnimation.setTrue);
			}
		}
		else
		{
			Debug.LogWarning("Was not able to retrieve NetworkObject from NetworkObjectReference; string " + serverAnimation.animationString);
		}
	}

	public void PlayAudioServer(ServerAudio serverAudio)
	{
		playersManager.allPlayerObjects[playersManager.thisClientPlayerId].GetComponentInChildren<GlobalEffects>().PlayAudioServerFromSenderObject(serverAudio);
	}

	public void PlayAudioServerFromSenderObject(ServerAudio serverAudio)
	{
		PlayAudioServerRpc(serverAudio);
	}

	[ServerRpc(RequireOwnership = false)]
	private void PlayAudioServerRpc(ServerAudio serverAudio)
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Server && (networkManager.IsClient || networkManager.IsHost))
			{
				ServerRpcParams serverRpcParams = default(ServerRpcParams);
				FastBufferWriter bufferWriter = __beginSendServerRpc(1842858504u, serverRpcParams, RpcDelivery.Reliable);
				bufferWriter.WriteValueSafe(in serverAudio, default(FastBufferWriter.ForNetworkSerializable));
				__endSendServerRpc(ref bufferWriter, 1842858504u, serverRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Server && (networkManager.IsServer || networkManager.IsHost))
			{
				PlayAudioClientRpc(serverAudio);
			}
		}
	}

	[ClientRpc]
	private void PlayAudioClientRpc(ServerAudio serverAudio)
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Client && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default(ClientRpcParams);
			FastBufferWriter bufferWriter = __beginSendClientRpc(182727243u, clientRpcParams, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in serverAudio, default(FastBufferWriter.ForNetworkSerializable));
			__endSendClientRpc(ref bufferWriter, 182727243u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage != __RpcExecStage.Client || (!networkManager.IsClient && !networkManager.IsHost) || base.IsOwner)
		{
			return;
		}
		if (serverAudio.audioObj.TryGet(out var networkObject))
		{
			AudioSource component = networkObject.gameObject.GetComponent<AudioSource>();
			if (serverAudio.oneshot)
			{
				component.PlayOneShot(component.clip, 1f);
				return;
			}
			component.loop = serverAudio.looped;
			component.Play();
		}
		else
		{
			Debug.LogWarning("Was not able to retrieve NetworkObject from NetworkObjectReference; audio");
		}
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	[RuntimeInitializeOnLoadMethod]
	internal static void InitializeRPCS_GlobalEffects()
	{
		NetworkManager.__rpc_func_table.Add(2259057361u, __rpc_handler_2259057361);
		NetworkManager.__rpc_func_table.Add(2993461149u, __rpc_handler_2993461149);
		NetworkManager.__rpc_func_table.Add(2698736096u, __rpc_handler_2698736096);
		NetworkManager.__rpc_func_table.Add(780678780u, __rpc_handler_780678780);
		NetworkManager.__rpc_func_table.Add(1842858504u, __rpc_handler_1842858504);
		NetworkManager.__rpc_func_table.Add(182727243u, __rpc_handler_182727243);
	}

	private static void __rpc_handler_2259057361(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out ServerAnimAndAudio value, default(FastBufferWriter.ForNetworkSerializable));
			target.__rpc_exec_stage = __RpcExecStage.Server;
			((GlobalEffects)target).PlayAnimAndAudioServerRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	private static void __rpc_handler_2993461149(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out ServerAnimAndAudio value, default(FastBufferWriter.ForNetworkSerializable));
			target.__rpc_exec_stage = __RpcExecStage.Client;
			((GlobalEffects)target).PlayAnimAndAudioClientRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	private static void __rpc_handler_2698736096(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out ServerAnimation value, default(FastBufferWriter.ForNetworkSerializable));
			target.__rpc_exec_stage = __RpcExecStage.Server;
			((GlobalEffects)target).PlayAnimationServerRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	private static void __rpc_handler_780678780(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out ServerAnimation value, default(FastBufferWriter.ForNetworkSerializable));
			target.__rpc_exec_stage = __RpcExecStage.Client;
			((GlobalEffects)target).PlayAnimationClientRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	private static void __rpc_handler_1842858504(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out ServerAudio value, default(FastBufferWriter.ForNetworkSerializable));
			target.__rpc_exec_stage = __RpcExecStage.Server;
			((GlobalEffects)target).PlayAudioServerRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	private static void __rpc_handler_182727243(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out ServerAudio value, default(FastBufferWriter.ForNetworkSerializable));
			target.__rpc_exec_stage = __RpcExecStage.Client;
			((GlobalEffects)target).PlayAudioClientRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	protected internal override string __getTypeName()
	{
		return "GlobalEffects";
	}
}
