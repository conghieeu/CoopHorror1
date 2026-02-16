using System;
using System.Collections;
using System.Collections.Generic;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

public class GiftBoxItem : GrabbableObject
{
	private GameObject objectInPresent;

	public ParticleSystem PoofParticle;

	public AudioSource presentAudio;

	public AudioClip openGiftAudio;

	private PlayerControllerB previousPlayerHeldBy;

	private bool hasUsedGift;

	private int objectInPresentValue;

	public override void Start()
	{
		base.Start();
		System.Random random = new System.Random((int)targetFloorPosition.x + (int)targetFloorPosition.y);
		if (!base.IsServer)
		{
			return;
		}
		List<int> list = new List<int>(RoundManager.Instance.currentLevel.spawnableScrap.Count);
		for (int i = 0; i < RoundManager.Instance.currentLevel.spawnableScrap.Count; i++)
		{
			if (RoundManager.Instance.currentLevel.spawnableScrap[i].spawnableItem.itemId == 152767)
			{
				list.Add(0);
			}
			else
			{
				list.Add(RoundManager.Instance.currentLevel.spawnableScrap[i].rarity);
			}
		}
		int randomWeightedIndexList = RoundManager.Instance.GetRandomWeightedIndexList(list, random);
		Item spawnableItem = RoundManager.Instance.currentLevel.spawnableScrap[randomWeightedIndexList].spawnableItem;
		objectInPresent = spawnableItem.spawnPrefab;
		objectInPresentValue = (int)((float)random.Next(spawnableItem.minValue + 25, spawnableItem.maxValue + 35) * RoundManager.Instance.scrapValueMultiplier);
	}

	public override void EquipItem()
	{
		base.EquipItem();
		previousPlayerHeldBy = playerHeldBy;
	}

	public override void ItemActivate(bool used, bool buttonDown = true)
	{
		base.ItemActivate(used, buttonDown);
		if (!(playerHeldBy == null) && !hasUsedGift)
		{
			hasUsedGift = true;
			playerHeldBy.activatingItem = true;
			OpenGiftBoxServerRpc();
		}
	}

	public override void PocketItem()
	{
		base.PocketItem();
		playerHeldBy.activatingItem = false;
	}

	[ServerRpc(RequireOwnership = false)]
	public void OpenGiftBoxServerRpc()
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Server && (networkManager.IsClient || networkManager.IsHost))
		{
			ServerRpcParams serverRpcParams = default(ServerRpcParams);
			FastBufferWriter bufferWriter = __beginSendServerRpc(2878544999u, serverRpcParams, RpcDelivery.Reliable);
			__endSendServerRpc(ref bufferWriter, 2878544999u, serverRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage != __RpcExecStage.Server || (!networkManager.IsServer && !networkManager.IsHost))
		{
			return;
		}
		GameObject gameObject = null;
		int presentValue = 0;
		Vector3 vector = Vector3.zero;
		if (objectInPresent == null)
		{
			Debug.LogError("Error: There is no object in gift box!");
		}
		else
		{
			Transform parent = ((((!(playerHeldBy != null) || !playerHeldBy.isInElevator) && !StartOfRound.Instance.inShipPhase) || !(RoundManager.Instance.spawnedScrapContainer != null)) ? StartOfRound.Instance.elevatorTransform : RoundManager.Instance.spawnedScrapContainer);
			vector = base.transform.position + Vector3.up * 0.25f;
			gameObject = UnityEngine.Object.Instantiate(objectInPresent, vector, Quaternion.identity, parent);
			GrabbableObject component = gameObject.GetComponent<GrabbableObject>();
			component.startFallingPosition = vector;
			StartCoroutine(SetObjectToHitGroundSFX(component));
			component.targetFloorPosition = component.GetItemFloorPosition(base.transform.position);
			if (previousPlayerHeldBy != null && previousPlayerHeldBy.isInHangarShipRoom)
			{
				previousPlayerHeldBy.SetItemInElevator(droppedInShipRoom: true, droppedInElevator: true, component);
			}
			presentValue = objectInPresentValue;
			component.SetScrapValue(presentValue);
			component.NetworkObject.Spawn();
		}
		if (gameObject != null)
		{
			OpenGiftBoxClientRpc(gameObject.GetComponent<NetworkObject>(), presentValue, vector);
		}
		OpenGiftBoxNoPresentClientRpc();
	}

	private IEnumerator SetObjectToHitGroundSFX(GrabbableObject gObject)
	{
		yield return new WaitForEndOfFrame();
		Debug.Log("Setting " + gObject.itemProperties.itemName + " hit ground to false");
		gObject.reachedFloorTarget = false;
		gObject.hasHitGround = false;
		gObject.fallTime = 0f;
	}

	[ClientRpc]
	public void OpenGiftBoxNoPresentClientRpc()
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Client && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default(ClientRpcParams);
			FastBufferWriter bufferWriter = __beginSendClientRpc(3328558740u, clientRpcParams, RpcDelivery.Reliable);
			__endSendClientRpc(ref bufferWriter, 3328558740u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Client && (networkManager.IsClient || networkManager.IsHost))
		{
			PoofParticle.Play();
			presentAudio.PlayOneShot(openGiftAudio);
			WalkieTalkie.TransmitOneShotAudio(presentAudio, openGiftAudio);
			RoundManager.Instance.PlayAudibleNoise(presentAudio.transform.position, 8f, 0.5f, 0, isInShipRoom && StartOfRound.Instance.hangarDoorsClosed);
			if (playerHeldBy != null)
			{
				playerHeldBy.activatingItem = false;
				DestroyObjectInHand(playerHeldBy);
			}
		}
	}

	[ClientRpc]
	public void OpenGiftBoxClientRpc(NetworkObjectReference netObjectRef, int presentValue, Vector3 startFallingPos)
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Client && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default(ClientRpcParams);
			FastBufferWriter bufferWriter = __beginSendClientRpc(1252354594u, clientRpcParams, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in netObjectRef, default(FastBufferWriter.ForNetworkSerializable));
			BytePacker.WriteValueBitPacked(bufferWriter, presentValue);
			bufferWriter.WriteValueSafe(in startFallingPos);
			__endSendClientRpc(ref bufferWriter, 1252354594u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Client && (networkManager.IsClient || networkManager.IsHost))
		{
			PoofParticle.Play();
			presentAudio.PlayOneShot(openGiftAudio);
			WalkieTalkie.TransmitOneShotAudio(presentAudio, openGiftAudio);
			RoundManager.Instance.PlayAudibleNoise(presentAudio.transform.position, 8f, 0.5f, 0, isInShipRoom && StartOfRound.Instance.hangarDoorsClosed);
			if (playerHeldBy != null)
			{
				playerHeldBy.activatingItem = false;
				DestroyObjectInHand(playerHeldBy);
			}
			if (!base.IsServer)
			{
				StartCoroutine(waitForGiftPresentToSpawnOnClient(netObjectRef, presentValue, startFallingPos));
			}
		}
	}

	private IEnumerator waitForGiftPresentToSpawnOnClient(NetworkObjectReference netObjectRef, int presentValue, Vector3 startFallingPos)
	{
		NetworkObject netObject = null;
		float startTime = Time.realtimeSinceStartup;
		while (Time.realtimeSinceStartup - startTime < 8f && !netObjectRef.TryGet(out netObject))
		{
			yield return new WaitForSeconds(0.03f);
		}
		if (netObject == null)
		{
			Debug.Log("No network object found");
			yield break;
		}
		yield return new WaitForEndOfFrame();
		GrabbableObject component = netObject.GetComponent<GrabbableObject>();
		RoundManager.Instance.totalScrapValueInLevel -= scrapValue;
		RoundManager.Instance.totalScrapValueInLevel += component.scrapValue;
		component.SetScrapValue(presentValue);
		component.startFallingPosition = startFallingPos;
		component.fallTime = 0f;
		component.hasHitGround = false;
		component.reachedFloorTarget = false;
		if (previousPlayerHeldBy != null && previousPlayerHeldBy.isInHangarShipRoom)
		{
			previousPlayerHeldBy.SetItemInElevator(droppedInShipRoom: true, droppedInElevator: true, component);
		}
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	[RuntimeInitializeOnLoadMethod]
	internal static void InitializeRPCS_GiftBoxItem()
	{
		NetworkManager.__rpc_func_table.Add(2878544999u, __rpc_handler_2878544999);
		NetworkManager.__rpc_func_table.Add(3328558740u, __rpc_handler_3328558740);
		NetworkManager.__rpc_func_table.Add(1252354594u, __rpc_handler_1252354594);
	}

	private static void __rpc_handler_2878544999(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Server;
			((GiftBoxItem)target).OpenGiftBoxServerRpc();
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	private static void __rpc_handler_3328558740(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Client;
			((GiftBoxItem)target).OpenGiftBoxNoPresentClientRpc();
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	private static void __rpc_handler_1252354594(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out NetworkObjectReference value, default(FastBufferWriter.ForNetworkSerializable));
			ByteUnpacker.ReadValueBitPacked(reader, out int value2);
			reader.ReadValueSafe(out Vector3 value3);
			target.__rpc_exec_stage = __RpcExecStage.Client;
			((GiftBoxItem)target).OpenGiftBoxClientRpc(value, value2, value3);
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	protected internal override string __getTypeName()
	{
		return "GiftBoxItem";
	}
}
