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

	[ClientRpc]
	public void OpenGiftBoxClientRpc(NetworkObjectReference netObjectRef, int presentValue, Vector3 startFallingPos)
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


}
