using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

public class Shovel : GrabbableObject
{
	public int shovelHitForce = 1;

	public bool reelingUp;

	public bool isHoldingButton;

	private RaycastHit rayHit;

	private Coroutine reelingUpCoroutine;

	private RaycastHit[] objectsHitByShovel;

	private List<RaycastHit> objectsHitByShovelList = new List<RaycastHit>();

	public AudioClip reelUp;

	public AudioClip swing;

	public AudioClip[] hitSFX;

	public AudioSource shovelAudio;

	private PlayerControllerB previousPlayerHeldBy;

	private int shovelMask = 11012424;

	public override void ItemActivate(bool used, bool buttonDown = true)
	{
		if (playerHeldBy == null)
		{
			return;
		}
		Debug.Log($"Is player pressing down button?: {buttonDown}");
		isHoldingButton = buttonDown;
		Debug.Log("PLAYER ACTIVATED ITEM TO HIT WITH SHOVEL. Who sent this log: " + GameNetworkManager.Instance.localPlayerController.gameObject.name);
		if (!reelingUp && buttonDown)
		{
			reelingUp = true;
			previousPlayerHeldBy = playerHeldBy;
			Debug.Log($"Set previousPlayerHeldBy: {previousPlayerHeldBy}");
			if (reelingUpCoroutine != null)
			{
				StopCoroutine(reelingUpCoroutine);
			}
			reelingUpCoroutine = StartCoroutine(reelUpShovel());
		}
	}

	private IEnumerator reelUpShovel()
	{
		playerHeldBy.activatingItem = true;
		playerHeldBy.twoHanded = true;
		playerHeldBy.playerBodyAnimator.ResetTrigger("shovelHit");
		playerHeldBy.playerBodyAnimator.SetBool("reelingUp", value: true);
		shovelAudio.PlayOneShot(reelUp);
		ReelUpSFXServerRpc();
		yield return new WaitForSeconds(0.35f);
		yield return new WaitUntil(() => !isHoldingButton || !isHeld);
		SwingShovel(!isHeld);
		yield return new WaitForSeconds(0.13f);
		yield return new WaitForEndOfFrame();
		HitShovel(!isHeld);
		yield return new WaitForSeconds(0.3f);
		reelingUp = false;
		reelingUpCoroutine = null;
	}

	[ServerRpc]
	public void ReelUpSFXServerRpc()
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
			FastBufferWriter bufferWriter = __beginSendServerRpc(4113335123u, serverRpcParams, RpcDelivery.Reliable);
			__endSendServerRpc(ref bufferWriter, 4113335123u, serverRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Server && (networkManager.IsServer || networkManager.IsHost))
		{
			ReelUpSFXClientRpc();
		}
	}

	[ClientRpc]
	public void ReelUpSFXClientRpc()
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Client && (networkManager.IsServer || networkManager.IsHost))
			{
				ClientRpcParams clientRpcParams = default(ClientRpcParams);
				FastBufferWriter bufferWriter = __beginSendClientRpc(2042054613u, clientRpcParams, RpcDelivery.Reliable);
				__endSendClientRpc(ref bufferWriter, 2042054613u, clientRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Client && (networkManager.IsClient || networkManager.IsHost) && !base.IsOwner)
			{
				shovelAudio.PlayOneShot(reelUp);
			}
		}
	}

	public override void DiscardItem()
	{
		if (playerHeldBy != null)
		{
			playerHeldBy.activatingItem = false;
		}
		base.DiscardItem();
	}

	public void SwingShovel(bool cancel = false)
	{
		previousPlayerHeldBy.playerBodyAnimator.SetBool("reelingUp", value: false);
		if (!cancel)
		{
			shovelAudio.PlayOneShot(swing);
			previousPlayerHeldBy.UpdateSpecialAnimationValue(specialAnimation: true, (short)previousPlayerHeldBy.transform.localEulerAngles.y, 0.4f);
		}
	}

	public void HitShovel(bool cancel = false)
	{
		if (previousPlayerHeldBy == null)
		{
			Debug.LogError("Previousplayerheldby is null on this client when HitShovel is called.");
			return;
		}
		previousPlayerHeldBy.activatingItem = false;
		bool flag = false;
		bool flag2 = false;
		int num = -1;
		if (!cancel)
		{
			previousPlayerHeldBy.twoHanded = false;
			objectsHitByShovel = Physics.SphereCastAll(previousPlayerHeldBy.gameplayCamera.transform.position + previousPlayerHeldBy.gameplayCamera.transform.right * -0.35f, 0.8f, previousPlayerHeldBy.gameplayCamera.transform.forward, 1.5f, shovelMask, QueryTriggerInteraction.Collide);
			objectsHitByShovelList = objectsHitByShovel.OrderBy((RaycastHit x) => x.distance).ToList();
			for (int num2 = 0; num2 < objectsHitByShovelList.Count; num2++)
			{
				IHittable component;
				RaycastHit hitInfo;
				if (objectsHitByShovelList[num2].transform.gameObject.layer == 8 || objectsHitByShovelList[num2].transform.gameObject.layer == 11)
				{
					flag = true;
					string text = objectsHitByShovelList[num2].collider.gameObject.tag;
					for (int num3 = 0; num3 < StartOfRound.Instance.footstepSurfaces.Length; num3++)
					{
						if (StartOfRound.Instance.footstepSurfaces[num3].surfaceTag == text)
						{
							num = num3;
							break;
						}
					}
				}
				else if (objectsHitByShovelList[num2].transform.TryGetComponent<IHittable>(out component) && !(objectsHitByShovelList[num2].transform == previousPlayerHeldBy.transform) && (objectsHitByShovelList[num2].point == Vector3.zero || !Physics.Linecast(previousPlayerHeldBy.gameplayCamera.transform.position, objectsHitByShovelList[num2].point, out hitInfo, StartOfRound.Instance.collidersAndRoomMaskAndDefault)))
				{
					flag = true;
					Vector3 forward = previousPlayerHeldBy.gameplayCamera.transform.forward;
					Debug.DrawRay(objectsHitByShovelList[num2].point, Vector3.up * 0.25f, Color.green, 5f);
					try
					{
						component.Hit(shovelHitForce, forward, previousPlayerHeldBy, playHitSFX: true);
						flag2 = true;
					}
					catch (Exception arg)
					{
						Debug.Log($"Exception caught when hitting object with shovel from player #{previousPlayerHeldBy.playerClientId}: {arg}");
					}
				}
			}
		}
		if (flag)
		{
			RoundManager.PlayRandomClip(shovelAudio, hitSFX);
			UnityEngine.Object.FindObjectOfType<RoundManager>().PlayAudibleNoise(base.transform.position, 17f, 0.8f);
			if (!flag2 && num != -1)
			{
				shovelAudio.PlayOneShot(StartOfRound.Instance.footstepSurfaces[num].hitSurfaceSFX);
				WalkieTalkie.TransmitOneShotAudio(shovelAudio, StartOfRound.Instance.footstepSurfaces[num].hitSurfaceSFX);
			}
			playerHeldBy.playerBodyAnimator.SetTrigger("shovelHit");
			HitShovelServerRpc(num);
		}
	}

	[ServerRpc]
	public void HitShovelServerRpc(int hitSurfaceID)
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
			FastBufferWriter bufferWriter = __beginSendServerRpc(2096026133u, serverRpcParams, RpcDelivery.Reliable);
			BytePacker.WriteValueBitPacked(bufferWriter, hitSurfaceID);
			__endSendServerRpc(ref bufferWriter, 2096026133u, serverRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Server && (networkManager.IsServer || networkManager.IsHost))
		{
			HitShovelClientRpc(hitSurfaceID);
		}
	}

	[ClientRpc]
	public void HitShovelClientRpc(int hitSurfaceID)
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Client && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default(ClientRpcParams);
			FastBufferWriter bufferWriter = __beginSendClientRpc(275435223u, clientRpcParams, RpcDelivery.Reliable);
			BytePacker.WriteValueBitPacked(bufferWriter, hitSurfaceID);
			__endSendClientRpc(ref bufferWriter, 275435223u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Client && (networkManager.IsClient || networkManager.IsHost) && !base.IsOwner)
		{
			RoundManager.PlayRandomClip(shovelAudio, hitSFX);
			if (hitSurfaceID != -1)
			{
				HitSurfaceWithShovel(hitSurfaceID);
			}
		}
	}

	private void HitSurfaceWithShovel(int hitSurfaceID)
	{
		shovelAudio.PlayOneShot(StartOfRound.Instance.footstepSurfaces[hitSurfaceID].hitSurfaceSFX);
		WalkieTalkie.TransmitOneShotAudio(shovelAudio, StartOfRound.Instance.footstepSurfaces[hitSurfaceID].hitSurfaceSFX);
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	[RuntimeInitializeOnLoadMethod]
	internal static void InitializeRPCS_Shovel()
	{
		NetworkManager.__rpc_func_table.Add(4113335123u, __rpc_handler_4113335123);
		NetworkManager.__rpc_func_table.Add(2042054613u, __rpc_handler_2042054613);
		NetworkManager.__rpc_func_table.Add(2096026133u, __rpc_handler_2096026133);
		NetworkManager.__rpc_func_table.Add(275435223u, __rpc_handler_275435223);
	}

	private static void __rpc_handler_4113335123(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
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
			((Shovel)target).ReelUpSFXServerRpc();
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	private static void __rpc_handler_2042054613(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Client;
			((Shovel)target).ReelUpSFXClientRpc();
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	private static void __rpc_handler_2096026133(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
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
			((Shovel)target).HitShovelServerRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	private static void __rpc_handler_275435223(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			target.__rpc_exec_stage = __RpcExecStage.Client;
			((Shovel)target).HitShovelClientRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	protected internal override string __getTypeName()
	{
		return "Shovel";
	}
}
