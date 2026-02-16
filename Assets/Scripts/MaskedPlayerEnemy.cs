using System;
using System.Collections;
using System.Collections.Generic;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Animations.Rigging;

public class MaskedPlayerEnemy : EnemyAI
{
	public SkinnedMeshRenderer rendererLOD0;

	public SkinnedMeshRenderer rendererLOD1;

	public SkinnedMeshRenderer rendererLOD2;

	private Ray enemyRay;

	private RaycastHit enemyRayHit;

	private int currentFootstepSurfaceIndex;

	private int previousFootstepClip;

	public AudioSource movementAudio;

	private bool sprinting;

	private int previousBehaviourState = -1;

	public float walkCheckInterval;

	private Vector3 positionLastCheck;

	private Coroutine teleportCoroutine;

	public ParticleSystem teleportParticle;

	public AISearchRoutine searchForPlayers;

	private Vector3 agentLocalVelocity;

	private Vector3 previousPosition;

	private float velX;

	private float velZ;

	public Transform animationContainer;

	private Vector3 currentRandomLookDirection;

	private Vector3 focusOnPosition;

	private float verticalLookAngle;

	private float currentLookAngle;

	public Transform headTiltTarget;

	private float lookAtPositionTimer;

	private float randomLookTimer;

	private bool lostPlayerInChase;

	private float lostLOSTimer;

	private bool running;

	private bool crouching;

	[Space(3f)]
	public PlayerControllerB mimickingPlayer;

	public bool allowSpawningWithoutPlayer;

	[Space(3f)]
	public Transform lerpTarget;

	public float turnSpeedMultiplier;

	public MultiRotationConstraint lookRig1;

	public MultiRotationConstraint lookRig2;

	private float stopAndStareTimer;

	public Transform stareAtTransform;

	private bool handsOut;

	private bool inKillAnimation;

	public bool startingKillAnimationLocalClient;

	private Coroutine killAnimationCoroutine;

	private Ray playerRay;

	public MeshRenderer[] maskEyesGlow;

	public Light maskEyesGlowLight;

	public ParticleSystem maskFloodParticle;

	private PlayerControllerB lastPlayerKilled;

	private float timeLookingAtLastNoise;

	private Vector3 shipHidingSpot;

	private float staminaTimer;

	private bool runningRandomly;

	private bool enemyEnabled;

	public GameObject[] maskTypes;

	public int maskTypeIndex;

	private Vector3 mainEntrancePosition;

	private float timeAtLastUsingEntrance;

	private float interestInShipCooldown;

	private List<int> playersKilled = new List<int>();

	public override void Start()
	{
		try
		{
			agent = base.gameObject.GetComponentInChildren<NavMeshAgent>();
			skinnedMeshRenderers = base.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
			meshRenderers = base.gameObject.GetComponentsInChildren<MeshRenderer>();
			if (creatureAnimator == null)
			{
				creatureAnimator = base.gameObject.GetComponentInChildren<Animator>();
			}
			thisNetworkObject = base.gameObject.GetComponentInChildren<NetworkObject>();
			serverPosition = base.transform.position;
			thisEnemyIndex = RoundManager.Instance.numberOfEnemiesInScene;
			RoundManager.Instance.numberOfEnemiesInScene++;
			isOutside = base.transform.position.y > -80f;
			mainEntrancePosition = RoundManager.FindMainEntrancePosition(getTeleportPosition: true, isOutside);
			if (isOutside)
			{
				if (allAINodes == null || allAINodes.Length == 0)
				{
					allAINodes = GameObject.FindGameObjectsWithTag("OutsideAINode");
				}
				if (GameNetworkManager.Instance.localPlayerController != null)
				{
					EnableEnemyMesh(!StartOfRound.Instance.hangarDoorsClosed || !GameNetworkManager.Instance.localPlayerController.isInHangarShipRoom);
				}
			}
			else if (allAINodes == null || allAINodes.Length == 0)
			{
				allAINodes = GameObject.FindGameObjectsWithTag("AINode");
			}
			path1 = new NavMeshPath();
			openDoorSpeedMultiplier = enemyType.doorSpeedMultiplier;
			if (base.IsOwner)
			{
				SyncPositionToClients();
			}
			else
			{
				SetClientCalculatingAI(enable: false);
			}
		}
		catch (Exception arg)
		{
			Debug.LogError($"Error when initializing enemy variables for {base.gameObject.name} : {arg}");
		}
		lerpTarget.SetParent(RoundManager.Instance.mapPropsContainer.transform);
		enemyRayHit = default(RaycastHit);
		addPlayerVelocityToDestination = 3f;
		if (base.IsServer && mimickingPlayer == null)
		{
			SetEnemyAsHavingNoPlayerServerRpc();
		}
	}

	[ServerRpc]
	public void SetEnemyAsHavingNoPlayerServerRpc()
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
			FastBufferWriter bufferWriter = __beginSendServerRpc(3110137062u, serverRpcParams, RpcDelivery.Reliable);
			__endSendServerRpc(ref bufferWriter, 3110137062u, serverRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Server && (networkManager.IsServer || networkManager.IsHost))
		{
			SetEnemyAsHavingNoPlayerClientRpc();
		}
	}

	[ClientRpc]
	public void SetEnemyAsHavingNoPlayerClientRpc()
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Client && (networkManager.IsServer || networkManager.IsHost))
			{
				ClientRpcParams clientRpcParams = default(ClientRpcParams);
				FastBufferWriter bufferWriter = __beginSendClientRpc(1038760037u, clientRpcParams, RpcDelivery.Reliable);
				__endSendClientRpc(ref bufferWriter, 1038760037u, clientRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Client && (networkManager.IsClient || networkManager.IsHost))
			{
				allowSpawningWithoutPlayer = true;
			}
		}
	}

	private void Awake()
	{
		SetVisibilityOfMaskedEnemy();
	}

	private void LookAndRunRandomly(bool canStartRunning = false, bool onlySetRunning = false)
	{
		randomLookTimer -= AIIntervalTime;
		if (!runningRandomly && !running)
		{
			staminaTimer = Mathf.Min(6f, staminaTimer + AIIntervalTime);
		}
		else
		{
			staminaTimer = Mathf.Max(0f, staminaTimer - AIIntervalTime);
		}
		if (!(randomLookTimer <= 0f))
		{
			return;
		}
		randomLookTimer = UnityEngine.Random.Range(0.7f, 5f);
		if (!runningRandomly)
		{
			int num = ((!isOutside) ? 20 : 35);
			if (onlySetRunning)
			{
				num /= 3;
			}
			if (staminaTimer >= 5f && UnityEngine.Random.Range(0, 100) < num)
			{
				running = true;
				runningRandomly = true;
				creatureAnimator.SetBool("Running", value: true);
				SetRunningServerRpc(running: true);
			}
			else if (!onlySetRunning)
			{
				Vector3 onUnitSphere = UnityEngine.Random.onUnitSphere;
				float y = 0f;
				if (Physics.Raycast(eye.position, onUnitSphere, 5f, StartOfRound.Instance.collidersRoomMaskDefaultAndPlayers))
				{
					y = RoundManager.Instance.YRotationThatFacesTheFarthestFromPosition(eye.position, 12f, 5);
				}
				onUnitSphere.y = y;
				LookAtDirectionServerRpc(onUnitSphere, UnityEngine.Random.Range(0.25f, 2f), UnityEngine.Random.Range(-60f, 60f));
			}
		}
		else
		{
			int num2 = ((!isOutside) ? 30 : 80);
			if (onlySetRunning)
			{
				num2 /= 5;
			}
			if (UnityEngine.Random.Range(0, 100) > num2 || staminaTimer <= 0f)
			{
				running = false;
				runningRandomly = false;
				staminaTimer = -6f;
				creatureAnimator.SetBool("Running", value: false);
				SetRunningServerRpc(running: false);
			}
		}
	}

	private void TeleportMaskedEnemyAndSync(Vector3 pos, bool setOutside)
	{
		if (base.IsOwner)
		{
			TeleportMaskedEnemy(pos, setOutside);
			TeleportMaskedEnemyServerRpc(pos, setOutside);
		}
	}

	[ServerRpc]
	public void TeleportMaskedEnemyServerRpc(Vector3 pos, bool setOutside)
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
			FastBufferWriter bufferWriter = __beginSendServerRpc(657232826u, serverRpcParams, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in pos);
			bufferWriter.WriteValueSafe(in setOutside, default(FastBufferWriter.ForPrimitives));
			__endSendServerRpc(ref bufferWriter, 657232826u, serverRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Server && (networkManager.IsServer || networkManager.IsHost))
		{
			TeleportMaskedEnemyClientRpc(pos, setOutside);
		}
	}

	[ClientRpc]
	public void TeleportMaskedEnemyClientRpc(Vector3 pos, bool setOutside)
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Client && (networkManager.IsServer || networkManager.IsHost))
			{
				ClientRpcParams clientRpcParams = default(ClientRpcParams);
				FastBufferWriter bufferWriter = __beginSendClientRpc(2539470808u, clientRpcParams, RpcDelivery.Reliable);
				bufferWriter.WriteValueSafe(in pos);
				bufferWriter.WriteValueSafe(in setOutside, default(FastBufferWriter.ForPrimitives));
				__endSendClientRpc(ref bufferWriter, 2539470808u, clientRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Client && (networkManager.IsClient || networkManager.IsHost) && !base.IsOwner)
			{
				TeleportMaskedEnemy(pos, setOutside);
			}
		}
	}

	private void TeleportMaskedEnemy(Vector3 pos, bool setOutside)
	{
		timeAtLastUsingEntrance = Time.realtimeSinceStartup;
		Vector3 navMeshPosition = RoundManager.Instance.GetNavMeshPosition(pos);
		if (base.IsOwner)
		{
			agent.enabled = false;
			base.transform.position = navMeshPosition;
			agent.enabled = true;
		}
		else
		{
			base.transform.position = navMeshPosition;
		}
		serverPosition = navMeshPosition;
		SetEnemyOutside(setOutside);
		EntranceTeleport entranceTeleport = RoundManager.FindMainEntranceScript(setOutside);
		if (entranceTeleport.doorAudios != null && entranceTeleport.doorAudios.Length != 0)
		{
			entranceTeleport.entrancePointAudio.PlayOneShot(entranceTeleport.doorAudios[0]);
			WalkieTalkie.TransmitOneShotAudio(entranceTeleport.entrancePointAudio, entranceTeleport.doorAudios[0]);
		}
	}

	public override void DoAIInterval()
	{
		base.DoAIInterval();
		if (isEnemyDead)
		{
			agent.speed = 0f;
			return;
		}
		PlayerControllerB playerControllerB = null;
		switch (currentBehaviourStateIndex)
		{
		case 0:
			LookAndRunRandomly(canStartRunning: true);
			if (Time.realtimeSinceStartup - timeAtLastUsingEntrance > 3f && !GetClosestPlayer() && !PathIsIntersectedByLineOfSight(mainEntrancePosition, calculatePathDistance: false, avoidLineOfSight: false))
			{
				if (Vector3.Distance(base.transform.position, mainEntrancePosition) < 1f)
				{
					TeleportMaskedEnemyAndSync(RoundManager.FindMainEntrancePosition(getTeleportPosition: true, !isOutside), !isOutside);
					return;
				}
				if (searchForPlayers.inProgress)
				{
					StopSearch(searchForPlayers);
				}
				SetDestinationToPosition(mainEntrancePosition);
				return;
			}
			if (!searchForPlayers.inProgress)
			{
				StartSearch(base.transform.position, searchForPlayers);
			}
			playerControllerB = CheckLineOfSightForClosestPlayer();
			if (playerControllerB != null)
			{
				LookAtPlayerServerRpc((int)playerControllerB.playerClientId);
				SetMovingTowardsTargetPlayer(playerControllerB);
				SwitchToBehaviourState(1);
				break;
			}
			interestInShipCooldown += AIIntervalTime;
			if (interestInShipCooldown >= 17f && Vector3.Distance(base.transform.position, StartOfRound.Instance.elevatorTransform.position) < 22f)
			{
				SwitchToBehaviourState(2);
			}
			break;
		case 1:
			LookAndRunRandomly(canStartRunning: true, onlySetRunning: true);
			playerControllerB = CheckLineOfSightForClosestPlayer(70f, 50, 1, 3f);
			if (playerControllerB != null)
			{
				lostPlayerInChase = false;
				lostLOSTimer = 0f;
				if (playerControllerB != targetPlayer)
				{
					SetMovingTowardsTargetPlayer(playerControllerB);
					LookAtPlayerServerRpc((int)playerControllerB.playerClientId);
				}
				if (mostOptimalDistance > 17f)
				{
					if (handsOut)
					{
						handsOut = false;
						SetHandsOutServerRpc(setOut: false);
					}
					if (!running)
					{
						running = true;
						creatureAnimator.SetBool("Running", value: true);
						Debug.Log(string.Format("Setting running to true 8; {0}", creatureAnimator.GetBool("Running")));
						SetRunningServerRpc(running: true);
					}
				}
				else if (mostOptimalDistance < 6f)
				{
					if (!handsOut)
					{
						handsOut = true;
						SetHandsOutServerRpc(setOut: true);
					}
				}
				else if (mostOptimalDistance < 12f)
				{
					if (handsOut)
					{
						handsOut = false;
						SetHandsOutServerRpc(setOut: false);
					}
					if (running && !runningRandomly)
					{
						running = false;
						creatureAnimator.SetBool("Running", value: false);
						Debug.Log(string.Format("Setting running to false 1; {0}", creatureAnimator.GetBool("Running")));
						SetRunningServerRpc(running: false);
					}
				}
				break;
			}
			lostLOSTimer += AIIntervalTime;
			if (lostLOSTimer > 10f)
			{
				SwitchToBehaviourState(0);
				targetPlayer = null;
			}
			else if (lostLOSTimer > 3.5f)
			{
				lostPlayerInChase = true;
				StopLookingAtTransformServerRpc();
				targetPlayer = null;
				if (running)
				{
					running = false;
					creatureAnimator.SetBool("Running", value: false);
					Debug.Log(string.Format("Setting running to false 2; {0}", creatureAnimator.GetBool("Running")));
					SetRunningServerRpc(running: false);
				}
				if (handsOut)
				{
					handsOut = false;
					SetHandsOutServerRpc(setOut: false);
				}
			}
			break;
		case 2:
		{
			if (!isInsidePlayerShip)
			{
				interestInShipCooldown -= AIIntervalTime;
			}
			if (Vector3.Distance(base.transform.position, StartOfRound.Instance.insideShipPositions[0].position) > 27f || interestInShipCooldown <= 0f)
			{
				SwitchToBehaviourState(0);
				break;
			}
			PlayerControllerB closestPlayer = GetClosestPlayer();
			if (closestPlayer != null)
			{
				PlayerControllerB playerControllerB2 = CheckLineOfSightForClosestPlayer(70f, 20, 0);
				if (playerControllerB2 != null)
				{
					if (stareAtTransform != playerControllerB2.gameplayCamera.transform)
					{
						LookAtPlayerServerRpc((int)playerControllerB2.playerClientId);
					}
					SetMovingTowardsTargetPlayer(playerControllerB2);
					SwitchToBehaviourState(1);
				}
				else if (isInsidePlayerShip && closestPlayer.HasLineOfSightToPosition(base.transform.position + Vector3.up * 0.7f, 4f, 20))
				{
					if (stareAtTransform != closestPlayer.gameplayCamera.transform)
					{
						LookAtPlayerServerRpc((int)closestPlayer.playerClientId);
					}
					SetMovingTowardsTargetPlayer(closestPlayer);
					SwitchToBehaviourState(1);
				}
				else if (mostOptimalDistance < 6f)
				{
					if (stareAtTransform != closestPlayer.gameplayCamera.transform)
					{
						stareAtTransform = closestPlayer.gameplayCamera.transform;
						LookAtPlayerServerRpc((int)closestPlayer.playerClientId);
					}
				}
				else if (mostOptimalDistance > 12f && stareAtTransform != null)
				{
					stareAtTransform = null;
					StopLookingAtTransformServerRpc();
				}
			}
			SetDestinationToPosition(shipHidingSpot);
			if (!crouching && Vector3.Distance(base.transform.position, shipHidingSpot) < 0.4f)
			{
				agent.speed = 0f;
				crouching = true;
				SetCrouchingServerRpc(setOut: true);
			}
			else if (crouching && Vector3.Distance(base.transform.position, shipHidingSpot) > 1f)
			{
				crouching = false;
				SetCrouchingServerRpc(setOut: false);
			}
			break;
		}
		}
		if (!(targetPlayer != null) || !PlayerIsTargetable(targetPlayer) || (currentBehaviourStateIndex != 1 && currentBehaviourStateIndex != 2))
		{
			return;
		}
		if (lostPlayerInChase)
		{
			movingTowardsTargetPlayer = false;
			if (!searchForPlayers.inProgress)
			{
				StartSearch(base.transform.position, searchForPlayers);
			}
		}
		else
		{
			if (searchForPlayers.inProgress)
			{
				StopSearch(searchForPlayers);
			}
			SetMovingTowardsTargetPlayer(targetPlayer);
		}
	}

	[ServerRpc]
	public void LookAtDirectionServerRpc(Vector3 dir, float time, float vertLookAngle)
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
			FastBufferWriter bufferWriter = __beginSendServerRpc(2502006210u, serverRpcParams, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in dir);
			bufferWriter.WriteValueSafe(in time, default(FastBufferWriter.ForPrimitives));
			bufferWriter.WriteValueSafe(in vertLookAngle, default(FastBufferWriter.ForPrimitives));
			__endSendServerRpc(ref bufferWriter, 2502006210u, serverRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Server && (networkManager.IsServer || networkManager.IsHost))
		{
			LookAtDirectionClientRpc(dir, time, vertLookAngle);
		}
	}

	[ClientRpc]
	public void LookAtDirectionClientRpc(Vector3 dir, float time, float vertLookAngle)
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Client && (networkManager.IsServer || networkManager.IsHost))
			{
				ClientRpcParams clientRpcParams = default(ClientRpcParams);
				FastBufferWriter bufferWriter = __beginSendClientRpc(3625708449u, clientRpcParams, RpcDelivery.Reliable);
				bufferWriter.WriteValueSafe(in dir);
				bufferWriter.WriteValueSafe(in time, default(FastBufferWriter.ForPrimitives));
				bufferWriter.WriteValueSafe(in vertLookAngle, default(FastBufferWriter.ForPrimitives));
				__endSendClientRpc(ref bufferWriter, 3625708449u, clientRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Client && (networkManager.IsClient || networkManager.IsHost))
			{
				LookAtDirection(dir, time, vertLookAngle);
			}
		}
	}

	[ServerRpc]
	public void LookAtPositionServerRpc(Vector3 pos, float time)
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
			FastBufferWriter bufferWriter = __beginSendServerRpc(675153417u, serverRpcParams, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in pos);
			bufferWriter.WriteValueSafe(in time, default(FastBufferWriter.ForPrimitives));
			__endSendServerRpc(ref bufferWriter, 675153417u, serverRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Server && (networkManager.IsServer || networkManager.IsHost))
		{
			LookAtPositionClientRpc(pos, time);
		}
	}

	[ClientRpc]
	public void LookAtPositionClientRpc(Vector3 pos, float time)
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Client && (networkManager.IsServer || networkManager.IsHost))
			{
				ClientRpcParams clientRpcParams = default(ClientRpcParams);
				FastBufferWriter bufferWriter = __beginSendClientRpc(432295350u, clientRpcParams, RpcDelivery.Reliable);
				bufferWriter.WriteValueSafe(in pos);
				bufferWriter.WriteValueSafe(in time, default(FastBufferWriter.ForPrimitives));
				__endSendClientRpc(ref bufferWriter, 432295350u, clientRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Client && (networkManager.IsClient || networkManager.IsHost))
			{
				LookAtPosition(pos, time);
			}
		}
	}

	[ServerRpc]
	public void LookAtPlayerServerRpc(int playerId)
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
			FastBufferWriter bufferWriter = __beginSendServerRpc(1141953697u, serverRpcParams, RpcDelivery.Reliable);
			BytePacker.WriteValueBitPacked(bufferWriter, playerId);
			__endSendServerRpc(ref bufferWriter, 1141953697u, serverRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Server && (networkManager.IsServer || networkManager.IsHost))
		{
			LookAtPlayerClientRpc(playerId);
		}
	}

	[ClientRpc]
	public void LookAtPlayerClientRpc(int playerId)
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Client && (networkManager.IsServer || networkManager.IsHost))
			{
				ClientRpcParams clientRpcParams = default(ClientRpcParams);
				FastBufferWriter bufferWriter = __beginSendClientRpc(2397761797u, clientRpcParams, RpcDelivery.Reliable);
				BytePacker.WriteValueBitPacked(bufferWriter, playerId);
				__endSendClientRpc(ref bufferWriter, 2397761797u, clientRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Client && (networkManager.IsClient || networkManager.IsHost))
			{
				stareAtTransform = StartOfRound.Instance.allPlayerScripts[playerId].gameplayCamera.transform;
			}
		}
	}

	[ServerRpc]
	public void StopLookingAtTransformServerRpc()
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
			FastBufferWriter bufferWriter = __beginSendServerRpc(1407409549u, serverRpcParams, RpcDelivery.Reliable);
			__endSendServerRpc(ref bufferWriter, 1407409549u, serverRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Server && (networkManager.IsServer || networkManager.IsHost))
		{
			StopLookingAtTransformClientRpc();
		}
	}

	[ClientRpc]
	public void StopLookingAtTransformClientRpc()
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Client && (networkManager.IsServer || networkManager.IsHost))
			{
				ClientRpcParams clientRpcParams = default(ClientRpcParams);
				FastBufferWriter bufferWriter = __beginSendClientRpc(1561581057u, clientRpcParams, RpcDelivery.Reliable);
				__endSendClientRpc(ref bufferWriter, 1561581057u, clientRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Client && (networkManager.IsClient || networkManager.IsHost))
			{
				stareAtTransform = null;
			}
		}
	}

	[ServerRpc]
	public void SetHandsOutServerRpc(bool setOut)
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
			FastBufferWriter bufferWriter = __beginSendServerRpc(519961256u, serverRpcParams, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in setOut, default(FastBufferWriter.ForPrimitives));
			__endSendServerRpc(ref bufferWriter, 519961256u, serverRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Server && (networkManager.IsServer || networkManager.IsHost))
		{
			SetHandsOutClientRpc(setOut);
		}
	}

	[ClientRpc]
	public void SetHandsOutClientRpc(bool setOut)
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Client && (networkManager.IsServer || networkManager.IsHost))
			{
				ClientRpcParams clientRpcParams = default(ClientRpcParams);
				FastBufferWriter bufferWriter = __beginSendClientRpc(222504553u, clientRpcParams, RpcDelivery.Reliable);
				bufferWriter.WriteValueSafe(in setOut, default(FastBufferWriter.ForPrimitives));
				__endSendClientRpc(ref bufferWriter, 222504553u, clientRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Client && (networkManager.IsClient || networkManager.IsHost))
			{
				handsOut = setOut;
				creatureAnimator.SetBool("HandsOut", setOut);
			}
		}
	}

	[ServerRpc]
	public void SetCrouchingServerRpc(bool setOut)
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
			FastBufferWriter bufferWriter = __beginSendServerRpc(2560207573u, serverRpcParams, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in setOut, default(FastBufferWriter.ForPrimitives));
			__endSendServerRpc(ref bufferWriter, 2560207573u, serverRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Server && (networkManager.IsServer || networkManager.IsHost))
		{
			SetCrouchingClientRpc(setOut);
		}
	}

	[ClientRpc]
	public void SetCrouchingClientRpc(bool setCrouch)
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Client && (networkManager.IsServer || networkManager.IsHost))
			{
				ClientRpcParams clientRpcParams = default(ClientRpcParams);
				FastBufferWriter bufferWriter = __beginSendClientRpc(1162325818u, clientRpcParams, RpcDelivery.Reliable);
				bufferWriter.WriteValueSafe(in setCrouch, default(FastBufferWriter.ForPrimitives));
				__endSendClientRpc(ref bufferWriter, 1162325818u, clientRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Client && (networkManager.IsClient || networkManager.IsHost))
			{
				crouching = setCrouch;
				creatureAnimator.SetBool("Crouching", setCrouch);
			}
		}
	}

	public void LookAtFocusedPosition()
	{
		if (inSpecialAnimation)
		{
			verticalLookAngle = Mathf.Lerp(verticalLookAngle, 0f, 10f * Time.deltaTime);
			currentLookAngle = Mathf.Lerp(currentLookAngle, verticalLookAngle, 7f);
			headTiltTarget.localEulerAngles = new Vector3(currentLookAngle, 0f, 0f);
			return;
		}
		if (lookAtPositionTimer <= 0f)
		{
			if (stareAtTransform != null)
			{
				if (!(Vector3.Distance(stareAtTransform.position, base.transform.position) > 80f))
				{
					agent.angularSpeed = 0f;
					RoundManager.Instance.tempTransform.position = base.transform.position;
					RoundManager.Instance.tempTransform.LookAt(stareAtTransform);
					base.transform.rotation = Quaternion.Lerp(base.transform.rotation, RoundManager.Instance.tempTransform.rotation, turnSpeedMultiplier * Time.deltaTime);
					base.transform.eulerAngles = new Vector3(0f, base.transform.eulerAngles.y, 0f);
					headTiltTarget.LookAt(stareAtTransform);
					headTiltTarget.localEulerAngles = new Vector3(headTiltTarget.localEulerAngles.x, 0f, 0f);
				}
				return;
			}
			agent.angularSpeed = 450f;
			verticalLookAngle = Mathf.Clamp(verticalLookAngle, -30f, 10f);
		}
		else
		{
			agent.angularSpeed = 0f;
			lookAtPositionTimer -= Time.deltaTime;
			RoundManager.Instance.tempTransform.position = base.transform.position;
			RoundManager.Instance.tempTransform.LookAt(focusOnPosition);
			base.transform.rotation = Quaternion.Lerp(base.transform.rotation, RoundManager.Instance.tempTransform.rotation, turnSpeedMultiplier * Time.deltaTime);
			base.transform.eulerAngles = new Vector3(0f, base.transform.eulerAngles.y, 0f);
			verticalLookAngle = Mathf.Clamp(verticalLookAngle + UnityEngine.Random.Range(-3f * Time.deltaTime, 3f * Time.deltaTime), -70f, 70f);
		}
		currentLookAngle = Mathf.Lerp(currentLookAngle, verticalLookAngle, 7f);
		headTiltTarget.localEulerAngles = new Vector3(currentLookAngle, 0f, 0f);
	}

	public void LookAtDirection(Vector3 direction, float lookAtTime = 1f, float vertLookAngle = 0f)
	{
		verticalLookAngle = vertLookAngle;
		direction = Vector3.Normalize(direction * 100f);
		focusOnPosition = base.transform.position + direction * 1000f;
		lookAtPositionTimer = lookAtTime;
	}

	public void LookAtPosition(Vector3 pos, float lookAtTime = 1f)
	{
		Debug.Log($"Look at position {pos} called! lookatpositiontimer setting to {lookAtTime}");
		focusOnPosition = pos;
		lookAtPositionTimer = lookAtTime;
		float num = Vector3.Angle(base.transform.forward, pos - base.transform.position);
		if (pos.y - headTiltTarget.position.y < 0f)
		{
			num *= -1f;
		}
		verticalLookAngle = num;
	}

	[ServerRpc]
	public void SetRunningServerRpc(bool running)
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
			FastBufferWriter bufferWriter = __beginSendServerRpc(3309468324u, serverRpcParams, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in running, default(FastBufferWriter.ForPrimitives));
			__endSendServerRpc(ref bufferWriter, 3309468324u, serverRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Server && (networkManager.IsServer || networkManager.IsHost))
		{
			SetRunningClientRpc(running);
		}
	}

	[ClientRpc]
	public void SetRunningClientRpc(bool setRunning)
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Client && (networkManager.IsServer || networkManager.IsHost))
			{
				ClientRpcParams clientRpcParams = default(ClientRpcParams);
				FastBufferWriter bufferWriter = __beginSendClientRpc(3512011720u, clientRpcParams, RpcDelivery.Reliable);
				bufferWriter.WriteValueSafe(in setRunning, default(FastBufferWriter.ForPrimitives));
				__endSendClientRpc(ref bufferWriter, 3512011720u, clientRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Client && (networkManager.IsClient || networkManager.IsHost))
			{
				running = setRunning;
				creatureAnimator.SetBool("Running", setRunning);
			}
		}
	}

	private void CalculateAnimationDirection(float maxSpeed = 1f)
	{
		creatureAnimator.SetBool("IsMoving", Vector3.Distance(base.transform.position, previousPosition) > 0f);
		agentLocalVelocity = animationContainer.InverseTransformDirection(Vector3.ClampMagnitude(base.transform.position - previousPosition, 1f) / (Time.deltaTime * 2f));
		velX = Mathf.Lerp(velX, agentLocalVelocity.x, 10f * Time.deltaTime);
		creatureAnimator.SetFloat("VelocityX", Mathf.Clamp(velX, 0f - maxSpeed, maxSpeed));
		velZ = Mathf.Lerp(velZ, agentLocalVelocity.z, 10f * Time.deltaTime);
		creatureAnimator.SetFloat("VelocityZ", Mathf.Clamp(velZ, 0f - maxSpeed, maxSpeed));
		previousPosition = base.transform.position;
	}

	public override void DetectNoise(Vector3 noisePosition, float noiseLoudness, int timesPlayedInOneSpot = 0, int noiseID = 0)
	{
		base.DetectNoise(noisePosition, noiseLoudness, timesPlayedInOneSpot, noiseID);
		if (!base.IsOwner || isEnemyDead || inSpecialAnimation)
		{
			return;
		}
		if (Vector3.Distance(noisePosition, base.transform.position + Vector3.up * 0.4f) < 0.75f)
		{
			Debug.Log("Can't hear noise reason A");
			return;
		}
		if (handsOut || (stareAtTransform != null && Vector3.Distance(noisePosition, stareAtTransform.position) < 2f))
		{
			Debug.Log("Can't hear noise reason B");
			return;
		}
		float num = Vector3.Distance(noisePosition, base.transform.position);
		float num2 = noiseLoudness / num;
		Debug.Log($"Noise heard relative loudness: {num2}");
		if (!(num2 < 0.12f) && !(Time.realtimeSinceStartup - timeLookingAtLastNoise < 3f))
		{
			timeLookingAtLastNoise = Time.realtimeSinceStartup;
			LookAtPositionServerRpc(noisePosition, Mathf.Min(num2 * 6f, 2f));
		}
	}

	public void LateUpdate()
	{
		if (!(stunNormalizedTimer >= 0f) && !isEnemyDead)
		{
			LookAtFocusedPosition();
		}
	}

	public void SetVisibilityOfMaskedEnemy()
	{
		if (allowSpawningWithoutPlayer)
		{
			if (mimickingPlayer != null && mimickingPlayer.deadBody != null && !mimickingPlayer.deadBody.deactivated)
			{
				if (enemyEnabled)
				{
					enemyEnabled = false;
					EnableEnemyMesh(enable: false);
				}
			}
			else if (!enemyEnabled)
			{
				enemyEnabled = true;
				EnableEnemyMesh(enable: true);
			}
		}
		else if (mimickingPlayer == null || (mimickingPlayer.deadBody != null && !mimickingPlayer.deadBody.deactivated))
		{
			if (enemyEnabled)
			{
				enemyEnabled = false;
				EnableEnemyMesh(enable: false);
			}
		}
		else if (!enemyEnabled)
		{
			enemyEnabled = true;
			EnableEnemyMesh(enable: true);
		}
	}

	public override void Update()
	{
		base.Update();
		CalculateAnimationDirection();
		SetVisibilityOfMaskedEnemy();
		if (isEnemyDead)
		{
			agent.speed = 0f;
			if (inSpecialAnimation)
			{
				FinishKillAnimation();
			}
			return;
		}
		if (lastPlayerKilled != null && lastPlayerKilled.deadBody != null && !lastPlayerKilled.deadBody.deactivated)
		{
			Debug.Log($"Deactivating body of killed player! {lastPlayerKilled.playerClientId}; {isEnemyDead}");
			lastPlayerKilled.deadBody.DeactivateBody(setActive: false);
			lastPlayerKilled = null;
		}
		if (!enemyEnabled)
		{
			return;
		}
		if (ventAnimationFinished)
		{
			lookRig1.weight = 0.452f;
			lookRig2.weight = 1f;
			creatureAnimator.SetBool("Stunned", stunNormalizedTimer >= 0f);
			if (stunNormalizedTimer >= 0f)
			{
				agent.speed = 0f;
				if (base.IsOwner && searchForPlayers.inProgress)
				{
					StopSearch(searchForPlayers);
				}
				if (inSpecialAnimation)
				{
					FinishKillAnimation();
				}
			}
			else
			{
				if (inSpecialAnimation)
				{
					return;
				}
				if (walkCheckInterval <= 0f)
				{
					walkCheckInterval = 0.1f;
					positionLastCheck = base.transform.position;
				}
				else
				{
					walkCheckInterval -= Time.deltaTime;
				}
				switch (currentBehaviourStateIndex)
				{
				case 0:
					if (previousBehaviourState != currentBehaviourStateIndex)
					{
						stareAtTransform = null;
						running = false;
						runningRandomly = false;
						creatureAnimator.SetBool("Running", value: false);
						handsOut = false;
						creatureAnimator.SetBool("HandsOut", value: false);
						crouching = false;
						creatureAnimator.SetBool("Crouching", value: false);
						previousBehaviourState = currentBehaviourStateIndex;
					}
					if (running || runningRandomly)
					{
						agent.speed = 7f;
					}
					else
					{
						agent.speed = 3.8f;
					}
					break;
				case 1:
					if (previousBehaviourState != currentBehaviourStateIndex)
					{
						lookAtPositionTimer = 0f;
						if (previousBehaviourState == 0)
						{
							stopAndStareTimer = UnityEngine.Random.Range(2f, 5f);
						}
						runningRandomly = false;
						running = false;
						creatureAnimator.SetBool("Running", value: false);
						crouching = false;
						creatureAnimator.SetBool("Crouching", value: false);
						previousBehaviourState = currentBehaviourStateIndex;
					}
					if (!base.IsOwner)
					{
						break;
					}
					stopAndStareTimer -= Time.deltaTime;
					if (stopAndStareTimer >= 0f)
					{
						agent.speed = 0f;
						break;
					}
					if (stopAndStareTimer <= -5f)
					{
						stopAndStareTimer = UnityEngine.Random.Range(0f, 3f);
					}
					if (running || runningRandomly)
					{
						agent.speed = 8f;
					}
					else
					{
						agent.speed = 3.8f;
					}
					break;
				case 2:
					if (previousBehaviourState != currentBehaviourStateIndex)
					{
						movingTowardsTargetPlayer = false;
						interestInShipCooldown = 17f;
						agent.speed = 5f;
						runningRandomly = false;
						running = false;
						creatureAnimator.SetBool("Running", value: false);
						handsOut = false;
						creatureAnimator.SetBool("HandsOut", value: false);
						if (base.IsOwner)
						{
							ChooseShipHidingSpot();
						}
						previousBehaviourState = currentBehaviourStateIndex;
					}
					break;
				}
			}
		}
		else
		{
			lookRig1.weight = 0f;
			lookRig2.weight = 0f;
		}
	}

	private void ChooseShipHidingSpot()
	{
		bool flag = false;
		for (int i = 0; i < StartOfRound.Instance.insideShipPositions.Length; i++)
		{
			if (Physics.Linecast(StartOfRound.Instance.shipDoorAudioSource.transform.position, StartOfRound.Instance.insideShipPositions[i].position, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore) && SetDestinationToPosition(StartOfRound.Instance.insideShipPositions[i].position, checkForPath: true))
			{
				flag = true;
				shipHidingSpot = destination;
				break;
			}
		}
		if (!flag)
		{
			shipHidingSpot = StartOfRound.Instance.insideShipPositions[UnityEngine.Random.Range(0, StartOfRound.Instance.insideShipPositions.Length)].position;
		}
	}

	public override void ShipTeleportEnemy()
	{
		base.ShipTeleportEnemy();
		if (teleportCoroutine != null)
		{
			StopCoroutine(teleportCoroutine);
		}
		StartCoroutine(teleportMasked());
	}

	private IEnumerator teleportMasked()
	{
		teleportParticle.Play();
		movementAudio.PlayOneShot(UnityEngine.Object.FindObjectOfType<ShipTeleporter>().beamUpPlayerBodySFX);
		yield return new WaitForSeconds(3f);
		if (StartOfRound.Instance.shipIsLeaving)
		{
			yield break;
		}
		SetEnemyOutside(outside: true);
		isInsidePlayerShip = true;
		ShipTeleporter[] array = UnityEngine.Object.FindObjectsOfType<ShipTeleporter>();
		ShipTeleporter shipTeleporter = null;
		if (array != null)
		{
			for (int i = 0; i < array.Length; i++)
			{
				if (!array[i].isInverseTeleporter)
				{
					shipTeleporter = array[i];
				}
			}
		}
		if (shipTeleporter != null)
		{
			if (base.IsOwner)
			{
				agent.enabled = false;
				base.transform.position = shipTeleporter.teleporterPosition.position;
				agent.enabled = true;
				isInsidePlayerShip = true;
			}
			serverPosition = shipTeleporter.teleporterPosition.position;
		}
	}

	public void SetEnemyOutside(bool outside = false)
	{
		isOutside = outside;
		mainEntrancePosition = RoundManager.FindMainEntrancePosition(getTeleportPosition: true, isOutside);
		if (outside)
		{
			allAINodes = GameObject.FindGameObjectsWithTag("OutsideAINode");
		}
		else
		{
			allAINodes = GameObject.FindGameObjectsWithTag("AINode");
		}
		if (searchForPlayers.inProgress)
		{
			StopSearch(searchForPlayers);
		}
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
	}

	public override void KillEnemy(bool destroy = false)
	{
		base.KillEnemy(destroy);
		creatureAnimator.SetBool("Stunned", value: false);
		creatureAnimator.SetBool("Dead", value: true);
	}

	public override void HitEnemy(int force = 1, PlayerControllerB playerWhoHit = null, bool playHitSFX = false)
	{
		base.HitEnemy(force, playerWhoHit, playHitSFX);
		enemyHP -= force;
		stunNormalizedTimer = 0.5f;
		creatureAnimator.SetTrigger("HitEnemy");
		stopAndStareTimer = 0f;
		if (((float)UnityEngine.Random.Range(0, 100) < 40f || enemyHP == 1) && !running)
		{
			running = true;
			runningRandomly = true;
			creatureAnimator.SetBool("Running", value: true);
			SetRunningServerRpc(running: true);
			staminaTimer = 5f;
		}
		if (enemyHP <= 0)
		{
			KillEnemyOnOwnerClient();
		}
	}

	public override void OnCollideWithPlayer(Collider other)
	{
		base.OnCollideWithPlayer(other);
		if (!(stunNormalizedTimer >= 0f) && !isEnemyDead && !(Time.realtimeSinceStartup - timeAtLastUsingEntrance < 1.75f))
		{
			PlayerControllerB playerControllerB = MeetsStandardPlayerCollisionConditions(other, inKillAnimation || startingKillAnimationLocalClient || !enemyEnabled);
			if (playerControllerB != null)
			{
				startingKillAnimationLocalClient = true;
				KillPlayerAnimationServerRpc((int)playerControllerB.playerClientId);
			}
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void KillPlayerAnimationServerRpc(int playerObjectId)
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Server && (networkManager.IsClient || networkManager.IsHost))
		{
			ServerRpcParams serverRpcParams = default(ServerRpcParams);
			FastBufferWriter bufferWriter = __beginSendServerRpc(3192502457u, serverRpcParams, RpcDelivery.Reliable);
			BytePacker.WriteValueBitPacked(bufferWriter, playerObjectId);
			__endSendServerRpc(ref bufferWriter, 3192502457u, serverRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Server && (networkManager.IsServer || networkManager.IsHost))
		{
			if (!inKillAnimation && !playersKilled.Contains(playerObjectId))
			{
				inSpecialAnimationWithPlayer = StartOfRound.Instance.allPlayerScripts[playerObjectId];
				inSpecialAnimationWithPlayer.inAnimationWithEnemy = this;
				inKillAnimation = true;
				inSpecialAnimation = true;
				isClientCalculatingAI = false;
				KillPlayerAnimationClientRpc(playerObjectId);
			}
			else
			{
				CancelKillAnimationClientRpc(playerObjectId);
			}
		}
	}

	[ClientRpc]
	public void CancelKillAnimationClientRpc(int playerObjectId)
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Client && (networkManager.IsServer || networkManager.IsHost))
			{
				ClientRpcParams clientRpcParams = default(ClientRpcParams);
				FastBufferWriter bufferWriter = __beginSendClientRpc(4032958935u, clientRpcParams, RpcDelivery.Reliable);
				BytePacker.WriteValueBitPacked(bufferWriter, playerObjectId);
				__endSendClientRpc(ref bufferWriter, 4032958935u, clientRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Client && (networkManager.IsClient || networkManager.IsHost) && (int)GameNetworkManager.Instance.localPlayerController.playerClientId == playerObjectId)
			{
				startingKillAnimationLocalClient = false;
			}
		}
	}

	[ClientRpc]
	public void KillPlayerAnimationClientRpc(int playerObjectId)
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Client && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default(ClientRpcParams);
			FastBufferWriter bufferWriter = __beginSendClientRpc(3071650946u, clientRpcParams, RpcDelivery.Reliable);
			BytePacker.WriteValueBitPacked(bufferWriter, playerObjectId);
			__endSendClientRpc(ref bufferWriter, 3071650946u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage != __RpcExecStage.Client || (!networkManager.IsClient && !networkManager.IsHost))
		{
			return;
		}
		if (searchForPlayers.inProgress)
		{
			StopSearch(searchForPlayers);
		}
		inSpecialAnimationWithPlayer = StartOfRound.Instance.allPlayerScripts[playerObjectId];
		if (inSpecialAnimationWithPlayer == GameNetworkManager.Instance.localPlayerController)
		{
			startingKillAnimationLocalClient = false;
		}
		if (inSpecialAnimationWithPlayer == null || inSpecialAnimationWithPlayer.isPlayerDead || inSpecialAnimationWithPlayer.isInsideFactory != !isOutside)
		{
			FinishKillAnimation();
			return;
		}
		inSpecialAnimationWithPlayer.inAnimationWithEnemy = this;
		if (inSpecialAnimationWithPlayer == GameNetworkManager.Instance.localPlayerController)
		{
			inSpecialAnimationWithPlayer.CancelSpecialTriggerAnimations();
		}
		inKillAnimation = true;
		inSpecialAnimation = true;
		creatureAnimator.SetBool("killing", value: true);
		agent.enabled = false;
		inSpecialAnimationWithPlayer.inSpecialInteractAnimation = true;
		inSpecialAnimationWithPlayer.snapToServerPosition = true;
		Vector3 origin = ((!inSpecialAnimationWithPlayer.IsOwner) ? inSpecialAnimationWithPlayer.transform.parent.TransformPoint(inSpecialAnimationWithPlayer.serverPlayerPosition) : inSpecialAnimationWithPlayer.transform.position);
		Vector3 vector = base.transform.position - base.transform.forward * 2f;
		vector.y = origin.y;
		playerRay = new Ray(origin, vector - inSpecialAnimationWithPlayer.transform.position);
		if (killAnimationCoroutine != null)
		{
			StopCoroutine(killAnimationCoroutine);
		}
		killAnimationCoroutine = StartCoroutine(killAnimation());
	}

	private IEnumerator killAnimation()
	{
		WalkieTalkie.TransmitOneShotAudio(creatureSFX, enemyType.audioClips[0]);
		creatureSFX.PlayOneShot(enemyType.audioClips[0]);
		Vector3 endPosition = playerRay.GetPoint(0.7f);
		if (isOutside && endPosition.y < -80f)
		{
			SetEnemyOutside();
		}
		else if (!isOutside && endPosition.y > -80f)
		{
			SetEnemyOutside(outside: true);
		}
		inSpecialAnimationWithPlayer.disableSyncInAnimation = true;
		inSpecialAnimationWithPlayer.disableLookInput = true;
		RoundManager.Instance.tempTransform.position = inSpecialAnimationWithPlayer.transform.position;
		RoundManager.Instance.tempTransform.LookAt(endPosition);
		Quaternion startingPlayerRot = inSpecialAnimationWithPlayer.transform.rotation;
		Quaternion targetRot = RoundManager.Instance.tempTransform.rotation;
		Vector3 startingPosition = base.transform.position;
		for (int i = 0; i < 8; i++)
		{
			if (i > 0)
			{
				base.transform.LookAt(inSpecialAnimationWithPlayer.transform.position);
				base.transform.eulerAngles = new Vector3(0f, base.transform.eulerAngles.y, 0f);
			}
			base.transform.position = Vector3.Lerp(startingPosition, endPosition, (float)i / 8f);
			inSpecialAnimationWithPlayer.transform.rotation = Quaternion.Lerp(startingPlayerRot, targetRot, (float)i / 8f);
			inSpecialAnimationWithPlayer.transform.eulerAngles = new Vector3(0f, inSpecialAnimationWithPlayer.transform.eulerAngles.y, 0f);
			yield return null;
		}
		base.transform.position = endPosition;
		inSpecialAnimationWithPlayer.transform.rotation = targetRot;
		inSpecialAnimationWithPlayer.transform.eulerAngles = new Vector3(0f, inSpecialAnimationWithPlayer.transform.eulerAngles.y, 0f);
		yield return new WaitForSeconds(0.3f);
		SetMaskGlow(enable: true);
		yield return new WaitForSeconds(1.2f);
		maskFloodParticle.Play();
		if (inSpecialAnimationWithPlayer == GameNetworkManager.Instance.localPlayerController)
		{
			HUDManager.Instance.HUDAnimator.SetBool("biohazardDamage", value: true);
		}
		creatureSFX.PlayOneShot(enemyType.audioClips[2]);
		WalkieTalkie.TransmitOneShotAudio(creatureSFX, enemyType.audioClips[2]);
		yield return new WaitForSeconds(1.5f);
		lastPlayerKilled = inSpecialAnimationWithPlayer;
		if (inSpecialAnimationWithPlayer != null)
		{
			bool flag = inSpecialAnimationWithPlayer.transform.position.y < -80f;
			inSpecialAnimationWithPlayer.KillPlayer(Vector3.zero, spawnBody: false, CauseOfDeath.Strangulation, 4);
			inSpecialAnimationWithPlayer.snapToServerPosition = false;
			if (base.IsServer)
			{
				playersKilled.Add((int)inSpecialAnimationWithPlayer.playerClientId);
				NetworkObjectReference netObjectRef = RoundManager.Instance.SpawnEnemyGameObject(GetGroundPosition(playerRay.origin), inSpecialAnimationWithPlayer.transform.eulerAngles.y, -1, enemyType);
				if (netObjectRef.TryGet(out var networkObject))
				{
					MaskedPlayerEnemy component = networkObject.GetComponent<MaskedPlayerEnemy>();
					component.SetSuit(inSpecialAnimationWithPlayer.currentSuitID);
					component.mimickingPlayer = inSpecialAnimationWithPlayer;
					component.SetEnemyOutside(!flag);
					inSpecialAnimationWithPlayer.redirectToEnemy = component;
					if (inSpecialAnimationWithPlayer.deadBody != null)
					{
						inSpecialAnimationWithPlayer.deadBody.DeactivateBody(setActive: false);
					}
				}
				CreateMimicClientRpc(netObjectRef, flag, (int)inSpecialAnimationWithPlayer.playerClientId);
			}
			FinishKillAnimation(killedPlayer: true);
		}
		else
		{
			FinishKillAnimation();
		}
	}

	[ClientRpc]
	public void CreateMimicClientRpc(NetworkObjectReference netObjectRef, bool inFactory, int playerKilled)
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Client && (networkManager.IsServer || networkManager.IsHost))
			{
				ClientRpcParams clientRpcParams = default(ClientRpcParams);
				FastBufferWriter bufferWriter = __beginSendClientRpc(1687215509u, clientRpcParams, RpcDelivery.Reliable);
				bufferWriter.WriteValueSafe(in netObjectRef, default(FastBufferWriter.ForNetworkSerializable));
				bufferWriter.WriteValueSafe(in inFactory, default(FastBufferWriter.ForPrimitives));
				BytePacker.WriteValueBitPacked(bufferWriter, playerKilled);
				__endSendClientRpc(ref bufferWriter, 1687215509u, clientRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Client && (networkManager.IsClient || networkManager.IsHost) && !base.IsServer)
			{
				StartCoroutine(waitForMimicEnemySpawn(netObjectRef, inFactory, playerKilled));
			}
		}
	}

	private IEnumerator waitForMimicEnemySpawn(NetworkObjectReference netObjectRef, bool inFactory, int playerKilled)
	{
		PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[playerKilled];
		NetworkObject netObject = null;
		float startTime = Time.realtimeSinceStartup;
		yield return new WaitUntil(() => Time.realtimeSinceStartup - startTime > 20f || netObjectRef.TryGet(out netObject));
		if (player.deadBody == null)
		{
			startTime = Time.realtimeSinceStartup;
			yield return new WaitUntil(() => Time.realtimeSinceStartup - startTime > 20f || player.deadBody != null);
		}
		if (!(player.deadBody == null))
		{
			player.deadBody.DeactivateBody(setActive: false);
			if (netObject != null)
			{
				MaskedPlayerEnemy component = netObject.GetComponent<MaskedPlayerEnemy>();
				component.mimickingPlayer = player;
				component.SetSuit(player.currentSuitID);
				component.SetEnemyOutside(!inFactory);
				player.redirectToEnemy = component;
			}
		}
	}

	public override void CancelSpecialAnimationWithPlayer()
	{
		FinishKillAnimation();
		base.CancelSpecialAnimationWithPlayer();
		if (inSpecialAnimationWithPlayer == GameNetworkManager.Instance.localPlayerController)
		{
			HUDManager.Instance.HUDAnimator.SetBool("biohazardDamage", value: false);
		}
	}

	public void FinishKillAnimation(bool killedPlayer = false)
	{
		if (!killedPlayer)
		{
			creatureSFX.Stop();
		}
		if (killAnimationCoroutine != null)
		{
			StopCoroutine(killAnimationCoroutine);
		}
		inSpecialAnimation = false;
		inKillAnimation = false;
		creatureAnimator.SetBool("killing", value: false);
		startingKillAnimationLocalClient = false;
		if (inSpecialAnimationWithPlayer != null)
		{
			inSpecialAnimationWithPlayer.disableSyncInAnimation = false;
			inSpecialAnimationWithPlayer.disableLookInput = false;
			inSpecialAnimationWithPlayer.inSpecialInteractAnimation = false;
			inSpecialAnimationWithPlayer.snapToServerPosition = false;
			inSpecialAnimationWithPlayer.inAnimationWithEnemy = null;
		}
		else
		{
			Debug.Log("masked enemy inSpecialAnimationWithPlayer is null");
		}
		SetMaskGlow(enable: false);
		maskFloodParticle.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
		stopAndStareTimer = 3f;
		movingTowardsTargetPlayer = false;
		if (base.IsOwner)
		{
			base.transform.position = GetGroundPosition(base.transform.position);
			agent.enabled = true;
			isClientCalculatingAI = true;
		}
		if (base.NetworkObject.IsSpawned)
		{
			SwitchToBehaviourStateOnLocalClient(0);
			if (base.IsServer)
			{
				SwitchToBehaviourState(0);
			}
		}
	}

	private Vector3 GetGroundPosition(Vector3 startingPos)
	{
		Vector3 pos = startingPos;
		pos = RoundManager.Instance.GetNavMeshPosition(pos, default(NavMeshHit), 3f);
		if (!RoundManager.Instance.GotNavMeshPositionResult)
		{
			if (Physics.Raycast(startingPos + Vector3.up * 0.15f, -Vector3.up, out var hitInfo, 50f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
			{
				pos = RoundManager.Instance.GetNavMeshPosition(hitInfo.point, default(NavMeshHit), 10f);
			}
			else
			{
				int num = UnityEngine.Random.Range(0, allAINodes.Length);
				if (allAINodes != null && allAINodes[num] != null)
				{
					pos = allAINodes[num].transform.position;
				}
			}
		}
		return pos;
	}

	public void SetSuit(int suitId)
	{
		Material suitMaterial = StartOfRound.Instance.unlockablesList.unlockables[suitId].suitMaterial;
		rendererLOD0.material = suitMaterial;
		rendererLOD1.material = suitMaterial;
		rendererLOD2.material = suitMaterial;
	}

	public void SetMaskType(int maskType)
	{
		switch (maskType)
		{
		case 4:
			maskTypes[0].SetActive(value: true);
			maskTypes[1].SetActive(value: false);
			maskTypeIndex = 0;
			break;
		case 5:
			maskTypes[1].SetActive(value: true);
			maskTypes[0].SetActive(value: false);
			maskTypeIndex = 1;
			break;
		}
	}

	public void GetMaterialStandingOn()
	{
		enemyRay = new Ray(base.transform.position + Vector3.up, -Vector3.up);
		if (Physics.Raycast(enemyRay, out enemyRayHit, 6f, StartOfRound.Instance.walkableSurfacesMask, QueryTriggerInteraction.Ignore))
		{
			if (enemyRayHit.collider.CompareTag(StartOfRound.Instance.footstepSurfaces[currentFootstepSurfaceIndex].surfaceTag))
			{
				return;
			}
			for (int i = 0; i < StartOfRound.Instance.footstepSurfaces.Length; i++)
			{
				if (enemyRayHit.collider.CompareTag(StartOfRound.Instance.footstepSurfaces[i].surfaceTag))
				{
					currentFootstepSurfaceIndex = i;
					break;
				}
			}
		}
		else
		{
			Debug.DrawRay(enemyRay.origin, enemyRay.direction, Color.white, 0.3f);
		}
	}

	public void PlayFootstepSound()
	{
		GetMaterialStandingOn();
		int num = UnityEngine.Random.Range(0, StartOfRound.Instance.footstepSurfaces[currentFootstepSurfaceIndex].clips.Length);
		if (num == previousFootstepClip)
		{
			num = (num + 1) % StartOfRound.Instance.footstepSurfaces[currentFootstepSurfaceIndex].clips.Length;
		}
		movementAudio.pitch = UnityEngine.Random.Range(0.93f, 1.07f);
		float num2 = 0.95f;
		if (!sprinting)
		{
			num2 = 0.75f;
		}
		movementAudio.PlayOneShot(StartOfRound.Instance.footstepSurfaces[currentFootstepSurfaceIndex].clips[num], num2);
		previousFootstepClip = num;
		WalkieTalkie.TransmitOneShotAudio(movementAudio, StartOfRound.Instance.footstepSurfaces[currentFootstepSurfaceIndex].clips[num], num2);
	}

	public override void AnimationEventA()
	{
		base.AnimationEventA();
		PlayFootstepSound();
	}

	public void SetMaskGlow(bool enable)
	{
		maskEyesGlow[maskTypeIndex].enabled = enable;
		maskEyesGlowLight.enabled = enable;
		if (enable)
		{
			creatureSFX.PlayOneShot(enemyType.audioClips[1]);
		}
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	[RuntimeInitializeOnLoadMethod]
	internal static void InitializeRPCS_MaskedPlayerEnemy()
	{
		NetworkManager.__rpc_func_table.Add(3110137062u, __rpc_handler_3110137062);
		NetworkManager.__rpc_func_table.Add(1038760037u, __rpc_handler_1038760037);
		NetworkManager.__rpc_func_table.Add(657232826u, __rpc_handler_657232826);
		NetworkManager.__rpc_func_table.Add(2539470808u, __rpc_handler_2539470808);
		NetworkManager.__rpc_func_table.Add(2502006210u, __rpc_handler_2502006210);
		NetworkManager.__rpc_func_table.Add(3625708449u, __rpc_handler_3625708449);
		NetworkManager.__rpc_func_table.Add(675153417u, __rpc_handler_675153417);
		NetworkManager.__rpc_func_table.Add(432295350u, __rpc_handler_432295350);
		NetworkManager.__rpc_func_table.Add(1141953697u, __rpc_handler_1141953697);
		NetworkManager.__rpc_func_table.Add(2397761797u, __rpc_handler_2397761797);
		NetworkManager.__rpc_func_table.Add(1407409549u, __rpc_handler_1407409549);
		NetworkManager.__rpc_func_table.Add(1561581057u, __rpc_handler_1561581057);
		NetworkManager.__rpc_func_table.Add(519961256u, __rpc_handler_519961256);
		NetworkManager.__rpc_func_table.Add(222504553u, __rpc_handler_222504553);
		NetworkManager.__rpc_func_table.Add(2560207573u, __rpc_handler_2560207573);
		NetworkManager.__rpc_func_table.Add(1162325818u, __rpc_handler_1162325818);
		NetworkManager.__rpc_func_table.Add(3309468324u, __rpc_handler_3309468324);
		NetworkManager.__rpc_func_table.Add(3512011720u, __rpc_handler_3512011720);
		NetworkManager.__rpc_func_table.Add(3192502457u, __rpc_handler_3192502457);
		NetworkManager.__rpc_func_table.Add(4032958935u, __rpc_handler_4032958935);
		NetworkManager.__rpc_func_table.Add(3071650946u, __rpc_handler_3071650946);
		NetworkManager.__rpc_func_table.Add(1687215509u, __rpc_handler_1687215509);
	}

	private static void __rpc_handler_3110137062(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
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
			((MaskedPlayerEnemy)target).SetEnemyAsHavingNoPlayerServerRpc();
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	private static void __rpc_handler_1038760037(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Client;
			((MaskedPlayerEnemy)target).SetEnemyAsHavingNoPlayerClientRpc();
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	private static void __rpc_handler_657232826(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
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
			reader.ReadValueSafe(out Vector3 value);
			reader.ReadValueSafe(out bool value2, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Server;
			((MaskedPlayerEnemy)target).TeleportMaskedEnemyServerRpc(value, value2);
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	private static void __rpc_handler_2539470808(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out Vector3 value);
			reader.ReadValueSafe(out bool value2, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Client;
			((MaskedPlayerEnemy)target).TeleportMaskedEnemyClientRpc(value, value2);
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	private static void __rpc_handler_2502006210(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
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
			return;
		}
		reader.ReadValueSafe(out Vector3 value);
		reader.ReadValueSafe(out float value2, default(FastBufferWriter.ForPrimitives));
		reader.ReadValueSafe(out float value3, default(FastBufferWriter.ForPrimitives));
		target.__rpc_exec_stage = __RpcExecStage.Server;
		((MaskedPlayerEnemy)target).LookAtDirectionServerRpc(value, value2, value3);
		target.__rpc_exec_stage = __RpcExecStage.None;
	}

	private static void __rpc_handler_3625708449(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out Vector3 value);
			reader.ReadValueSafe(out float value2, default(FastBufferWriter.ForPrimitives));
			reader.ReadValueSafe(out float value3, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Client;
			((MaskedPlayerEnemy)target).LookAtDirectionClientRpc(value, value2, value3);
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	private static void __rpc_handler_675153417(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
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
			reader.ReadValueSafe(out Vector3 value);
			reader.ReadValueSafe(out float value2, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Server;
			((MaskedPlayerEnemy)target).LookAtPositionServerRpc(value, value2);
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	private static void __rpc_handler_432295350(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out Vector3 value);
			reader.ReadValueSafe(out float value2, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Client;
			((MaskedPlayerEnemy)target).LookAtPositionClientRpc(value, value2);
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	private static void __rpc_handler_1141953697(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
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
			((MaskedPlayerEnemy)target).LookAtPlayerServerRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	private static void __rpc_handler_2397761797(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			target.__rpc_exec_stage = __RpcExecStage.Client;
			((MaskedPlayerEnemy)target).LookAtPlayerClientRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	private static void __rpc_handler_1407409549(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
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
			((MaskedPlayerEnemy)target).StopLookingAtTransformServerRpc();
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	private static void __rpc_handler_1561581057(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Client;
			((MaskedPlayerEnemy)target).StopLookingAtTransformClientRpc();
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	private static void __rpc_handler_519961256(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
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
			reader.ReadValueSafe(out bool value, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Server;
			((MaskedPlayerEnemy)target).SetHandsOutServerRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	private static void __rpc_handler_222504553(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out bool value, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Client;
			((MaskedPlayerEnemy)target).SetHandsOutClientRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	private static void __rpc_handler_2560207573(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
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
			reader.ReadValueSafe(out bool value, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Server;
			((MaskedPlayerEnemy)target).SetCrouchingServerRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	private static void __rpc_handler_1162325818(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out bool value, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Client;
			((MaskedPlayerEnemy)target).SetCrouchingClientRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	private static void __rpc_handler_3309468324(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
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
			reader.ReadValueSafe(out bool value, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Server;
			((MaskedPlayerEnemy)target).SetRunningServerRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	private static void __rpc_handler_3512011720(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out bool value, default(FastBufferWriter.ForPrimitives));
			target.__rpc_exec_stage = __RpcExecStage.Client;
			((MaskedPlayerEnemy)target).SetRunningClientRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	private static void __rpc_handler_3192502457(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			target.__rpc_exec_stage = __RpcExecStage.Server;
			((MaskedPlayerEnemy)target).KillPlayerAnimationServerRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	private static void __rpc_handler_4032958935(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			target.__rpc_exec_stage = __RpcExecStage.Client;
			((MaskedPlayerEnemy)target).CancelKillAnimationClientRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	private static void __rpc_handler_3071650946(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			target.__rpc_exec_stage = __RpcExecStage.Client;
			((MaskedPlayerEnemy)target).KillPlayerAnimationClientRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	private static void __rpc_handler_1687215509(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out NetworkObjectReference value, default(FastBufferWriter.ForNetworkSerializable));
			reader.ReadValueSafe(out bool value2, default(FastBufferWriter.ForPrimitives));
			ByteUnpacker.ReadValueBitPacked(reader, out int value3);
			target.__rpc_exec_stage = __RpcExecStage.Client;
			((MaskedPlayerEnemy)target).CreateMimicClientRpc(value, value2, value3);
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	protected internal override string __getTypeName()
	{
		return "MaskedPlayerEnemy";
	}
}
