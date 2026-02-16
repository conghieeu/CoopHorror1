using System;
using GameNetcodeStuff;
using Unity.Netcode;
using Unity.Netcode.Samples;
using UnityEngine;

public abstract class GrabbableObject : NetworkBehaviour
{
	public bool grabbable;

	public bool isHeld;

	public bool isHeldByEnemy;

	public bool deactivated;

	[Space(3f)]
	public Transform parentObject;

	public Vector3 targetFloorPosition;

	public Vector3 startFallingPosition;

	public int floorYRot;

	public float fallTime;

	public bool hasHitGround;

	[Space(5f)]
	public int scrapValue;

	public bool itemUsedUp;

	public PlayerControllerB playerHeldBy;

	public bool isPocketed;

	public bool isBeingUsed;

	public bool isInElevator;

	public bool isInShipRoom;

	public bool isInFactory = true;

	[Space(10f)]
	public float useCooldown;

	public float currentUseCooldown;

	[Space(10f)]
	public Item itemProperties;

	public Battery insertedBattery;

	public string customGrabTooltip;

	[HideInInspector]
	public Rigidbody propBody;

	[HideInInspector]
	public Collider[] propColliders;

	[HideInInspector]
	public Vector3 originalScale;

	public bool wasOwnerLastFrame;

	public MeshRenderer mainObjectRenderer;

	private int isSendingItemRPC;

	public bool scrapPersistedThroughRounds;

	public bool heldByPlayerOnServer;

	[HideInInspector]
	public Transform radarIcon;

	public bool reachedFloorTarget;

	[Space(3f)]
	public bool grabbableToEnemies = true;

	private bool hasBeenHeld;

	public virtual int GetItemDataToSave()
	{
		if (!itemProperties.saveItemVariable)
		{
			Debug.LogError("GetItemDataToSave is being called on " + itemProperties.itemName + ", which does not have saveItemVariable set true.");
		}
		return 0;
	}

	public virtual void LoadItemSaveData(int saveData)
	{
		if (!itemProperties.saveItemVariable)
		{
			Debug.LogError("LoadItemSaveData is being called on " + itemProperties.itemName + ", which does not have saveItemVariable set true.");
		}
	}

	public virtual void Start()
	{
		propColliders = base.gameObject.GetComponentsInChildren<Collider>();
		originalScale = base.transform.localScale;
		if (itemProperties.itemSpawnsOnGround)
		{
			startFallingPosition = base.transform.position;
			if (base.transform.parent != null)
			{
				startFallingPosition = base.transform.parent.InverseTransformPoint(startFallingPosition);
			}
			FallToGround();
		}
		else
		{
			fallTime = 1f;
			hasHitGround = true;
			reachedFloorTarget = true;
			targetFloorPosition = base.transform.localPosition;
		}
		if (itemProperties.isScrap)
		{
			fallTime = 1f;
			hasHitGround = true;
		}
		if (itemProperties.isScrap && RoundManager.Instance.mapPropsContainer != null)
		{
			radarIcon = UnityEngine.Object.Instantiate(StartOfRound.Instance.itemRadarIconPrefab, RoundManager.Instance.mapPropsContainer.transform).transform;
		}
		if (!itemProperties.isScrap)
		{
			HoarderBugAI.grabbableObjectsInMap.Add(base.gameObject);
		}
		MeshRenderer[] componentsInChildren = base.gameObject.GetComponentsInChildren<MeshRenderer>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].renderingLayerMask = 1u;
		}
		SkinnedMeshRenderer[] componentsInChildren2 = base.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
		for (int j = 0; j < componentsInChildren2.Length; j++)
		{
			componentsInChildren2[j].renderingLayerMask = 1u;
		}
	}

	public void FallToGround(bool randomizePosition = false)
	{
		fallTime = 0f;
		if (Physics.Raycast(base.transform.position, Vector3.down, out var hitInfo, 80f, 268437760, QueryTriggerInteraction.Ignore))
		{
			targetFloorPosition = hitInfo.point + itemProperties.verticalOffset * Vector3.up;
			if (base.transform.parent != null)
			{
				targetFloorPosition = base.transform.parent.InverseTransformPoint(targetFloorPosition);
			}
		}
		else
		{
			Debug.Log("dropping item did not get raycast : " + base.gameObject.name);
			targetFloorPosition = base.transform.localPosition;
		}
		if (randomizePosition)
		{
			targetFloorPosition += new Vector3(UnityEngine.Random.Range(-0.5f, 0.5f), 0f, UnityEngine.Random.Range(-0.5f, 0.5f));
		}
	}

	public void EnablePhysics(bool enable)
	{
		for (int i = 0; i < propColliders.Length; i++)
		{
			if (!(propColliders[i] == null) && !propColliders[i].gameObject.CompareTag("InteractTrigger") && !propColliders[i].gameObject.CompareTag("DoNotSet"))
			{
				propColliders[i].enabled = enable;
			}
		}
	}

	public virtual void InspectItem()
	{
		if (base.IsOwner && playerHeldBy != null && itemProperties.canBeInspected)
		{
			playerHeldBy.IsInspectingItem = !playerHeldBy.IsInspectingItem;
			HUDManager.Instance.SetNearDepthOfFieldEnabled(!playerHeldBy.IsInspectingItem);
		}
	}

	public virtual void InteractItem()
	{
	}

	public void GrabItemOnClient()
	{
		if (!base.IsOwner)
		{
			Debug.LogError("GrabItemOnClient was called but player was not the owner.");
			return;
		}
		SetControlTipsForItem();
		GrabItem();
		if (itemProperties.syncGrabFunction)
		{
			isSendingItemRPC++;
			GrabServerRpc();
		}
	}

	public virtual void SetControlTipsForItem()
	{
		HUDManager.Instance.ChangeControlTipMultiple(itemProperties.toolTips, holdingItem: true, itemProperties);
	}

	public virtual void GrabItem()
	{
	}

	public void UseItemOnClient(bool buttonDown = true)
	{
		if (!base.IsOwner)
		{
			Debug.Log("Can't use item; not owner");
		}
		else if (!RequireCooldown() && UseItemBatteries())
		{
			if (itemProperties.syncUseFunction)
			{
				isSendingItemRPC++;
				ActivateItemServerRpc(isBeingUsed, buttonDown);
			}
			ItemActivate(isBeingUsed, buttonDown);
		}
	}

	public bool UseItemBatteries()
	{
		if (itemProperties.requiresBattery && (insertedBattery == null || insertedBattery.empty))
		{
			return false;
		}
		if (itemProperties.itemIsTrigger)
		{
			insertedBattery.charge = Mathf.Clamp(insertedBattery.charge - itemProperties.batteryUsage, 0f, 1f);
			if (insertedBattery.charge <= 0f)
			{
				insertedBattery.empty = true;
			}
			isBeingUsed = false;
		}
		else if (itemProperties.automaticallySetUsingPower)
		{
			isBeingUsed = !isBeingUsed;
		}
		return true;
	}

	public virtual void ItemActivate(bool used, bool buttonDown = true)
	{
	}

	public void ItemInteractLeftRightOnClient(bool right)
	{
		if (!base.IsOwner)
		{
			Debug.Log("InteractLeftRight was called but player was not the owner.");
		}
		else if (!RequireCooldown() && UseItemBatteries())
		{
			ItemInteractLeftRight(right);
			if (itemProperties.syncInteractLRFunction)
			{
				isSendingItemRPC++;
				InteractLeftRightServerRpc(right);
			}
		}
	}

	public virtual void ItemInteractLeftRight(bool right)
	{
	}

	public virtual void UseUpBatteries()
	{
		isBeingUsed = false;
	}

	public virtual void GrabItemFromEnemy(EnemyAI enemy)
	{
	}

	public virtual void DiscardItemFromEnemy()
	{
	}

	public virtual void ChargeBatteries()
	{
	}

	public virtual void DestroyObjectInHand(PlayerControllerB playerHolding)
	{
		grabbable = false;
		grabbableToEnemies = false;
		deactivated = true;
		if (playerHolding != null)
		{
			playerHolding.activatingItem = false;
		}
		if (radarIcon != null)
		{
			UnityEngine.Object.Destroy(radarIcon.gameObject);
		}
		MeshRenderer[] componentsInChildren = base.gameObject.GetComponentsInChildren<MeshRenderer>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			UnityEngine.Object.Destroy(componentsInChildren[i]);
		}
		Collider[] componentsInChildren2 = base.gameObject.GetComponentsInChildren<Collider>();
		for (int j = 0; j < componentsInChildren2.Length; j++)
		{
			UnityEngine.Object.Destroy(componentsInChildren2[j]);
		}
		if (base.IsOwner && isHeld && !isPocketed && playerHolding != null && playerHeldBy == GameNetworkManager.Instance.localPlayerController)
		{
			playerHeldBy.DiscardHeldObject();
		}
	}

	public virtual void EquipItem()
	{
		if (base.IsOwner)
		{
			HUDManager.Instance.ClearControlTips();
			SetControlTipsForItem();
		}
		EnableItemMeshes(enable: true);
		isPocketed = false;
		if (!hasBeenHeld)
		{
			hasBeenHeld = true;
			if (!isInShipRoom && !StartOfRound.Instance.inShipPhase && StartOfRound.Instance.currentLevel.spawnEnemiesAndScrap)
			{
				RoundManager.Instance.valueOfFoundScrapItems += scrapValue;
			}
		}
	}

	public virtual void PocketItem()
	{
		if (base.IsOwner && playerHeldBy != null)
		{
			playerHeldBy.IsInspectingItem = false;
		}
		isPocketed = true;
		EnableItemMeshes(enable: false);
		base.gameObject.GetComponent<AudioSource>().PlayOneShot(itemProperties.pocketSFX, 1f);
	}

	public void DiscardItemOnClient()
	{
		if (base.IsOwner)
		{
			DiscardItem();
			HUDManager.Instance.ClearControlTips();
			SyncBatteryServerRpc((int)(insertedBattery.charge * 100f));
			if (itemProperties.syncDiscardFunction)
			{
				isSendingItemRPC++;
				DiscardItemServerRpc();
			}
		}
	}

	[ServerRpc]
	public void SyncBatteryServerRpc(int charge)
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
			FastBufferWriter bufferWriter = __beginSendServerRpc(3484508350u, serverRpcParams, RpcDelivery.Reliable);
			BytePacker.WriteValueBitPacked(bufferWriter, charge);
			__endSendServerRpc(ref bufferWriter, 3484508350u, serverRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Server && (networkManager.IsServer || networkManager.IsHost))
		{
			SyncBatteryClientRpc(charge);
		}
	}

	[ClientRpc]
	public void SyncBatteryClientRpc(int charge)
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Client && (networkManager.IsServer || networkManager.IsHost))
			{
				ClientRpcParams clientRpcParams = default(ClientRpcParams);
				FastBufferWriter bufferWriter = __beginSendClientRpc(2670202430u, clientRpcParams, RpcDelivery.Reliable);
				BytePacker.WriteValueBitPacked(bufferWriter, charge);
				__endSendClientRpc(ref bufferWriter, 2670202430u, clientRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Client && (networkManager.IsClient || networkManager.IsHost))
			{
				float num = (float)charge / 100f;
				insertedBattery = new Battery(num <= 0f, num);
				ChargeBatteries();
			}
		}
	}

	public virtual void DiscardItem()
	{
		if (base.IsOwner)
		{
			HUDManager.Instance.ClearControlTips();
			if (playerHeldBy != null)
			{
				playerHeldBy.IsInspectingItem = false;
				playerHeldBy.activatingItem = false;
			}
		}
		playerHeldBy = null;
	}

	public virtual void LateUpdate()
	{
		if (parentObject != null)
		{
			base.transform.rotation = parentObject.rotation;
			base.transform.Rotate(itemProperties.rotationOffset);
			base.transform.position = parentObject.position;
			Vector3 positionOffset = itemProperties.positionOffset;
			positionOffset = parentObject.rotation * positionOffset;
			base.transform.position += positionOffset;
		}
		if (radarIcon != null)
		{
			radarIcon.position = base.transform.position;
		}
	}

	public virtual void FallWithCurve()
	{
		float num = startFallingPosition.y - targetFloorPosition.y;
		if (floorYRot == -1)
		{
			base.transform.rotation = Quaternion.Lerp(base.transform.rotation, Quaternion.Euler(itemProperties.restingRotation.x, base.transform.eulerAngles.y, itemProperties.restingRotation.z), Mathf.Clamp(14f * Time.deltaTime / num, 0f, 1f));
		}
		else
		{
			base.transform.rotation = Quaternion.Lerp(base.transform.rotation, Quaternion.Euler(itemProperties.restingRotation.x, (float)(floorYRot + itemProperties.floorYOffset) + 90f, itemProperties.restingRotation.z), Mathf.Clamp(14f * Time.deltaTime / num, 0f, 1f));
		}
		if (num > 5f)
		{
			base.transform.localPosition = Vector3.Lerp(startFallingPosition, targetFloorPosition, StartOfRound.Instance.objectFallToGroundCurveNoBounce.Evaluate(fallTime));
		}
		else
		{
			base.transform.localPosition = Vector3.Lerp(startFallingPosition, targetFloorPosition, StartOfRound.Instance.objectFallToGroundCurve.Evaluate(fallTime));
		}
		fallTime += Mathf.Abs(Time.deltaTime * 6f / num);
	}

	public virtual void OnPlaceObject()
	{
	}

	public virtual void OnBroughtToShip()
	{
		if (radarIcon != null)
		{
			UnityEngine.Object.Destroy(radarIcon.gameObject);
		}
	}

	public virtual void Update()
	{
		if (currentUseCooldown >= 0f)
		{
			currentUseCooldown -= Time.deltaTime;
		}
		if (base.IsOwner)
		{
			if (isBeingUsed && itemProperties.requiresBattery)
			{
				if (insertedBattery.charge > 0f)
				{
					if (!itemProperties.itemIsTrigger)
					{
						insertedBattery.charge -= Time.deltaTime / itemProperties.batteryUsage;
					}
				}
				else if (!insertedBattery.empty)
				{
					insertedBattery.empty = true;
					if (isBeingUsed)
					{
						isBeingUsed = false;
						UseUpBatteries();
						isSendingItemRPC++;
						UseUpItemBatteriesServerRpc();
					}
				}
			}
			if (!wasOwnerLastFrame)
			{
				wasOwnerLastFrame = true;
			}
		}
		else if (wasOwnerLastFrame)
		{
			wasOwnerLastFrame = false;
		}
		if (isHeld || !(parentObject == null))
		{
			return;
		}
		if (fallTime < 1f)
		{
			reachedFloorTarget = false;
			FallWithCurve();
			if (base.transform.localPosition.y - targetFloorPosition.y < 0.1f && !hasHitGround)
			{
				PlayDropSFX();
				OnHitGround();
			}
			return;
		}
		if (!reachedFloorTarget)
		{
			reachedFloorTarget = true;
			if (floorYRot == -1)
			{
				base.transform.rotation = Quaternion.Euler(itemProperties.restingRotation.x, base.transform.eulerAngles.y, itemProperties.restingRotation.z);
			}
			else
			{
				base.transform.rotation = Quaternion.Euler(itemProperties.restingRotation.x, (float)(floorYRot + itemProperties.floorYOffset) + 90f, itemProperties.restingRotation.z);
			}
		}
		base.transform.localPosition = targetFloorPosition;
	}

	public virtual void OnHitGround()
	{
	}

	private void PlayDropSFX()
	{
		if (itemProperties.dropSFX != null)
		{
			base.gameObject.GetComponent<AudioSource>().PlayOneShot(itemProperties.dropSFX);
			if (base.IsOwner)
			{
				RoundManager.Instance.PlayAudibleNoise(base.transform.position, 8f, 0.5f, 0, isInElevator && StartOfRound.Instance.hangarDoorsClosed, 941);
			}
		}
		hasHitGround = true;
	}

	public void SetScrapValue(int setValueTo)
	{
		scrapValue = setValueTo;
		ScanNodeProperties componentInChildren = base.gameObject.GetComponentInChildren<ScanNodeProperties>();
		if (componentInChildren == null)
		{
			Debug.LogError("Scan node is missing for item!: " + base.gameObject.name);
			return;
		}
		componentInChildren.subText = $"Value: ${setValueTo}";
		componentInChildren.scrapValue = setValueTo;
	}

	public bool RequireCooldown()
	{
		if (useCooldown > 0f)
		{
			if (itemProperties.holdButtonUse && isBeingUsed)
			{
				return false;
			}
			if (currentUseCooldown <= 0f)
			{
				currentUseCooldown = useCooldown;
				return false;
			}
			return true;
		}
		return false;
	}

	[ServerRpc(RequireOwnership = false)]
	private void InteractLeftRightServerRpc(bool right)
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Server && (networkManager.IsClient || networkManager.IsHost))
			{
				ServerRpcParams serverRpcParams = default(ServerRpcParams);
				FastBufferWriter bufferWriter = __beginSendServerRpc(1469591241u, serverRpcParams, RpcDelivery.Reliable);
				bufferWriter.WriteValueSafe(in right, default(FastBufferWriter.ForPrimitives));
				__endSendServerRpc(ref bufferWriter, 1469591241u, serverRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Server && (networkManager.IsServer || networkManager.IsHost))
			{
				InteractLeftRightClientRpc(right);
			}
		}
	}

	[ClientRpc]
	private void InteractLeftRightClientRpc(bool right)
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Client && (networkManager.IsServer || networkManager.IsHost))
			{
				ClientRpcParams clientRpcParams = default(ClientRpcParams);
				FastBufferWriter bufferWriter = __beginSendClientRpc(3081511085u, clientRpcParams, RpcDelivery.Reliable);
				bufferWriter.WriteValueSafe(in right, default(FastBufferWriter.ForPrimitives));
				__endSendClientRpc(ref bufferWriter, 3081511085u, clientRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Client && (networkManager.IsClient || networkManager.IsHost) && !base.IsOwner)
			{
				ItemInteractLeftRight(right);
			}
		}
	}

	[ServerRpc(RequireOwnership = false)]
	private void GrabServerRpc()
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Server && (networkManager.IsClient || networkManager.IsHost))
			{
				ServerRpcParams serverRpcParams = default(ServerRpcParams);
				FastBufferWriter bufferWriter = __beginSendServerRpc(2618697776u, serverRpcParams, RpcDelivery.Reliable);
				__endSendServerRpc(ref bufferWriter, 2618697776u, serverRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Server && (networkManager.IsServer || networkManager.IsHost))
			{
				GrabClientRpc();
			}
		}
	}

	[ClientRpc]
	private void GrabClientRpc()
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Client && (networkManager.IsServer || networkManager.IsHost))
			{
				ClientRpcParams clientRpcParams = default(ClientRpcParams);
				FastBufferWriter bufferWriter = __beginSendClientRpc(1334815929u, clientRpcParams, RpcDelivery.Reliable);
				__endSendClientRpc(ref bufferWriter, 1334815929u, clientRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Client && (networkManager.IsClient || networkManager.IsHost) && !base.IsOwner)
			{
				GrabItem();
			}
		}
	}

	[ServerRpc(RequireOwnership = false)]
	private void ActivateItemServerRpc(bool onOff, bool buttonDown)
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Server && (networkManager.IsClient || networkManager.IsHost))
			{
				ServerRpcParams serverRpcParams = default(ServerRpcParams);
				FastBufferWriter bufferWriter = __beginSendServerRpc(4280509730u, serverRpcParams, RpcDelivery.Reliable);
				bufferWriter.WriteValueSafe(in onOff, default(FastBufferWriter.ForPrimitives));
				bufferWriter.WriteValueSafe(in buttonDown, default(FastBufferWriter.ForPrimitives));
				__endSendServerRpc(ref bufferWriter, 4280509730u, serverRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Server && (networkManager.IsServer || networkManager.IsHost))
			{
				ActivateItemClientRpc(onOff, buttonDown);
			}
		}
	}

	[ClientRpc]
	private void ActivateItemClientRpc(bool onOff, bool buttonDown)
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Client && (networkManager.IsServer || networkManager.IsHost))
			{
				ClientRpcParams clientRpcParams = default(ClientRpcParams);
				FastBufferWriter bufferWriter = __beginSendClientRpc(1761213193u, clientRpcParams, RpcDelivery.Reliable);
				bufferWriter.WriteValueSafe(in onOff, default(FastBufferWriter.ForPrimitives));
				bufferWriter.WriteValueSafe(in buttonDown, default(FastBufferWriter.ForPrimitives));
				__endSendClientRpc(ref bufferWriter, 1761213193u, clientRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Client && (networkManager.IsClient || networkManager.IsHost) && !base.IsOwner)
			{
				isBeingUsed = onOff;
				ItemActivate(onOff, buttonDown);
			}
		}
	}

	[ServerRpc(RequireOwnership = false)]
	private void DiscardItemServerRpc()
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Server && (networkManager.IsClient || networkManager.IsHost))
			{
				ServerRpcParams serverRpcParams = default(ServerRpcParams);
				FastBufferWriter bufferWriter = __beginSendServerRpc(1974688543u, serverRpcParams, RpcDelivery.Reliable);
				__endSendServerRpc(ref bufferWriter, 1974688543u, serverRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Server && (networkManager.IsServer || networkManager.IsHost))
			{
				DiscardItemClientRpc();
			}
		}
	}

	[ClientRpc]
	private void DiscardItemClientRpc()
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Client && (networkManager.IsServer || networkManager.IsHost))
			{
				ClientRpcParams clientRpcParams = default(ClientRpcParams);
				FastBufferWriter bufferWriter = __beginSendClientRpc(335835173u, clientRpcParams, RpcDelivery.Reliable);
				__endSendClientRpc(ref bufferWriter, 335835173u, clientRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Client && (networkManager.IsClient || networkManager.IsHost) && !base.IsOwner)
			{
				DiscardItem();
			}
		}
	}

	[ServerRpc(RequireOwnership = false)]
	private void UseUpItemBatteriesServerRpc()
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Server && (networkManager.IsClient || networkManager.IsHost))
			{
				ServerRpcParams serverRpcParams = default(ServerRpcParams);
				FastBufferWriter bufferWriter = __beginSendServerRpc(2025123357u, serverRpcParams, RpcDelivery.Reliable);
				__endSendServerRpc(ref bufferWriter, 2025123357u, serverRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Server && (networkManager.IsServer || networkManager.IsHost))
			{
				UseUpItemBatteriesClientRpc();
			}
		}
	}

	[ClientRpc]
	private void UseUpItemBatteriesClientRpc()
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Client && (networkManager.IsServer || networkManager.IsHost))
			{
				ClientRpcParams clientRpcParams = default(ClientRpcParams);
				FastBufferWriter bufferWriter = __beginSendClientRpc(738171084u, clientRpcParams, RpcDelivery.Reliable);
				__endSendClientRpc(ref bufferWriter, 738171084u, clientRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Client && (networkManager.IsClient || networkManager.IsHost) && !base.IsOwner)
			{
				UseUpBatteries();
			}
		}
	}

	[ServerRpc(RequireOwnership = false)]
	private void EquipItemServerRpc()
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Server && (networkManager.IsClient || networkManager.IsHost))
			{
				ServerRpcParams serverRpcParams = default(ServerRpcParams);
				FastBufferWriter bufferWriter = __beginSendServerRpc(947748389u, serverRpcParams, RpcDelivery.Reliable);
				__endSendServerRpc(ref bufferWriter, 947748389u, serverRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Server && (networkManager.IsServer || networkManager.IsHost))
			{
				EquipItemClientRpc();
			}
		}
	}

	[ClientRpc]
	private void EquipItemClientRpc()
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Client && (networkManager.IsServer || networkManager.IsHost))
			{
				ClientRpcParams clientRpcParams = default(ClientRpcParams);
				FastBufferWriter bufferWriter = __beginSendClientRpc(1898191537u, clientRpcParams, RpcDelivery.Reliable);
				__endSendClientRpc(ref bufferWriter, 1898191537u, clientRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Client && (networkManager.IsClient || networkManager.IsHost) && !base.IsOwner)
			{
				EquipItem();
			}
		}
	}

	[ServerRpc(RequireOwnership = false)]
	private void PocketItemServerRpc()
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Server && (networkManager.IsClient || networkManager.IsHost))
			{
				ServerRpcParams serverRpcParams = default(ServerRpcParams);
				FastBufferWriter bufferWriter = __beginSendServerRpc(101807903u, serverRpcParams, RpcDelivery.Reliable);
				__endSendServerRpc(ref bufferWriter, 101807903u, serverRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Server && (networkManager.IsServer || networkManager.IsHost))
			{
				PocketItemClientRpc();
			}
		}
	}

	[ClientRpc]
	private void PocketItemClientRpc()
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Client && (networkManager.IsServer || networkManager.IsHost))
			{
				ClientRpcParams clientRpcParams = default(ClientRpcParams);
				FastBufferWriter bufferWriter = __beginSendClientRpc(3399384424u, clientRpcParams, RpcDelivery.Reliable);
				__endSendClientRpc(ref bufferWriter, 3399384424u, clientRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Client && (networkManager.IsClient || networkManager.IsHost) && !base.IsOwner)
			{
				PocketItem();
			}
		}
	}

	public void ChangeOwnershipOfProp(ulong clientId)
	{
		ChangeOwnershipOfPropServerRpc(clientId);
	}

	[ServerRpc(RequireOwnership = false)]
	private void ChangeOwnershipOfPropServerRpc(ulong NewOwner)
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Server && (networkManager.IsClient || networkManager.IsHost))
		{
			ServerRpcParams serverRpcParams = default(ServerRpcParams);
			FastBufferWriter bufferWriter = __beginSendServerRpc(1391130874u, serverRpcParams, RpcDelivery.Reliable);
			BytePacker.WriteValueBitPacked(bufferWriter, NewOwner);
			__endSendServerRpc(ref bufferWriter, 1391130874u, serverRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage != __RpcExecStage.Server || (!networkManager.IsServer && !networkManager.IsHost))
		{
			return;
		}
		try
		{
			base.gameObject.GetComponent<NetworkRigidbodyModifiable>().kinematicOnOwner = true;
			base.transform.SetParent(playerHeldBy.localItemHolder, worldPositionStays: true);
			base.gameObject.GetComponent<ClientNetworkTransform>().InLocalSpace = true;
			base.transform.localPosition = Vector3.zero;
			base.transform.localEulerAngles = Vector3.zero;
			playerHeldBy.grabSetParentServer = false;
			base.gameObject.GetComponent<NetworkObject>().ChangeOwnership(NewOwner);
		}
		catch (Exception arg)
		{
			Debug.Log($"Failed to transfer ownership of prop to client: {arg}");
		}
	}

	public void EnableItemMeshes(bool enable)
	{
		MeshRenderer[] componentsInChildren = base.gameObject.GetComponentsInChildren<MeshRenderer>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			if (!componentsInChildren[i].gameObject.CompareTag("DoNotSet") && !componentsInChildren[i].gameObject.CompareTag("InteractTrigger"))
			{
				componentsInChildren[i].enabled = enable;
			}
		}
		SkinnedMeshRenderer[] componentsInChildren2 = base.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
		for (int j = 0; j < componentsInChildren2.Length; j++)
		{
			componentsInChildren2[j].enabled = enable;
			Debug.Log("DISABLING/ENABLING SKINNEDMESH: " + componentsInChildren2[j].gameObject.name);
		}
	}

	public Vector3 GetItemFloorPosition(Vector3 startPosition = default(Vector3))
	{
		if (startPosition == Vector3.zero)
		{
			startPosition = base.transform.position;
		}
		if (Physics.Raycast(startPosition, -Vector3.up, out var hitInfo, 80f, 268437761, QueryTriggerInteraction.Ignore))
		{
			return hitInfo.point + Vector3.up * 0.04f + itemProperties.verticalOffset * Vector3.up;
		}
		return startPosition;
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	[RuntimeInitializeOnLoadMethod]
	internal static void InitializeRPCS_GrabbableObject()
	{
		NetworkManager.__rpc_func_table.Add(3484508350u, __rpc_handler_3484508350);
		NetworkManager.__rpc_func_table.Add(2670202430u, __rpc_handler_2670202430);
		NetworkManager.__rpc_func_table.Add(1469591241u, __rpc_handler_1469591241);
		NetworkManager.__rpc_func_table.Add(3081511085u, __rpc_handler_3081511085);
		NetworkManager.__rpc_func_table.Add(2618697776u, __rpc_handler_2618697776);
		NetworkManager.__rpc_func_table.Add(1334815929u, __rpc_handler_1334815929);
		NetworkManager.__rpc_func_table.Add(4280509730u, __rpc_handler_4280509730);
		NetworkManager.__rpc_func_table.Add(1761213193u, __rpc_handler_1761213193);
		NetworkManager.__rpc_func_table.Add(1974688543u, __rpc_handler_1974688543);
		NetworkManager.__rpc_func_table.Add(335835173u, __rpc_handler_335835173);
		NetworkManager.__rpc_func_table.Add(2025123357u, __rpc_handler_2025123357);
		NetworkManager.__rpc_func_table.Add(738171084u, __rpc_handler_738171084);
		NetworkManager.__rpc_func_table.Add(947748389u, __rpc_handler_947748389);
		NetworkManager.__rpc_func_table.Add(1898191537u, __rpc_handler_1898191537);
		NetworkManager.__rpc_func_table.Add(101807903u, __rpc_handler_101807903);
		NetworkManager.__rpc_func_table.Add(3399384424u, __rpc_handler_3399384424);
		NetworkManager.__rpc_func_table.Add(1391130874u, __rpc_handler_1391130874);
	}

	private static void __rpc_handler_3484508350(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
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
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			target.__rpc_exec_stage = __RpcExecStage.Server;
			((GrabbableObject)target).SyncBatteryServerRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	private static void __rpc_handler_2670202430(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			target.__rpc_exec_stage = __RpcExecStage.Client;
			((GrabbableObject)target).SyncBatteryClientRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	private static void __rpc_handler_1469591241(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out bool value, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Server;
			((GrabbableObject)target).InteractLeftRightServerRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	private static void __rpc_handler_3081511085(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out bool value, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Client;
			((GrabbableObject)target).InteractLeftRightClientRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	private static void __rpc_handler_2618697776(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Server;
			((GrabbableObject)target).GrabServerRpc();
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	private static void __rpc_handler_1334815929(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Client;
			((GrabbableObject)target).GrabClientRpc();
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	private static void __rpc_handler_4280509730(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out bool value, default(FastBufferWriter.ForPrimitives));
			reader.ReadValueSafe(out bool value2, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Server;
			((GrabbableObject)target).ActivateItemServerRpc(value, value2);
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	private static void __rpc_handler_1761213193(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out bool value, default(FastBufferWriter.ForPrimitives));
			reader.ReadValueSafe(out bool value2, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Client;
			((GrabbableObject)target).ActivateItemClientRpc(value, value2);
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	private static void __rpc_handler_1974688543(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Server;
			((GrabbableObject)target).DiscardItemServerRpc();
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	private static void __rpc_handler_335835173(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Client;
			((GrabbableObject)target).DiscardItemClientRpc();
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	private static void __rpc_handler_2025123357(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Server;
			((GrabbableObject)target).UseUpItemBatteriesServerRpc();
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	private static void __rpc_handler_738171084(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Client;
			((GrabbableObject)target).UseUpItemBatteriesClientRpc();
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	private static void __rpc_handler_947748389(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Server;
			((GrabbableObject)target).EquipItemServerRpc();
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	private static void __rpc_handler_1898191537(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Client;
			((GrabbableObject)target).EquipItemClientRpc();
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	private static void __rpc_handler_101807903(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Server;
			((GrabbableObject)target).PocketItemServerRpc();
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	private static void __rpc_handler_3399384424(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Client;
			((GrabbableObject)target).PocketItemClientRpc();
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	private static void __rpc_handler_1391130874(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out ulong value);
			target.__rpc_exec_stage = __RpcExecStage.Server;
			((GrabbableObject)target).ChangeOwnershipOfPropServerRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	protected internal override string __getTypeName()
	{
		return "GrabbableObject";
	}
}
