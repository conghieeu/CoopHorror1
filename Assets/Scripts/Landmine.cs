using System.Collections;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

public class Landmine : NetworkBehaviour, IHittable
{
	private bool mineActivated = true;

	public bool hasExploded;

	public ParticleSystem explosionParticle;

	public Animator mineAnimator;

	public AudioSource mineAudio;

	public AudioSource mineFarAudio;

	public AudioClip mineDetonate;

	public AudioClip mineTrigger;

	public AudioClip mineDetonateFar;

	public AudioClip mineDeactivate;

	public AudioClip minePress;

	private bool sendingExplosionRPC;

	private RaycastHit hit;

	private RoundManager roundManager;

	private float pressMineDebounceTimer;

	private bool localPlayerOnMine;

	private void Start()
	{
		StartCoroutine(StartIdleAnimation());
	}

	private void Update()
	{
		if (pressMineDebounceTimer > 0f)
		{
			pressMineDebounceTimer -= Time.deltaTime;
		}
		if (localPlayerOnMine && GameNetworkManager.Instance.localPlayerController.teleportedLastFrame)
		{
			localPlayerOnMine = false;
			TriggerMineOnLocalClientByExiting();
		}
	}

	public void ToggleMine(bool enabled)
	{
		if (mineActivated != enabled)
		{
			mineActivated = enabled;
			if (!enabled)
			{
				mineAudio.PlayOneShot(mineDeactivate);
				WalkieTalkie.TransmitOneShotAudio(mineAudio, mineDeactivate);
			}
			ToggleMineServerRpc(enabled);
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void ToggleMineServerRpc(bool enable)
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Server && (networkManager.IsClient || networkManager.IsHost))
			{
				ServerRpcParams serverRpcParams = default(ServerRpcParams);
				FastBufferWriter bufferWriter = __beginSendServerRpc(2763604698u, serverRpcParams, RpcDelivery.Reliable);
				bufferWriter.WriteValueSafe(in enable, default(FastBufferWriter.ForPrimitives));
				__endSendServerRpc(ref bufferWriter, 2763604698u, serverRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Server && (networkManager.IsServer || networkManager.IsHost))
			{
				ToggleMineClientRpc(enable);
			}
		}
	}

	[ClientRpc]
	public void ToggleMineClientRpc(bool enable)
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Client && (networkManager.IsServer || networkManager.IsHost))
			{
				ClientRpcParams clientRpcParams = default(ClientRpcParams);
				FastBufferWriter bufferWriter = __beginSendClientRpc(3479956057u, clientRpcParams, RpcDelivery.Reliable);
				bufferWriter.WriteValueSafe(in enable, default(FastBufferWriter.ForPrimitives));
				__endSendClientRpc(ref bufferWriter, 3479956057u, clientRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Client && (networkManager.IsClient || networkManager.IsHost))
			{
				ToggleMineEnabledLocalClient(enable);
			}
		}
	}

	public void ToggleMineEnabledLocalClient(bool enabled)
	{
		if (mineActivated != enabled)
		{
			mineActivated = enabled;
			if (!enabled)
			{
				mineAudio.PlayOneShot(mineDeactivate);
				WalkieTalkie.TransmitOneShotAudio(mineAudio, mineDeactivate);
			}
		}
	}

	private IEnumerator StartIdleAnimation()
	{
		roundManager = Object.FindObjectOfType<RoundManager>();
		if (!(roundManager == null))
		{
			if (roundManager.BreakerBoxRandom != null)
			{
				yield return new WaitForSeconds((float)roundManager.BreakerBoxRandom.NextDouble() + 0.5f);
			}
			mineAnimator.SetTrigger("startIdle");
			mineAudio.pitch = Random.Range(0.9f, 1.1f);
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (hasExploded || pressMineDebounceTimer > 0f)
		{
			return;
		}
		Debug.Log(string.Format("Trigger entered mine: {0}; {1}; {2}", other.tag, other.CompareTag("Player"), other.CompareTag("PhysicsProp") || other.tag.StartsWith("PlayerRagdoll")));
		if (other.CompareTag("Player"))
		{
			PlayerControllerB component = other.gameObject.GetComponent<PlayerControllerB>();
			if (!(component != GameNetworkManager.Instance.localPlayerController) && component != null && !component.isPlayerDead)
			{
				localPlayerOnMine = true;
				pressMineDebounceTimer = 0.5f;
				PressMineServerRpc();
			}
		}
		else
		{
			if (!other.CompareTag("PhysicsProp") && !other.tag.StartsWith("PlayerRagdoll"))
			{
				return;
			}
			if ((bool)other.GetComponent<DeadBodyInfo>())
			{
				if (other.GetComponent<DeadBodyInfo>().playerScript != GameNetworkManager.Instance.localPlayerController)
				{
					return;
				}
			}
			else if ((bool)other.GetComponent<GrabbableObject>() && !other.GetComponent<GrabbableObject>().NetworkObject.IsOwner)
			{
				return;
			}
			pressMineDebounceTimer = 0.5f;
			PressMineServerRpc();
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void PressMineServerRpc()
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Server && (networkManager.IsClient || networkManager.IsHost))
			{
				ServerRpcParams serverRpcParams = default(ServerRpcParams);
				FastBufferWriter bufferWriter = __beginSendServerRpc(4224840819u, serverRpcParams, RpcDelivery.Reliable);
				__endSendServerRpc(ref bufferWriter, 4224840819u, serverRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Server && (networkManager.IsServer || networkManager.IsHost))
			{
				PressMineClientRpc();
			}
		}
	}

	[ClientRpc]
	public void PressMineClientRpc()
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Client && (networkManager.IsServer || networkManager.IsHost))
			{
				ClientRpcParams clientRpcParams = default(ClientRpcParams);
				FastBufferWriter bufferWriter = __beginSendClientRpc(2652432181u, clientRpcParams, RpcDelivery.Reliable);
				__endSendClientRpc(ref bufferWriter, 2652432181u, clientRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Client && (networkManager.IsClient || networkManager.IsHost))
			{
				pressMineDebounceTimer = 0.5f;
				mineAudio.PlayOneShot(minePress);
				WalkieTalkie.TransmitOneShotAudio(mineAudio, minePress);
			}
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (hasExploded || !mineActivated)
		{
			return;
		}
		Debug.Log("Object leaving mine trigger, gameobject name: " + other.gameObject.name);
		Debug.Log(string.Format("Trigger exited mine: {0}; ({1} / {2}) {3}; {4}", other.tag, other.gameObject.name, other.transform.name, other.CompareTag("Player"), other.CompareTag("PhysicsProp") || other.tag.StartsWith("PlayerRagdoll")));
		if (other.CompareTag("Player"))
		{
			PlayerControllerB component = other.gameObject.GetComponent<PlayerControllerB>();
			if (component != null && !component.isPlayerDead && !(component != GameNetworkManager.Instance.localPlayerController))
			{
				localPlayerOnMine = false;
				TriggerMineOnLocalClientByExiting();
			}
		}
		else
		{
			if (!other.tag.StartsWith("PlayerRagdoll") && !other.CompareTag("PhysicsProp"))
			{
				return;
			}
			if ((bool)other.GetComponent<DeadBodyInfo>())
			{
				if (other.GetComponent<DeadBodyInfo>().playerScript != GameNetworkManager.Instance.localPlayerController)
				{
					return;
				}
			}
			else if ((bool)other.GetComponent<GrabbableObject>() && !other.GetComponent<GrabbableObject>().NetworkObject.IsOwner)
			{
				return;
			}
			TriggerMineOnLocalClientByExiting();
		}
	}

	private void TriggerMineOnLocalClientByExiting()
	{
		if (!hasExploded)
		{
			SetOffMineAnimation();
			sendingExplosionRPC = true;
			ExplodeMineServerRpc();
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void ExplodeMineServerRpc()
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Server && (networkManager.IsClient || networkManager.IsHost))
			{
				ServerRpcParams serverRpcParams = default(ServerRpcParams);
				FastBufferWriter bufferWriter = __beginSendServerRpc(3032666565u, serverRpcParams, RpcDelivery.Reliable);
				__endSendServerRpc(ref bufferWriter, 3032666565u, serverRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Server && (networkManager.IsServer || networkManager.IsHost))
			{
				ExplodeMineClientRpc();
			}
		}
	}

	[ClientRpc]
	public void ExplodeMineClientRpc()
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Client && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default(ClientRpcParams);
			FastBufferWriter bufferWriter = __beginSendClientRpc(456724201u, clientRpcParams, RpcDelivery.Reliable);
			__endSendClientRpc(ref bufferWriter, 456724201u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Client && (networkManager.IsClient || networkManager.IsHost))
		{
			if (sendingExplosionRPC)
			{
				sendingExplosionRPC = false;
			}
			else
			{
				SetOffMineAnimation();
			}
		}
	}

	public void SetOffMineAnimation()
	{
		hasExploded = true;
		mineAnimator.SetTrigger("detonate");
		mineAudio.PlayOneShot(mineTrigger, 1f);
	}

	private IEnumerator TriggerOtherMineDelayed(Landmine mine)
	{
		if (!mine.hasExploded)
		{
			mine.mineAudio.pitch = Random.Range(0.75f, 1.07f);
			mine.hasExploded = true;
			yield return new WaitForSeconds(0.2f);
			mine.SetOffMineAnimation();
		}
	}

	public void Detonate()
	{
		mineAudio.pitch = Random.Range(0.93f, 1.07f);
		mineAudio.PlayOneShot(mineDetonate, 1f);
		SpawnExplosion(base.transform.position + Vector3.up, spawnExplosionEffect: false, 5.7f, 6.4f);
	}

	public static void SpawnExplosion(Vector3 explosionPosition, bool spawnExplosionEffect = false, float killRange = 1f, float damageRange = 1f)
	{
		Debug.Log("Spawning explosion at pos: {explosionPosition}");
		if (spawnExplosionEffect)
		{
			Object.Instantiate(StartOfRound.Instance.explosionPrefab, explosionPosition, Quaternion.Euler(-90f, 0f, 0f), RoundManager.Instance.mapPropsContainer.transform).SetActive(value: true);
		}
		float num = Vector3.Distance(GameNetworkManager.Instance.localPlayerController.transform.position, explosionPosition);
		if (num < 14f)
		{
			HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
		}
		else if (num < 25f)
		{
			HUDManager.Instance.ShakeCamera(ScreenShakeType.Small);
		}
		Collider[] array = Physics.OverlapSphere(explosionPosition, 6f, 2621448, QueryTriggerInteraction.Collide);
		PlayerControllerB playerControllerB = null;
		for (int i = 0; i < array.Length; i++)
		{
			float num2 = Vector3.Distance(explosionPosition, array[i].transform.position);
			if (num2 > 4f && Physics.Linecast(explosionPosition, array[i].transform.position + Vector3.up * 0.3f, 256, QueryTriggerInteraction.Ignore))
			{
				continue;
			}
			if (array[i].gameObject.layer == 3)
			{
				playerControllerB = array[i].gameObject.GetComponent<PlayerControllerB>();
				if (playerControllerB != null && playerControllerB.IsOwner)
				{
					if (num2 < killRange)
					{
						Vector3 bodyVelocity = (playerControllerB.gameplayCamera.transform.position - explosionPosition) * 80f / Vector3.Distance(playerControllerB.gameplayCamera.transform.position, explosionPosition);
						playerControllerB.KillPlayer(bodyVelocity, spawnBody: true, CauseOfDeath.Blast);
					}
					else if (num2 < damageRange)
					{
						playerControllerB.DamagePlayer(50);
					}
				}
			}
			else if (array[i].gameObject.layer == 21)
			{
				Landmine componentInChildren = array[i].gameObject.GetComponentInChildren<Landmine>();
				if (componentInChildren != null && !componentInChildren.hasExploded && num2 < 6f)
				{
					Debug.Log("Setting off other mine");
					componentInChildren.StartCoroutine(componentInChildren.TriggerOtherMineDelayed(componentInChildren));
				}
			}
			else if (array[i].gameObject.layer == 19)
			{
				EnemyAICollisionDetect componentInChildren2 = array[i].gameObject.GetComponentInChildren<EnemyAICollisionDetect>();
				if (componentInChildren2 != null && componentInChildren2.mainScript.IsOwner && num2 < 4.5f)
				{
					componentInChildren2.mainScript.HitEnemyOnLocalClient(6);
				}
			}
		}
		int num3 = ~LayerMask.GetMask("Room");
		num3 = ~LayerMask.GetMask("Colliders");
		array = Physics.OverlapSphere(explosionPosition, 10f, num3);
		for (int j = 0; j < array.Length; j++)
		{
			Rigidbody component = array[j].GetComponent<Rigidbody>();
			if (component != null)
			{
				component.AddExplosionForce(70f, explosionPosition, 10f);
			}
		}
	}

	public bool MineHasLineOfSight(Vector3 pos)
	{
		return !Physics.Linecast(base.transform.position, pos, out hit, 256);
	}

	bool IHittable.Hit(int force, Vector3 hitDirection, PlayerControllerB playerWhoHit = null, bool playHitSFX = false)
	{
		SetOffMineAnimation();
		sendingExplosionRPC = true;
		ExplodeMineServerRpc();
		return true;
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	[RuntimeInitializeOnLoadMethod]
	internal static void InitializeRPCS_Landmine()
	{
		NetworkManager.__rpc_func_table.Add(2763604698u, __rpc_handler_2763604698);
		NetworkManager.__rpc_func_table.Add(3479956057u, __rpc_handler_3479956057);
		NetworkManager.__rpc_func_table.Add(4224840819u, __rpc_handler_4224840819);
		NetworkManager.__rpc_func_table.Add(2652432181u, __rpc_handler_2652432181);
		NetworkManager.__rpc_func_table.Add(3032666565u, __rpc_handler_3032666565);
		NetworkManager.__rpc_func_table.Add(456724201u, __rpc_handler_456724201);
	}

	private static void __rpc_handler_2763604698(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out bool value, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Server;
			((Landmine)target).ToggleMineServerRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	private static void __rpc_handler_3479956057(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out bool value, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Client;
			((Landmine)target).ToggleMineClientRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	private static void __rpc_handler_4224840819(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Server;
			((Landmine)target).PressMineServerRpc();
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	private static void __rpc_handler_2652432181(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Client;
			((Landmine)target).PressMineClientRpc();
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	private static void __rpc_handler_3032666565(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Server;
			((Landmine)target).ExplodeMineServerRpc();
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	private static void __rpc_handler_456724201(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Client;
			((Landmine)target).ExplodeMineClientRpc();
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	protected internal override string __getTypeName()
	{
		return "Landmine";
	}
}
