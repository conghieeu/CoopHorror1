using System.Collections.Generic;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

public class BridgeTrigger : NetworkBehaviour
{
	public float bridgeDurability = 1f;

	private PlayerControllerB playerOnBridge;

	private List<PlayerControllerB> playersOnBridge = new List<PlayerControllerB>();

	public AudioSource bridgeAudioSource;

	public AudioClip[] bridgeCreakSFX;

	public AudioClip bridgeFallSFX;

	public Animator bridgeAnimator;

	private bool hasBridgeFallen;

	public Transform bridgePhysicsPartsContainer;

	private bool giantOnBridge;

	private bool giantOnBridgeLastFrame;

	public Collider[] fallenBridgeColliders;

	private void OnEnable()
	{
		StartOfRound.Instance.playerTeleportedEvent.AddListener(RemovePlayerFromBridge);
	}

	private void OnDisable()
	{
		StartOfRound.Instance.playerTeleportedEvent.RemoveListener(RemovePlayerFromBridge);
	}

	private void Update()
	{
		if (hasBridgeFallen)
		{
			return;
		}
		if (giantOnBridge)
		{
			bridgeDurability -= Time.deltaTime / 4.25f;
		}
		if (playersOnBridge.Count > 0)
		{
			bridgeDurability = Mathf.Clamp(bridgeDurability - Time.deltaTime * (0.02f * (float)(playersOnBridge.Count * playersOnBridge.Count)), 0f, 1f);
			for (int i = 0; i < playersOnBridge.Count; i++)
			{
				if (playersOnBridge[i].carryWeight > 1.1f)
				{
					bridgeDurability -= Time.deltaTime * (0.04f * playersOnBridge[i].carryWeight);
				}
			}
		}
		else if (bridgeDurability < 1f && !giantOnBridge)
		{
			bridgeDurability = Mathf.Clamp(bridgeDurability + Time.deltaTime * 0.2f, 0f, 1f);
		}
		if (base.IsServer && bridgeDurability <= 0f && !hasBridgeFallen)
		{
			hasBridgeFallen = true;
			BridgeFallServerRpc();
			Debug.Log("Bridge collapsed! On server");
		}
		bridgeAnimator.SetFloat("durability", Mathf.Clamp(Mathf.Abs(bridgeDurability - 1f), 0f, 1f));
	}

	private void LateUpdate()
	{
		if (giantOnBridge)
		{
			if (giantOnBridgeLastFrame)
			{
				giantOnBridge = false;
				giantOnBridgeLastFrame = false;
			}
			else
			{
				giantOnBridgeLastFrame = true;
			}
		}
	}

	[ServerRpc]
	public void BridgeFallServerRpc()
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Server && (networkManager.IsClient || networkManager.IsHost))
		{
			if (base.OwnerClientId != networkManager.LocalClientId)
			{
				if (networkManager.LogLevel <= LogLevel.Normal)
				{
					Debug.LogError("Only the owner can invoke a ServerRpc that requires ownership!");
				}
				return;
			}
			ServerRpcParams serverRpcParams = default(ServerRpcParams);
			FastBufferWriter bufferWriter = __beginSendServerRpc(2883846656u, serverRpcParams, RpcDelivery.Reliable);
			__endSendServerRpc(ref bufferWriter, 2883846656u, serverRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Server && (networkManager.IsServer || networkManager.IsHost))
		{
			BridgeFallClientRpc();
		}
	}

	[ClientRpc]
	public void BridgeFallClientRpc()
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Client && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default(ClientRpcParams);
			FastBufferWriter bufferWriter = __beginSendClientRpc(123213822u, clientRpcParams, RpcDelivery.Reliable);
			__endSendClientRpc(ref bufferWriter, 123213822u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Client && (networkManager.IsClient || networkManager.IsHost))
		{
			Debug.Log("Bridge collapsed! On client");
			hasBridgeFallen = true;
			bridgeAnimator.SetTrigger("Fall");
			EnableFallenBridgeColliders();
			bridgeAudioSource.PlayOneShot(bridgeFallSFX);
			float num = Vector3.Distance(GameNetworkManager.Instance.localPlayerController.transform.position, bridgeAudioSource.transform.position);
			if (num < 30f)
			{
				HUDManager.Instance.ShakeCamera(ScreenShakeType.VeryStrong);
				Debug.Log("Shaking screen!!!");
			}
			else if (num < 50f)
			{
				HUDManager.Instance.ShakeCamera(ScreenShakeType.Long);
			}
		}
	}

	private void EnableFallenBridgeColliders()
	{
		for (int i = 0; i < fallenBridgeColliders.Length; i++)
		{
			fallenBridgeColliders[i].enabled = true;
		}
	}

	private void OnTriggerStay(Collider other)
	{
		if (other.gameObject.CompareTag("Player"))
		{
			playerOnBridge = other.gameObject.GetComponent<PlayerControllerB>();
			if (playerOnBridge != null && !playersOnBridge.Contains(playerOnBridge))
			{
				playersOnBridge.Add(playerOnBridge);
				if (Random.Range(playersOnBridge.Count * 25, 100) > 60)
				{
					RoundManager.PlayRandomClip(bridgeAudioSource, bridgeCreakSFX);
				}
			}
		}
		else if (other.gameObject.CompareTag("Enemy"))
		{
			EnemyAICollisionDetect component = other.gameObject.GetComponent<EnemyAICollisionDetect>();
			if (component != null && component.mainScript.enemyType.enemyName == "ForestGiant")
			{
				giantOnBridge = true;
				giantOnBridgeLastFrame = false;
			}
		}
	}

	public void RemovePlayerFromBridge(PlayerControllerB playerOnBridge)
	{
		if (playerOnBridge != null && playersOnBridge.Contains(playerOnBridge))
		{
			playersOnBridge.Remove(playerOnBridge);
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (other.gameObject.CompareTag("Player"))
		{
			playerOnBridge = other.gameObject.GetComponent<PlayerControllerB>();
			RemovePlayerFromBridge(playerOnBridge);
		}
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	[RuntimeInitializeOnLoadMethod]
	internal static void InitializeRPCS_BridgeTrigger()
	{
		NetworkManager.__rpc_func_table.Add(2883846656u, __rpc_handler_2883846656);
		NetworkManager.__rpc_func_table.Add(123213822u, __rpc_handler_123213822);
	}

	private static void __rpc_handler_2883846656(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (rpcParams.Server.Receive.SenderClientId != target.OwnerClientId)
		{
			if (networkManager.LogLevel <= LogLevel.Normal)
			{
				Debug.LogError("Only the owner can invoke a ServerRpc that requires ownership!");
			}
		}
		else
		{
			target.__rpc_exec_stage = __RpcExecStage.Server;
			((BridgeTrigger)target).BridgeFallServerRpc();
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	private static void __rpc_handler_123213822(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Client;
			((BridgeTrigger)target).BridgeFallClientRpc();
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	protected internal override string __getTypeName()
	{
		return "BridgeTrigger";
	}
}
