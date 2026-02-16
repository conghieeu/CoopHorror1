using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Dissonance;
using Steamworks;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UI;

namespace GameNetcodeStuff
{
	public class PlayerControllerB : NetworkBehaviour, IHittable, IShockableWithGun, IVisibleThreat
	{
		public bool isTestingPlayer;

		[Header("MODELS / ANIMATIONS")]
		public Transform[] bodyParts;

		public Transform thisPlayerBody;

		public SkinnedMeshRenderer thisPlayerModel;

		public SkinnedMeshRenderer thisPlayerModelLOD1;

		public SkinnedMeshRenderer thisPlayerModelLOD2;

		public SkinnedMeshRenderer thisPlayerModelArms;

		public Transform playerGlobalHead;

		public Transform playerModelArmsMetarig;

		public Transform localArmsRotationTarget;

		public Transform meshContainer;

		public Transform lowerSpine;

		public Camera gameplayCamera;

		public Transform cameraContainerTransform;

		public Transform playerEye;

		public float targetFOV = 66f;

		public Camera visorCamera;

		public CharacterController thisController;

		public Animator playerBodyAnimator;

		public MeshFilter playerBadgeMesh;

		public MeshRenderer playerBetaBadgeMesh;

		public int playerLevelNumber;

		public Transform localVisor;

		public Transform localVisorTargetPoint;

		private bool isSidling;

		private bool wasMovingForward;

		public MultiRotationConstraint cameraLookRig1;

		public MultiRotationConstraint cameraLookRig2;

		public Transform playerHudUIContainer;

		public Transform playerHudBaseRotation;

		public ChainIKConstraint rightArmNormalRig;

		public ChainIKConstraint rightArmProceduralRig;

		public Transform rightArmProceduralTarget;

		private Vector3 rightArmProceduralTargetBasePosition;

		public Transform leftHandItemTarget;

		public Light nightVision;

		public int currentSuitID;

		public bool performingEmote;

		public float emoteLayerWeight;

		public float timeSinceStartingEmote;

		public ParticleSystem beamUpParticle;

		public ParticleSystem beamOutParticle;

		public ParticleSystem beamOutBuildupParticle;

		public bool localArmsMatchCamera;

		public Transform localArmsTransform;

		public Collider playerCollider;

		public Collider[] bodyPartSpraypaintColliders;

		[Header("AUDIOS")]
		public AudioSource movementAudio;

		public AudioSource itemAudio;

		public AudioSource statusEffectAudio;

		public AudioSource waterBubblesAudio;

		public int currentFootstepSurfaceIndex;

		private int previousFootstepClip;

		[HideInInspector]
		public Dictionary<AudioSource, AudioReverbTrigger> audioCoroutines = new Dictionary<AudioSource, AudioReverbTrigger>();

		[HideInInspector]
		public Dictionary<AudioSource, IEnumerator> audioCoroutines2 = new Dictionary<AudioSource, IEnumerator>();

		[HideInInspector]
		public AudioReverbTrigger currentAudioTrigger;

		public AudioReverbTrigger currentAudioTriggerB;

		public float targetDryLevel;

		public float targetRoom;

		public float targetHighFreq;

		public float targetLowFreq;

		public float targetDecayTime;

		public ReverbPreset reverbPreset;

		public AudioListener activeAudioListener;

		public AudioReverbFilter activeAudioReverbFilter;

		public ParticleSystem bloodParticle;

		public bool playingQuickSpecialAnimation;

		private Coroutine quickSpecialAnimationCoroutine;

		[Header("INPUT / MOVEMENT")]
		public float movementSpeed = 0.5f;

		public PlayerActions playerActions;

		private bool isWalking;

		private bool movingForward;

		public Vector2 moveInputVector;

		public Vector3 velocityLastFrame;

		private float sprintMultiplier = 1f;

		public bool isSprinting;

		public float sprintTime = 5f;

		public Image sprintMeterUI;

		[HideInInspector]
		public float sprintMeter;

		[HideInInspector]
		public bool isExhausted;

		private float exhaustionEffectLerp;

		public float jumpForce = 5f;

		private bool isJumping;

		private bool isFallingFromJump;

		private Coroutine jumpCoroutine;

		public float fallValue;

		public bool isGroundedOnServer;

		public bool isPlayerSliding;

		private float playerSlidingTimer;

		public Vector3 playerGroundNormal;

		public float maxSlideFriction;

		private float slideFriction;

		public float fallValueUncapped;

		public bool takingFallDamage;

		public float minVelocityToTakeDamage;

		public bool isCrouching;

		private float timeSinceCrouching;

		private bool isFallingNoJump;

		public int isMovementHindered;

		private int movementHinderedPrev;

		public float hinderedMultiplier = 1f;

		public int sourcesCausingSinking;

		public bool isSinking;

		public bool isUnderwater;

		private float syncUnderwaterInterval;

		private bool isFaceUnderwaterOnServer;

		public Collider underwaterCollider;

		private bool wasUnderwaterLastFrame;

		public float sinkingValue;

		public float sinkingSpeedMultiplier;

		public int statusEffectAudioIndex;

		private float cameraUp;

		public float lookSensitivity = 0.4f;

		public bool disableLookInput;

		private float oldLookRot;

		private float targetLookRot;

		private float previousYRot;

		private float targetYRot;

		public Vector3 syncFullRotation;

		private Vector3 walkForce;

		public Vector3 externalForces;

		private Vector3 movementForcesLastFrame;

		public Rigidbody playerRigidbody;

		public float averageVelocity;

		public int velocityMovingAverageLength = 20;

		public int velocityAverageCount;

		public float getAverageVelocityInterval;

		public bool jetpackControls;

		public bool disablingJetpackControls;

		public Transform jetpackTurnCompass;

		private bool startedJetpackControls;

		private float previousFrameDeltaTime;

		private Collider[] nearByPlayers = new Collider[4];

		private bool teleportingThisFrame;

		public bool teleportedLastFrame;

		[Header("LOCATION")]
		public bool isInElevator;

		public bool isInHangarShipRoom;

		public bool isInsideFactory;

		[Space(5f)]
		public bool wasInElevatorLastFrame;

		public Vector3 previousElevatorPosition;

		[Header("CONTROL / NETWORKING")]
		public ulong playerClientId;

		public string playerUsername = "Player";

		public ulong playerSteamId;

		public ulong actualClientId;

		public bool isPlayerControlled;

		public bool justConnected = true;

		public bool disconnectedMidGame;

		[Space(5f)]
		private bool isCameraDisabled;

		public StartOfRound playersManager;

		public bool isHostPlayerObject;

		public Vector3 oldPlayerPosition;

		private int previousAnimationState;

		public Vector3 serverPlayerPosition;

		public bool snapToServerPosition;

		private float oldCameraUp;

		public float ladderCameraHorizontal;

		private float updatePlayerAnimationsInterval;

		private float updatePlayerLookInterval;

		private List<int> currentAnimationStateHash = new List<int>();

		private List<int> previousAnimationStateHash = new List<int>();

		private float currentAnimationSpeed;

		private float previousAnimationSpeed;

		private int previousAnimationServer;

		private int oldConnectedPlayersAmount;

		private int playerMask = 8;

		public RawImage playerScreen;

		public VoicePlayerState voicePlayerState;

		public AudioSource currentVoiceChatAudioSource;

		public PlayerVoiceIngameSettings currentVoiceChatIngameSettings;

		private float voiceChatUpdateInterval;

		public bool isTypingChat;

		[Header("DEATH")]
		public int health;

		public float healthRegenerateTimer;

		public bool criticallyInjured;

		public bool hasBeenCriticallyInjured;

		private float limpMultiplier = 0.2f;

		public CauseOfDeath causeOfDeath;

		public bool isPlayerDead;

		[HideInInspector]
		public bool setPositionOfDeadPlayer;

		[HideInInspector]
		public Vector3 placeOfDeath;

		public Transform spectateCameraPivot;

		public PlayerControllerB spectatedPlayerScript;

		public DeadBodyInfo deadBody;

		public GameObject[] bodyBloodDecals;

		private int currentBloodIndex;

		public List<GameObject> playerBloodPooledObjects = new List<GameObject>();

		public bool bleedingHeavily;

		private float bloodDropTimer;

		private bool alternatePlaceFootprints;

		public EnemyAI inAnimationWithEnemy;

		[Header("UI/MENU")]
		public bool inTerminalMenu;

		public QuickMenuManager quickMenuManager;

		public TextMeshProUGUI usernameBillboardText;

		public Transform usernameBillboard;

		public CanvasGroup usernameAlpha;

		public Canvas usernameCanvas;

		private Vector3 tempVelocity;

		[Header("ITEM INTERACTION")]
		public float grabDistance = 5f;

		public float throwPower = 17f;

		public bool isHoldingObject;

		private bool hasThrownObject;

		public bool twoHanded;

		public bool twoHandedAnimation;

		public float carryWeight = 1f;

		public bool isGrabbingObjectAnimation;

		public bool activatingItem;

		public float grabObjectAnimationTime;

		public Transform localItemHolder;

		public Transform serverItemHolder;

		public Transform propThrowPosition;

		public GrabbableObject currentlyHeldObject;

		private GrabbableObject currentlyGrabbingObject;

		public GrabbableObject currentlyHeldObjectServer;

		public GameObject heldObjectServerCopy;

		private Coroutine grabObjectCoroutine;

		private Ray interactRay;

		private int grabbableObjectsMask = 64;

		private int interactableObjectsMask = 832;

		private int walkableSurfacesNoPlayersMask = 268437761;

		private RaycastHit hit;

		private float upperBodyAnimationsWeight;

		public float doingUpperBodyEmote;

		private float handsOnWallWeight;

		public Light helmetLight;

		public Light[] allHelmetLights;

		private bool grabbedObjectValidated;

		private bool grabInvalidated;

		private bool throwingObject;

		[Space(5f)]
		public GrabbableObject[] ItemSlots;

		public int currentItemSlot;

		private MeshRenderer[] itemRenderers;

		private float timeSinceSwitchingSlots;

		[HideInInspector]
		public bool grabSetParentServer;

		[Header("TRIGGERS AND SPECIAL")]
		public Image cursorIcon;

		public TextMeshProUGUI cursorTip;

		public Sprite grabItemIcon;

		private bool hoveringOverItem;

		public InteractTrigger hoveringOverTrigger;

		public InteractTrigger previousHoveringOverTrigger;

		public InteractTrigger currentTriggerInAnimationWith;

		public bool isHoldingInteract;

		public bool inSpecialInteractAnimation;

		public bool disableSyncInAnimation;

		public float specialAnimationWeight;

		public bool isClimbingLadder;

		public bool enteringSpecialAnimation;

		public float climbSpeed = 4f;

		public Vector3 clampCameraRotation;

		public Transform lineOfSightCube;

		public bool voiceMuffledByEnemy;

		[Header("SPECIAL ITEMS")]
		public int shipTeleporterId;

		public EnemyAI redirectToEnemy;

		public MeshRenderer mapRadarDirectionIndicator;

		public Animator mapRadarDotAnimator;

		public bool equippedUsableItemQE;

		public bool IsInspectingItem;

		public bool isFirstFrameLateUpdate = true;

		public GrabbableObject pocketedFlashlight;

		public bool isFreeCamera;

		public bool isSpeedCheating;

		public bool inShockingMinigame;

		public Transform shockingTarget;

		public Transform turnCompass;

		public Transform smoothLookTurnCompass;

		public float smoothLookMultiplier = 25f;

		private bool smoothLookEnabledLastFrame;

		public Camera turnCompassCamera;

		[HideInInspector]
		public Vector3 targetScreenPos;

		[HideInInspector]
		public float shockMinigamePullPosition;

		[Space(5f)]
		public bool speakingToWalkieTalkie;

		public bool holdingWalkieTalkie;

		public float isInGameOverAnimation;

		[HideInInspector]
		public bool hasBegunSpectating;

		private Coroutine timeSpecialAnimationCoroutine;

		private float spectatedPlayerDeadTimer;

		public float insanityLevel;

		public float maxInsanityLevel = 50f;

		public float insanitySpeedMultiplier = 1f;

		public bool isPlayerAlone;

		public float timeSincePlayerMoving;

		public Scrollbar terminalScrollVertical;

		private bool updatePositionForNewlyJoinedClient;

		private float timeSinceTakingGravityDamage;

		[Space(5f)]
		public float drunkness;

		public float drunknessInertia = 1f;

		public float drunknessSpeed;

		public bool increasingDrunknessThisFrame;

		public float timeSinceMakingLoudNoise;

		ThreatType IVisibleThreat.type => ThreatType.Player;

		float IVisibleThreat.GetVisibility()
		{
			if (isPlayerDead)
			{
				return 0f;
			}
			float num = 1f;
			if (isCrouching)
			{
				num -= 0.25f;
			}
			if (timeSincePlayerMoving > 0.5f)
			{
				num -= 0.16f;
			}
			return num;
		}

		int IVisibleThreat.GetThreatLevel(Vector3 seenByPosition)
		{
			int num = 0;
			if (isHoldingObject && currentlyHeldObjectServer != null && currentlyHeldObjectServer.itemProperties.isDefensiveWeapon)
			{
				num += 2;
			}
			if (timeSinceMakingLoudNoise < 0.8f)
			{
				num++;
			}
			float num2 = LineOfSightToPositionAngle(seenByPosition);
			if (num2 == -361f || num2 > 100f)
			{
				num--;
			}
			else if (num2 < 45f)
			{
				num++;
			}
			if (TimeOfDay.Instance.normalizedTimeOfDay < 0.2f)
			{
				num++;
			}
			else if (TimeOfDay.Instance.normalizedTimeOfDay > 0.8f)
			{
				num--;
			}
			if (isInHangarShipRoom)
			{
				num++;
			}
			else if (Vector3.Distance(base.transform.position, StartOfRound.Instance.elevatorTransform.position) > 30f)
			{
				num--;
			}
			int num3 = Physics.OverlapSphereNonAlloc(base.transform.position, 12f, nearByPlayers, StartOfRound.Instance.playersMask);
			for (int i = 0; i < num3; i++)
			{
				if (!(Vector3.Distance(base.transform.position, nearByPlayers[i].transform.position) > 6f) || !Physics.Linecast(gameplayCamera.transform.position, nearByPlayers[i].transform.position + Vector3.up * 0.6f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
				{
					num++;
				}
			}
			if (health >= 30)
			{
				num++;
			}
			else if (criticallyInjured)
			{
				num -= 2;
			}
			if (StartOfRound.Instance.connectedPlayersAmount <= 0)
			{
				num++;
			}
			return num;
		}

		Vector3 IVisibleThreat.GetThreatVelocity()
		{
			if (base.IsOwner)
			{
				return Vector3.Normalize(thisController.velocity * 100f);
			}
			if (timeSincePlayerMoving < 0.25f)
			{
				return Vector3.Normalize((serverPlayerPosition - oldPlayerPosition) * 100f);
			}
			return Vector3.zero;
		}

		int IVisibleThreat.GetInterestLevel()
		{
			int num = 0;
			if (currentlyHeldObjectServer != null && currentlyHeldObjectServer.itemProperties.isScrap)
			{
				num++;
			}
			if (carryWeight > 1.22f)
			{
				num++;
			}
			if (carryWeight > 1.5f)
			{
				num++;
			}
			return num;
		}

		Transform IVisibleThreat.GetThreatLookTransform()
		{
			return gameplayCamera.transform;
		}

		Transform IVisibleThreat.GetThreatTransform()
		{
			return base.transform;
		}

		private void Awake()
		{
			isHostPlayerObject = base.gameObject == playersManager.allPlayerObjects[0];
			playerActions = new PlayerActions();
			previousAnimationState = 0;
			serverPlayerPosition = base.transform.position;
			gameplayCamera.enabled = false;
			visorCamera.enabled = false;
			thisPlayerModel.enabled = true;
			thisPlayerModel.shadowCastingMode = ShadowCastingMode.On;
			thisPlayerModelArms.enabled = false;
			gameplayCamera.enabled = false;
			previousAnimationStateHash = new List<int>(new int[playerBodyAnimator.layerCount]);
			currentAnimationStateHash = new List<int>(new int[playerBodyAnimator.layerCount]);
			if (playerBodyAnimator.runtimeAnimatorController != playersManager.otherClientsAnimatorController)
			{
				playerBodyAnimator.runtimeAnimatorController = playersManager.otherClientsAnimatorController;
			}
			isCameraDisabled = true;
			sprintMeter = 1f;
			ItemSlots = new GrabbableObject[4];
			rightArmProceduralTargetBasePosition = rightArmProceduralTarget.localPosition;
			playerUsername = $"Player #{playerClientId}";
			previousElevatorPosition = playersManager.elevatorTransform.position;
			if ((bool)base.gameObject.GetComponent<Rigidbody>())
			{
				base.gameObject.GetComponent<Rigidbody>().interpolation = RigidbodyInterpolation.None;
			}
			base.gameObject.GetComponent<CharacterController>().enabled = false;
			syncFullRotation = base.transform.eulerAngles;
		}

		private void Start()
		{
			InstantiateBloodPooledObjects();
			StartCoroutine(PlayIntroTip());
			jetpackTurnCompass.SetParent(null);
			terminalScrollVertical.value += 500f;
		}

		private IEnumerator PlayIntroTip()
		{
			yield return new WaitForSeconds(4f);
			QuickMenuManager quickMenu = UnityEngine.Object.FindObjectOfType<QuickMenuManager>();
			yield return new WaitUntil(() => !quickMenu.isMenuOpen);
			HUDManager.Instance.DisplayTip("Welcome!", "Right-click to scan objects in the ship for info.", isWarning: false, useSave: true, "LC_IntroTip1");
		}

		private void InstantiateBloodPooledObjects()
		{
			int num = 50;
			for (int i = 0; i < num; i++)
			{
				GameObject gameObject = UnityEngine.Object.Instantiate(playersManager.playerBloodPrefab, playersManager.bloodObjectsContainer);
				gameObject.SetActive(value: false);
				playerBloodPooledObjects.Add(gameObject);
			}
		}

		public void ResetPlayerBloodObjects(bool resetBodyBlood = true)
		{
			if (playerBloodPooledObjects != null)
			{
				for (int i = 0; i < playerBloodPooledObjects.Count; i++)
				{
					playerBloodPooledObjects[i].SetActive(value: false);
				}
			}
			if (resetBodyBlood)
			{
				for (int j = 0; j < bodyBloodDecals.Length; j++)
				{
					bodyBloodDecals[j].SetActive(value: false);
				}
			}
		}

		private void OnEnable()
		{
			InputActionAsset actions = IngamePlayerSettings.Instance.playerInput.actions;
			try
			{
				playerActions.Movement.Look.performed += Look_performed;
				actions.FindAction("Jump").performed += Jump_performed;
				actions.FindAction("Crouch").performed += Crouch_performed;
				actions.FindAction("Interact").performed += Interact_performed;
				actions.FindAction("ItemSecondaryUse").performed += ItemSecondaryUse_performed;
				actions.FindAction("ItemTertiaryUse").performed += ItemTertiaryUse_performed;
				actions.FindAction("ActivateItem").performed += ActivateItem_performed;
				actions.FindAction("ActivateItem").canceled += ActivateItem_canceled;
				actions.FindAction("Discard").performed += Discard_performed;
				actions.FindAction("SwitchItem").performed += ScrollMouse_performed;
				actions.FindAction("OpenMenu").performed += OpenMenu_performed;
				actions.FindAction("InspectItem").performed += InspectItem_performed;
				actions.FindAction("SpeedCheat").performed += SpeedCheat_performed;
				actions.FindAction("Emote1").performed += Emote1_performed;
				actions.FindAction("Emote2").performed += Emote2_performed;
				playerActions.Movement.Enable();
			}
			catch (Exception arg)
			{
				Debug.LogError($"Error while subscribing to input in PlayerController!: {arg}");
			}
			playerActions.Movement.Enable();
		}

		private void OnDisable()
		{
			InputActionAsset actions = IngamePlayerSettings.Instance.playerInput.actions;
			try
			{
				playerActions.Movement.Look.performed -= Look_performed;
				actions.FindAction("Jump").performed -= Jump_performed;
				actions.FindAction("Crouch").performed -= Crouch_performed;
				actions.FindAction("Interact").performed -= Interact_performed;
				actions.FindAction("ItemSecondaryUse").performed -= ItemSecondaryUse_performed;
				actions.FindAction("ItemTertiaryUse").performed -= ItemTertiaryUse_performed;
				actions.FindAction("ActivateItem").performed -= ActivateItem_performed;
				actions.FindAction("ActivateItem").canceled -= ActivateItem_canceled;
				actions.FindAction("Discard").performed -= Discard_performed;
				actions.FindAction("SwitchItem").performed -= ScrollMouse_performed;
				actions.FindAction("OpenMenu").performed -= OpenMenu_performed;
				actions.FindAction("InspectItem").performed -= InspectItem_performed;
				actions.FindAction("SpeedCheat").performed -= SpeedCheat_performed;
				actions.FindAction("Emote1").performed -= Emote1_performed;
				actions.FindAction("Emote2").performed -= Emote2_performed;
				playerActions.Movement.Enable();
			}
			catch (Exception arg)
			{
				Debug.LogError($"Error while unsubscribing from input in PlayerController!: {arg}");
			}
			playerActions.Movement.Disable();
		}

		public override void OnDestroy()
		{
			base.OnDestroy();
		}

		private void SpeedCheat_performed(InputAction.CallbackContext context)
		{
			if (((base.IsOwner && (isPlayerControlled || isPlayerDead) && !inTerminalMenu && !isTypingChat && (!base.IsServer || isHostPlayerObject)) || isTestingPlayer) && context.performed && !(HUDManager.Instance == null) && IngamePlayerSettings.Instance.playerInput.actions.FindAction("Sprint").ReadValue<float>() > 0.5f)
			{
				HUDManager.Instance.ToggleErrorConsole();
			}
		}

		public bool AllowPlayerDeath()
		{
			if (!StartOfRound.Instance.allowLocalPlayerDeath)
			{
				return false;
			}
			if (playersManager.testRoom == null)
			{
				if (StartOfRound.Instance.timeSinceRoundStarted < 2f)
				{
					return false;
				}
				if (!playersManager.shipDoorsEnabled)
				{
					return false;
				}
			}
			return true;
		}

		public void DamagePlayer(int damageNumber, bool hasDamageSFX = true, bool callRPC = true, CauseOfDeath causeOfDeath = CauseOfDeath.Unknown, int deathAnimation = 0, bool fallDamage = false, Vector3 force = default(Vector3))
		{
			if (!base.IsOwner || isPlayerDead || !AllowPlayerDeath())
			{
				return;
			}
			if (health - damageNumber <= 0 && !criticallyInjured && damageNumber < 50)
			{
				health = 5;
			}
			else
			{
				health = Mathf.Clamp(health - damageNumber, 0, 100);
			}
			Debug.Log($"player's health after taking {damageNumber} damage: {health}");
			HUDManager.Instance.UpdateHealthUI(health);
			if (health <= 0)
			{
				KillPlayer(force, spawnBody: true, causeOfDeath, deathAnimation);
			}
			else
			{
				if (health < 10 && !criticallyInjured)
				{
					HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
					MakeCriticallyInjured(enable: true);
				}
				else
				{
					if (damageNumber > 30)
					{
						sprintMeter = Mathf.Clamp(sprintMeter + (float)damageNumber / 125f, 0f, 1f);
					}
					if (callRPC)
					{
						if (base.IsServer)
						{
							DamagePlayerClientRpc(damageNumber, health);
						}
						else
						{
							DamagePlayerServerRpc(damageNumber, health);
						}
					}
				}
				if (fallDamage)
				{
					HUDManager.Instance.UIAudio.PlayOneShot(StartOfRound.Instance.fallDamageSFX, 1f);
					WalkieTalkie.TransmitOneShotAudio(movementAudio, StartOfRound.Instance.fallDamageSFX);
					BreakLegsSFXClientRpc();
				}
				else if (hasDamageSFX)
				{
					HUDManager.Instance.UIAudio.PlayOneShot(StartOfRound.Instance.damageSFX, 1f);
				}
			}
			takingFallDamage = false;
			if (!inSpecialInteractAnimation)
			{
				playerBodyAnimator.SetTrigger("Damage");
			}
			specialAnimationWeight = 1f;
			PlayQuickSpecialAnimation(0.7f);
		}

		[ServerRpc]
		public void BreakLegsSFXServerRpc()
		{
			BreakLegsSFXClientRpc();
		}

		[ClientRpc]
		public void BreakLegsSFXClientRpc()
		{
			if (!base.IsOwner)
			{
				movementAudio.PlayOneShot(StartOfRound.Instance.fallDamageSFX, 1f);
				WalkieTalkie.TransmitOneShotAudio(movementAudio, StartOfRound.Instance.fallDamageSFX);
			}
		}

		public void MakeCriticallyInjured(bool enable)
		{
			if (enable)
			{
				criticallyInjured = true;
				playerBodyAnimator.SetBool("Limp", value: true);
				bleedingHeavily = true;
				if (base.IsServer)
				{
					MakeCriticallyInjuredClientRpc();
				}
				else
				{
					MakeCriticallyInjuredServerRpc();
				}
			}
			else
			{
				criticallyInjured = false;
				playerBodyAnimator.SetBool("Limp", value: false);
				bleedingHeavily = false;
				if (base.IsServer)
				{
					HealClientRpc();
				}
				else
				{
					HealServerRpc();
				}
			}
		}

		[ServerRpc]
		public void DamagePlayerServerRpc(int damageNumber, int newHealthAmount)
		{
			DamagePlayerClientRpc(damageNumber, newHealthAmount);
		}

		[ClientRpc]
		public void DamagePlayerClientRpc(int damageNumber, int newHealthAmount)
		{
			DamageOnOtherClients(damageNumber, newHealthAmount);
		}

		private void DamageOnOtherClients(int damageNumber, int newHealthAmount)
		{
			playersManager.gameStats.allPlayerStats[playerClientId].damageTaken += damageNumber;
			health = newHealthAmount;
			if (!base.IsOwner)
			{
				PlayQuickSpecialAnimation(0.7f);
			}
		}

		public void PlayQuickSpecialAnimation(float animTime)
		{
			if (quickSpecialAnimationCoroutine != null)
			{
				StopCoroutine(quickSpecialAnimationCoroutine);
			}
			quickSpecialAnimationCoroutine = StartCoroutine(playQuickSpecialAnimation(animTime));
		}

		private IEnumerator playQuickSpecialAnimation(float animTime)
		{
			playingQuickSpecialAnimation = true;
			yield return new WaitForSeconds(animTime);
			playingQuickSpecialAnimation = false;
		}

		[ServerRpc]
		public void StartSinkingServerRpc(float sinkingSpeed, int audioClipIndex)
		{
			StartSinkingClientRpc(sinkingSpeed, audioClipIndex);
		}

		[ClientRpc]
		public void StartSinkingClientRpc(float sinkingSpeed, int audioClipIndex)
		{
			sinkingSpeedMultiplier = sinkingSpeed;
			isSinking = true;
			statusEffectAudio.clip = StartOfRound.Instance.statusEffectClips[audioClipIndex];
			statusEffectAudio.Play();
		}

		[ServerRpc]
		public void StopSinkingServerRpc()
		{
			StopSinkingClientRpc();
		}

		[ClientRpc]
		public void StopSinkingClientRpc()
		{
			MakeCriticallyInjuredClientRpc();
		}

		[ClientRpc]
		public void MakeCriticallyInjuredClientRpc()
		{
			bleedingHeavily = true;
			criticallyInjured = true;
			hasBeenCriticallyInjured = true;
		}

		[ServerRpc]
		public void HealServerRpc()
		{
			HealClientRpc();
		}

		[ClientRpc]
		public void HealClientRpc()
		{
			if (!base.IsOwner)
			{
				bleedingHeavily = false;
				criticallyInjured = false;
			}
		}

		public void DropBlood(Vector3 direction = default(Vector3), bool leaveBlood = true, bool leaveFootprint = false)
		{
			bool flag = false;
			if (leaveBlood)
			{
				if (bloodDropTimer >= 0f && !isPlayerDead)
				{
					return;
				}
				bloodDropTimer = 0.4f;
				if (direction == Vector3.zero)
				{
					direction = Vector3.down;
				}
				Transform transform = playerBloodPooledObjects[currentBloodIndex].transform;
				transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
				if (isInElevator)
				{
					transform.SetParent(playersManager.elevatorTransform);
				}
				else
				{
					transform.SetParent(playersManager.bloodObjectsContainer);
				}
				if (isPlayerDead)
				{
					if (deadBody == null || !deadBody.gameObject.activeSelf)
					{
						return;
					}
					interactRay = new Ray(deadBody.bodyParts[3].transform.position + Vector3.up * 0.5f, direction);
				}
				else
				{
					interactRay = new Ray(base.transform.position + base.transform.up * 2f, direction);
				}
				if (Physics.Raycast(interactRay, out hit, 6f, playersManager.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
				{
					flag = true;
					transform.position = hit.point - direction.normalized * 0.45f;
					RandomizeBloodRotationAndScale(transform);
					transform.gameObject.SetActive(value: true);
				}
				currentBloodIndex = (currentBloodIndex + 1) % playerBloodPooledObjects.Count;
			}
			if (!leaveFootprint || isPlayerDead || playersManager.snowFootprintsPooledObjects == null || playersManager.snowFootprintsPooledObjects.Count <= 0)
			{
				return;
			}
			alternatePlaceFootprints = !alternatePlaceFootprints;
			if (alternatePlaceFootprints)
			{
				return;
			}
			Transform transform2 = playersManager.snowFootprintsPooledObjects[playersManager.currentFootprintIndex].transform;
			transform2.rotation = Quaternion.LookRotation(direction, Vector3.up);
			if (!flag)
			{
				interactRay = new Ray(base.transform.position + base.transform.up * 0.3f, direction);
				if (Physics.Raycast(interactRay, out hit, 6f, playersManager.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
				{
					transform2.position = hit.point - direction.normalized * 0.45f;
				}
			}
			else
			{
				transform2.position = hit.point - direction.normalized * 0.45f;
			}
			transform2.transform.eulerAngles = new Vector3(transform2.transform.eulerAngles.x, base.transform.eulerAngles.y, transform2.transform.eulerAngles.z);
			playersManager.snowFootprintsPooledObjects[playersManager.currentFootprintIndex].enabled = true;
			playersManager.currentFootprintIndex = (playersManager.currentFootprintIndex + 1) % playersManager.snowFootprintsPooledObjects.Count;
		}

		private void RandomizeBloodRotationAndScale(Transform blood)
		{
			Vector3 localEulerAngles = blood.localEulerAngles;
			localEulerAngles.z = UnityEngine.Random.Range(-180f, 180f);
			blood.localEulerAngles = localEulerAngles;
			blood.localScale = new Vector3(UnityEngine.Random.Range(0.15f, 0.7f), UnityEngine.Random.Range(0.15f, 0.7f), 0.55f);
		}

		private void Emote1_performed(InputAction.CallbackContext context)
		{
			PerformEmote(context, 1);
		}

		private void Emote2_performed(InputAction.CallbackContext context)
		{
			PerformEmote(context, 2);
		}

		public void PerformEmote(InputAction.CallbackContext context, int emoteID)
		{
			if (context.performed && ((base.IsOwner && isPlayerControlled && (!base.IsServer || isHostPlayerObject)) || isTestingPlayer) && CheckConditionsForEmote() && !(timeSinceStartingEmote < 0.5f))
			{
				timeSinceStartingEmote = 0f;
				performingEmote = true;
				playerBodyAnimator.SetInteger("emoteNumber", emoteID);
				StartPerformingEmoteServerRpc();
			}
		}

		[ServerRpc]
		public void StartPerformingEmoteServerRpc()
		{
			StartPerformingEmoteClientRpc();
		}

		[ClientRpc]
		public void StartPerformingEmoteClientRpc()
		{
			performingEmote = true;
		}

		[ServerRpc]
		public void StopPerformingEmoteServerRpc()
		{
			StopPerformingEmoteClientRpc();
		}

		[ClientRpc]
		public void StopPerformingEmoteClientRpc()
		{
			performingEmote = false;
		}

		public bool CheckConditionsForSinkingInQuicksand()
		{
			if (!thisController.isGrounded)
			{
				return false;
			}
			if (inSpecialInteractAnimation || (bool)inAnimationWithEnemy || isClimbingLadder)
			{
				return false;
			}
			if (currentFootstepSurfaceIndex != 1 && currentFootstepSurfaceIndex != 4 && currentFootstepSurfaceIndex != 8)
			{
				return false;
			}
			return true;
		}

		private bool CheckConditionsForEmote()
		{
			if (inSpecialInteractAnimation)
			{
				return false;
			}
			if (isPlayerDead || isJumping || isWalking)
			{
				return false;
			}
			if (isCrouching || isClimbingLadder)
			{
				return false;
			}
			if (isGrabbingObjectAnimation || inTerminalMenu || isTypingChat)
			{
				return false;
			}
			return true;
		}

		private void ActivateItem_performed(InputAction.CallbackContext context)
		{
			if (!context.performed)
			{
				return;
			}
			if (base.IsOwner && isPlayerDead && (!base.IsServer || isHostPlayerObject))
			{
				if (!StartOfRound.Instance.overrideSpectateCamera && spectatedPlayerScript != null && !spectatedPlayerScript.isPlayerDead)
				{
					SpectateNextPlayer();
				}
			}
			else if (CanUseItem() && !(timeSinceSwitchingSlots < 0.075f))
			{
				ShipBuildModeManager.Instance.CancelBuildMode();
				currentlyHeldObjectServer.gameObject.GetComponent<GrabbableObject>().UseItemOnClient();
				timeSinceSwitchingSlots = 0f;
			}
		}

		private void ActivateItem_canceled(InputAction.CallbackContext context)
		{
			if (CanUseItem() && currentlyHeldObjectServer.itemProperties.holdButtonUse)
			{
				ShipBuildModeManager.Instance.CancelBuildMode();
				currentlyHeldObjectServer.gameObject.GetComponent<GrabbableObject>().UseItemOnClient(buttonDown: false);
			}
		}

		private bool CanUseItem()
		{
			if ((!base.IsOwner || !isPlayerControlled || (base.IsServer && !isHostPlayerObject)) && !isTestingPlayer)
			{
				return false;
			}
			if (!isHoldingObject || currentlyHeldObjectServer == null)
			{
				return false;
			}
			if (quickMenuManager.isMenuOpen)
			{
				return false;
			}
			if (isPlayerDead)
			{
				return false;
			}
			if (!currentlyHeldObjectServer.itemProperties.usableInSpecialAnimations && (isGrabbingObjectAnimation || inTerminalMenu || isTypingChat || (inSpecialInteractAnimation && !inShockingMinigame)))
			{
				return false;
			}
			return true;
		}

		private int FirstEmptyItemSlot()
		{
			int result = -1;
			if (ItemSlots[currentItemSlot] == null)
			{
				result = currentItemSlot;
			}
			else
			{
				for (int i = 0; i < ItemSlots.Length; i++)
				{
					if (ItemSlots[i] == null)
					{
						result = i;
						break;
					}
				}
			}
			return result;
		}

		private int NextItemSlot(bool forward)
		{
			if (forward)
			{
				return (currentItemSlot + 1) % ItemSlots.Length;
			}
			if (currentItemSlot == 0)
			{
				return ItemSlots.Length - 1;
			}
			return currentItemSlot - 1;
		}

		private void SwitchToItemSlot(int slot, GrabbableObject fillSlotWithItem = null)
		{
			currentItemSlot = slot;
			if (base.IsOwner)
			{
				for (int i = 0; i < HUDManager.Instance.itemSlotIconFrames.Length; i++)
				{
					HUDManager.Instance.itemSlotIconFrames[i].GetComponent<Animator>().SetBool("selectedSlot", value: false);
				}
				HUDManager.Instance.itemSlotIconFrames[slot].GetComponent<Animator>().SetBool("selectedSlot", value: true);
			}
			if (fillSlotWithItem != null)
			{
				ItemSlots[slot] = fillSlotWithItem;
				if (base.IsOwner)
				{
					HUDManager.Instance.itemSlotIcons[slot].sprite = fillSlotWithItem.itemProperties.itemIcon;
					HUDManager.Instance.itemSlotIcons[currentItemSlot].enabled = true;
				}
			}
			if (currentlyHeldObjectServer != null)
			{
				currentlyHeldObjectServer.playerHeldBy = this;
				if (base.IsOwner)
				{
					SetSpecialGrabAnimationBool(setTrue: false, currentlyHeldObjectServer);
				}
				currentlyHeldObjectServer.PocketItem();
				if (ItemSlots[slot] != null && !string.IsNullOrEmpty(ItemSlots[slot].itemProperties.pocketAnim))
				{
					playerBodyAnimator.SetTrigger(ItemSlots[slot].itemProperties.pocketAnim);
				}
			}
			if (ItemSlots[slot] != null)
			{
				ItemSlots[slot].playerHeldBy = this;
				ItemSlots[slot].EquipItem();
				if (base.IsOwner)
				{
					SetSpecialGrabAnimationBool(setTrue: true, ItemSlots[slot]);
				}
				if (currentlyHeldObjectServer != null)
				{
					if (ItemSlots[slot].itemProperties.twoHandedAnimation || currentlyHeldObjectServer.itemProperties.twoHandedAnimation)
					{
						playerBodyAnimator.ResetTrigger("SwitchHoldAnimationTwoHanded");
						playerBodyAnimator.SetTrigger("SwitchHoldAnimationTwoHanded");
					}
					playerBodyAnimator.ResetTrigger("SwitchHoldAnimation");
					playerBodyAnimator.SetTrigger("SwitchHoldAnimation");
				}
				twoHandedAnimation = ItemSlots[slot].itemProperties.twoHandedAnimation;
				twoHanded = ItemSlots[slot].itemProperties.twoHanded;
				playerBodyAnimator.SetBool("GrabValidated", value: true);
				playerBodyAnimator.SetBool("cancelHolding", value: false);
				isHoldingObject = true;
				currentlyHeldObjectServer = ItemSlots[slot];
			}
			else
			{
				if (!base.IsOwner && heldObjectServerCopy != null)
				{
					heldObjectServerCopy.SetActive(value: false);
				}
				if (base.IsOwner)
				{
					HUDManager.Instance.ClearControlTips();
				}
				currentlyHeldObjectServer = null;
				currentlyHeldObject = null;
				isHoldingObject = false;
				twoHanded = false;
				playerBodyAnimator.SetBool("cancelHolding", value: true);
			}
			if (base.IsOwner)
			{
				if (twoHanded)
				{
					HUDManager.Instance.PingHUDElement(HUDManager.Instance.Inventory, 0.1f, 0.13f, 0.13f);
					HUDManager.Instance.holdingTwoHandedItem.enabled = true;
				}
				else
				{
					HUDManager.Instance.PingHUDElement(HUDManager.Instance.Inventory, 1.5f, 1f, 0.13f);
					HUDManager.Instance.holdingTwoHandedItem.enabled = false;
				}
			}
		}

		private void ScrollMouse_performed(InputAction.CallbackContext context)
		{
			if (inTerminalMenu)
			{
				float num = context.ReadValue<float>();
				terminalScrollVertical.value += num / 3f;
			}
			else if (((base.IsOwner && isPlayerControlled && (!base.IsServer || isHostPlayerObject)) || isTestingPlayer) && !(timeSinceSwitchingSlots < 0.3f) && !isGrabbingObjectAnimation && !quickMenuManager.isMenuOpen && !inSpecialInteractAnimation && !throwingObject && !isTypingChat && !twoHanded && !activatingItem && !jetpackControls && !disablingJetpackControls)
			{
				ShipBuildModeManager.Instance.CancelBuildMode();
				playerBodyAnimator.SetBool("GrabValidated", value: false);
				if (context.ReadValue<float>() > 0f)
				{
					SwitchToItemSlot(NextItemSlot(forward: true));
					SwitchItemSlotsServerRpc(forward: true);
				}
				else
				{
					SwitchToItemSlot(NextItemSlot(forward: false));
					SwitchItemSlotsServerRpc(forward: false);
				}
				if (currentlyHeldObjectServer != null)
				{
					currentlyHeldObjectServer.gameObject.GetComponent<AudioSource>().PlayOneShot(currentlyHeldObjectServer.itemProperties.grabSFX, 0.6f);
				}
				timeSinceSwitchingSlots = 0f;
			}
		}

		[ServerRpc]
		private void SwitchItemSlotsServerRpc(bool forward)
		{
			SwitchItemSlotsClientRpc(forward);
		}

		[ClientRpc]
		private void SwitchItemSlotsClientRpc(bool forward)
		{
			if (!base.IsOwner)
			{
				SwitchToItemSlot(NextItemSlot(forward));
			}
		}

		private bool InteractTriggerUseConditionsMet()
		{
			if (sinkingValue > 0.73f)
			{
				return false;
			}
			if (isClimbingLadder)
			{
				if (hoveringOverTrigger.isLadder)
				{
					if (!hoveringOverTrigger.usingLadder)
					{
						return false;
					}
				}
				else if (hoveringOverTrigger.specialCharacterAnimation)
				{
					return false;
				}
			}
			else if (inSpecialInteractAnimation)
			{
				return false;
			}
			if (hoveringOverTrigger.isPlayingSpecialAnimation)
			{
				return false;
			}
			return true;
		}

		private void InspectItem_performed(InputAction.CallbackContext context)
		{
			if (!ShipBuildModeManager.Instance.InBuildMode && context.performed && ((base.IsOwner && isPlayerControlled && (!base.IsServer || isHostPlayerObject)) || isTestingPlayer) && !isGrabbingObjectAnimation && !isTypingChat && !inSpecialInteractAnimation && !throwingObject && !activatingItem)
			{
				ShipBuildModeManager.Instance.CancelBuildMode();
				if (currentlyHeldObjectServer != null)
				{
					currentlyHeldObjectServer.InspectItem();
				}
			}
		}

		private void QEItemInteract_performed(InputAction.CallbackContext context)
		{
			if (!equippedUsableItemQE || ((!base.IsOwner || !isPlayerControlled || (base.IsServer && !isHostPlayerObject)) && !isTestingPlayer) || !context.performed || isGrabbingObjectAnimation || isTypingChat || inTerminalMenu || inSpecialInteractAnimation || throwingObject || timeSinceSwitchingSlots < 0.2f)
			{
				return;
			}
			float num = context.ReadValue<float>();
			if (currentlyHeldObjectServer != null)
			{
				timeSinceSwitchingSlots = 0f;
				if (num < 0f)
				{
					currentlyHeldObjectServer.ItemInteractLeftRightOnClient(right: false);
				}
				else
				{
					currentlyHeldObjectServer.ItemInteractLeftRightOnClient(right: true);
				}
			}
		}

		private void ItemSecondaryUse_performed(InputAction.CallbackContext context)
		{
			Debug.Log("secondary use A");
			if (!equippedUsableItemQE)
			{
				return;
			}
			Debug.Log("secondary use B");
			if ((!base.IsOwner || !isPlayerControlled || (base.IsServer && !isHostPlayerObject)) && !isTestingPlayer)
			{
				return;
			}
			Debug.Log("secondary use C");
			if (!context.performed)
			{
				return;
			}
			Debug.Log("secondary use D");
			if (isGrabbingObjectAnimation || isTypingChat || inTerminalMenu || inSpecialInteractAnimation || throwingObject)
			{
				return;
			}
			Debug.Log("secondary use E");
			if (!(timeSinceSwitchingSlots < 0.2f))
			{
				Debug.Log("secondary use F");
				if (currentlyHeldObjectServer != null)
				{
					Debug.Log("secondary use G");
					timeSinceSwitchingSlots = 0f;
					currentlyHeldObjectServer.ItemInteractLeftRightOnClient(right: false);
				}
			}
		}

		private void ItemTertiaryUse_performed(InputAction.CallbackContext context)
		{
			if (equippedUsableItemQE && ((base.IsOwner && isPlayerControlled && (!base.IsServer || isHostPlayerObject)) || isTestingPlayer) && context.performed && !isGrabbingObjectAnimation && !isTypingChat && !inTerminalMenu && !inSpecialInteractAnimation && !throwingObject && !(timeSinceSwitchingSlots < 0.2f) && currentlyHeldObjectServer != null)
			{
				timeSinceSwitchingSlots = 0f;
				currentlyHeldObjectServer.ItemInteractLeftRightOnClient(right: true);
			}
		}

		private void Interact_performed(InputAction.CallbackContext context)
		{
			if (base.IsOwner && isPlayerDead && (!base.IsServer || isHostPlayerObject))
			{
				if (!StartOfRound.Instance.overrideSpectateCamera && spectatedPlayerScript != null && !spectatedPlayerScript.isPlayerDead)
				{
					SpectateNextPlayer();
				}
			}
			else
			{
				if (((!base.IsOwner || !isPlayerControlled || (base.IsServer && !isHostPlayerObject)) && !isTestingPlayer) || !context.performed || timeSinceSwitchingSlots < 0.2f)
				{
					return;
				}
				ShipBuildModeManager.Instance.CancelBuildMode();
				if (!isGrabbingObjectAnimation && !isTypingChat && !inTerminalMenu && !throwingObject && !IsInspectingItem && !(inAnimationWithEnemy != null) && !jetpackControls && !disablingJetpackControls && !StartOfRound.Instance.suckingPlayersOutOfShip)
				{
					if (!activatingItem)
					{
						BeginGrabObject();
					}
					if (!(hoveringOverTrigger == null) && !hoveringOverTrigger.holdInteraction && (!isHoldingObject || hoveringOverTrigger.oneHandedItemAllowed) && (!twoHanded || (hoveringOverTrigger.twoHandedItemAllowed && !hoveringOverTrigger.specialCharacterAnimation)) && InteractTriggerUseConditionsMet())
					{
						hoveringOverTrigger.Interact(thisPlayerBody);
					}
				}
			}
		}

		private void BeginGrabObject()
		{
			interactRay = new Ray(gameplayCamera.transform.position, gameplayCamera.transform.forward);
			if (!Physics.Raycast(interactRay, out hit, grabDistance, interactableObjectsMask) || hit.collider.gameObject.layer == 8 || !(hit.collider.tag == "PhysicsProp") || twoHanded || sinkingValue > 0.73f)
			{
				return;
			}
			currentlyGrabbingObject = hit.collider.transform.gameObject.GetComponent<GrabbableObject>();
			if (!GameNetworkManager.Instance.gameHasStarted && !currentlyGrabbingObject.itemProperties.canBeGrabbedBeforeGameStart && StartOfRound.Instance.testRoom == null)
			{
				return;
			}
			grabInvalidated = false;
			if (currentlyGrabbingObject == null || inSpecialInteractAnimation || currentlyGrabbingObject.isHeld || currentlyGrabbingObject.isPocketed)
			{
				return;
			}
			NetworkObject networkObject = currentlyGrabbingObject.NetworkObject;
			if (networkObject == null || !networkObject.IsSpawned)
			{
				return;
			}
			currentlyGrabbingObject.InteractItem();
			if (currentlyGrabbingObject.grabbable && FirstEmptyItemSlot() != -1)
			{
				playerBodyAnimator.SetBool("GrabInvalidated", value: false);
				playerBodyAnimator.SetBool("GrabValidated", value: false);
				playerBodyAnimator.SetBool("cancelHolding", value: false);
				playerBodyAnimator.ResetTrigger("Throw");
				SetSpecialGrabAnimationBool(setTrue: true);
				isGrabbingObjectAnimation = true;
				cursorIcon.enabled = false;
				cursorTip.text = "";
				twoHanded = currentlyGrabbingObject.itemProperties.twoHanded;
				carryWeight += Mathf.Clamp(currentlyGrabbingObject.itemProperties.weight - 1f, 0f, 10f);
				if (currentlyGrabbingObject.itemProperties.grabAnimationTime > 0f)
				{
					grabObjectAnimationTime = currentlyGrabbingObject.itemProperties.grabAnimationTime;
				}
				else
				{
					grabObjectAnimationTime = 0.4f;
				}
				if (!isTestingPlayer)
				{
					GrabObjectServerRpc(networkObject);
				}
				if (grabObjectCoroutine != null)
				{
					StopCoroutine(grabObjectCoroutine);
				}
				grabObjectCoroutine = StartCoroutine(GrabObject());
			}
		}

		private IEnumerator GrabObject()
		{
			grabbedObjectValidated = false;
			yield return new WaitForSeconds(0.1f);
			currentlyGrabbingObject.parentObject = localItemHolder;
			if (currentlyGrabbingObject.itemProperties.grabSFX != null)
			{
				itemAudio.PlayOneShot(currentlyGrabbingObject.itemProperties.grabSFX, 1f);
			}
			if (currentlyGrabbingObject.playerHeldBy != null)
			{
				Debug.Log($"playerHeldBy on currentlyGrabbingObject 1: {currentlyGrabbingObject.playerHeldBy}");
			}
			while ((currentlyGrabbingObject != currentlyHeldObjectServer || !currentlyHeldObjectServer.wasOwnerLastFrame) && !grabInvalidated)
			{
				Debug.Log($"grabInvalidated: {grabInvalidated}");
				yield return null;
			}
			if (grabInvalidated)
			{
				grabInvalidated = false;
				Debug.Log("Grab was invalidated on object: " + currentlyGrabbingObject.name);
				if (currentlyGrabbingObject.playerHeldBy != null)
				{
					Debug.Log($"playerHeldBy on currentlyGrabbingObject 2: {currentlyGrabbingObject.playerHeldBy}");
				}
				if (currentlyGrabbingObject.parentObject == localItemHolder)
				{
					if (currentlyGrabbingObject.playerHeldBy != null)
					{
						Debug.Log($"Grab invalidated; giving grabbed object to the client who got it first; {currentlyGrabbingObject.playerHeldBy}");
						currentlyGrabbingObject.parentObject = currentlyGrabbingObject.playerHeldBy.serverItemHolder;
					}
					else
					{
						Debug.Log("Grab invalidated; no other client has possession of it, so set its parent object to null.");
						currentlyGrabbingObject.parentObject = null;
					}
				}
				twoHanded = false;
				SetSpecialGrabAnimationBool(setTrue: false);
				if (currentlyHeldObjectServer != null)
				{
					playerBodyAnimator.SetBool("Grab", value: true);
				}
				playerBodyAnimator.SetBool("GrabInvalidated", value: true);
				carryWeight = Mathf.Clamp(carryWeight - (currentlyGrabbingObject.itemProperties.weight - 1f), 0f, 10f);
				isGrabbingObjectAnimation = false;
				currentlyGrabbingObject = null;
			}
			else
			{
				grabbedObjectValidated = true;
				currentlyHeldObjectServer.GrabItemOnClient();
				isHoldingObject = true;
				yield return new WaitForSeconds(grabObjectAnimationTime - 0.2f);
				playerBodyAnimator.SetBool("GrabValidated", value: true);
				isGrabbingObjectAnimation = false;
			}
		}

		private void SetSpecialGrabAnimationBool(bool setTrue, GrabbableObject currentItem = null)
		{
			if (currentItem == null)
			{
				currentItem = currentlyGrabbingObject;
			}
			if (!base.IsOwner)
			{
				return;
			}
			playerBodyAnimator.SetBool("Grab", setTrue);
			if (string.IsNullOrEmpty(currentItem.itemProperties.grabAnim))
			{
				return;
			}
			try
			{
				playerBodyAnimator.SetBool(currentItem.itemProperties.grabAnim, setTrue);
			}
			catch (Exception)
			{
				Debug.LogError("An item tried to set an animator bool which does not exist: " + currentItem.itemProperties.grabAnim);
			}
		}

		[ServerRpc]
		private void GrabObjectServerRpc(NetworkObjectReference grabbedObject)
		{
			if (currentlyHeldObjectServer != null)
			{
				currentlyHeldObjectServer.gameObject.GetComponent<NetworkObject>().Despawn();
			}
			DespawnHeldObjectClientRpc();
		}

		[ClientRpc]
		private void DespawnHeldObjectClientRpc()
		{
			if (!base.IsOwner)
			{
				DespawnHeldObjectOnClient();
			}
			else
			{
				throwingObject = false;
			}
		}

		public void DiscardHeldObject(bool placeObject = false, NetworkObject parentObjectTo = null, Vector3 placePosition = default(Vector3), bool matchRotationOfParent = true)
		{
			SetSpecialGrabAnimationBool(setTrue: false, currentlyHeldObjectServer);
			playerBodyAnimator.SetBool("cancelHolding", value: true);
			playerBodyAnimator.SetTrigger("Throw");
			HUDManager.Instance.itemSlotIcons[currentItemSlot].enabled = false;
			HUDManager.Instance.holdingTwoHandedItem.enabled = false;
			if (placeObject)
			{
				if (parentObjectTo == null)
				{
					throwingObject = true;
					placePosition = ((!isInElevator) ? StartOfRound.Instance.propsContainer.InverseTransformPoint(placePosition) : StartOfRound.Instance.elevatorTransform.InverseTransformPoint(placePosition));
					int floorYRot = (int)base.transform.localEulerAngles.y;
					SetObjectAsNoLongerHeld(isInElevator, isInHangarShipRoom, placePosition, currentlyHeldObjectServer, floorYRot);
					currentlyHeldObjectServer.DiscardItemOnClient();
					ThrowObjectServerRpc(currentlyHeldObjectServer.gameObject.GetComponent<NetworkObject>(), isInElevator, isInHangarShipRoom, placePosition, floorYRot);
				}
				else
				{
					PlaceGrabbableObject(parentObjectTo.transform, placePosition, matchRotationOfParent, currentlyHeldObjectServer);
					currentlyHeldObjectServer.DiscardItemOnClient();
					PlaceObjectServerRpc(currentlyHeldObjectServer.gameObject.GetComponent<NetworkObject>(), parentObjectTo, placePosition, matchRotationOfParent);
				}
				return;
			}
			throwingObject = true;
			bool droppedInElevator = isInElevator;
			Vector3 targetFloorPosition;
			if (!isInElevator)
			{
				Vector3 vector = ((!currentlyHeldObjectServer.itemProperties.allowDroppingAheadOfPlayer) ? currentlyHeldObjectServer.GetItemFloorPosition() : DropItemAheadOfPlayer());
				if (!playersManager.shipBounds.bounds.Contains(vector))
				{
					targetFloorPosition = playersManager.propsContainer.InverseTransformPoint(vector);
				}
				else
				{
					droppedInElevator = true;
					targetFloorPosition = playersManager.elevatorTransform.InverseTransformPoint(vector);
				}
			}
			else
			{
				Vector3 vector = currentlyHeldObjectServer.GetItemFloorPosition();
				if (!playersManager.shipBounds.bounds.Contains(vector))
				{
					droppedInElevator = false;
					targetFloorPosition = playersManager.propsContainer.InverseTransformPoint(vector);
				}
				else
				{
					targetFloorPosition = playersManager.elevatorTransform.InverseTransformPoint(vector);
				}
			}
			int floorYRot2 = (int)base.transform.localEulerAngles.y;
			SetObjectAsNoLongerHeld(droppedInElevator, isInHangarShipRoom, targetFloorPosition, currentlyHeldObjectServer, floorYRot2);
			currentlyHeldObjectServer.DiscardItemOnClient();
			ThrowObjectServerRpc(currentlyHeldObjectServer.NetworkObject, droppedInElevator, isInHangarShipRoom, targetFloorPosition, floorYRot2);
		}

		private Vector3 DropItemAheadOfPlayer()
		{
			Vector3 zero = Vector3.zero;
			Ray ray = new Ray(base.transform.position + Vector3.up * 0.4f, gameplayCamera.transform.forward);
			zero = ((!Physics.Raycast(ray, out hit, 1.7f, 268438273, QueryTriggerInteraction.Ignore)) ? ray.GetPoint(1.7f) : ray.GetPoint(Mathf.Clamp(hit.distance - 0.3f, 0.01f, 2f)));
			Vector3 itemFloorPosition = currentlyHeldObjectServer.GetItemFloorPosition(zero);
			if (itemFloorPosition == zero)
			{
				itemFloorPosition = currentlyHeldObjectServer.GetItemFloorPosition();
			}
			return itemFloorPosition;
		}

		[ServerRpc]
		private void ThrowObjectServerRpc(NetworkObjectReference grabbedObject, bool droppedInElevator, bool droppedInShipRoom, Vector3 targetFloorPosition, int floorYRot)
		{
			if (grabbedObject.TryGet(out var _))
			{
				ThrowObjectClientRpc(droppedInElevator, droppedInShipRoom, targetFloorPosition, grabbedObject, floorYRot);
			}
			else
			{
				Debug.LogError("Object was not thrown because it does not exist on the server.");
			}
		}

		[ClientRpc]
		private void ThrowObjectClientRpc(bool droppedInElevator, bool droppedInShipRoom, Vector3 targetFloorPosition, NetworkObjectReference grabbedObject, int floorYRot)
		{
			NetworkObject networkObject3;
			NetworkObject networkObject4;
			if (grabbedObject.TryGet(out var _) && parentObject.TryGet(out var _))
			{
				PlaceObjectClientRpc(parentObject, placePositionOffset, matchRotationOfParent, grabbedObject);
			}
			else if (!grabbedObject.TryGet(out networkObject3))
			{
				Debug.LogError($"Object placement not synced to clients, missing reference to a network object: placing object with id: {grabbedObject.NetworkObjectId}; player #{playerClientId}");
			}
			else if (!parentObject.TryGet(out networkObject4))
			{
				Debug.LogError($"Object placement not synced to clients, missing reference to a network object: parent object with id: {grabbedObject.NetworkObjectId}; player #{playerClientId}");
			}
		}

		public void PlaceGrabbableObject(Transform parentObject, Vector3 positionOffset, bool matchRotationOfParent, GrabbableObject placeObject)
		{
			placeObject.EnablePhysics(enable: true);
			placeObject.EnableItemMeshes(enable: true);
			placeObject.isHeld = false;
			placeObject.isPocketed = false;
			placeObject.heldByPlayerOnServer = false;
			SetItemInElevator(isInHangarShipRoom, isInElevator, placeObject);
			placeObject.parentObject = null;
			placeObject.transform.SetParent(parentObject, worldPositionStays: true);
			placeObject.startFallingPosition = placeObject.transform.localPosition;
			placeObject.transform.localScale = placeObject.originalScale;
			placeObject.transform.localPosition = positionOffset;
			placeObject.targetFloorPosition = positionOffset;
			if (!matchRotationOfParent)
			{
				placeObject.fallTime = 0f;
			}
			else
			{
				placeObject.transform.localEulerAngles = new Vector3(0f, 0f, 0f);
				placeObject.fallTime = 1.1f;
			}
			placeObject.OnPlaceObject();
			for (int i = 0; i < ItemSlots.Length; i++)
			{
				if (ItemSlots[i] == placeObject)
				{
					ItemSlots[i] = null;
				}
			}
			twoHanded = false;
			twoHandedAnimation = false;
			carryWeight -= Mathf.Clamp(placeObject.itemProperties.weight - 1f, 0f, 10f);
			isHoldingObject = false;
			hasThrownObject = true;
		}

		[ClientRpc]
		private void PlaceObjectClientRpc(NetworkObjectReference parentObjectReference, Vector3 placePositionOffset, bool matchRotationOfParent, NetworkObjectReference grabbedObject)
		{
			playersManager.gameStats.allPlayerStats[playerClientId].turnAmount++;
			if (!base.IsOwner)
			{
				targetYRot = newYRot;
				targetLookRot = newRot;
			}
		}

		[ServerRpc]
		private void UpdatePlayerRotationFullServerRpc(Vector3 playerEulers)
		{
			if (!base.IsOwner)
			{
				syncFullRotation = playerEulers;
			}
		}

		private void UpdatePlayerAnimationsToOtherClients(Vector2 moveInputVector)
		{
			updatePlayerAnimationsInterval += Time.deltaTime;
			if (!inSpecialInteractAnimation && !(updatePlayerAnimationsInterval > 0.14f))
			{
				return;
			}
			updatePlayerAnimationsInterval = 0f;
			currentAnimationSpeed = playerBodyAnimator.GetFloat("animationSpeed");
			for (int i = 0; i < playerBodyAnimator.layerCount; i++)
			{
				currentAnimationStateHash[i] = playerBodyAnimator.GetCurrentAnimatorStateInfo(i).fullPathHash;
				if (previousAnimationStateHash[i] != currentAnimationStateHash[i])
				{
					previousAnimationStateHash[i] = currentAnimationStateHash[i];
					previousAnimationSpeed = currentAnimationSpeed;
					UpdatePlayerAnimationServerRpc(currentAnimationStateHash[i], currentAnimationSpeed);
					return;
				}
			}
			if (previousAnimationSpeed != currentAnimationSpeed)
			{
				previousAnimationSpeed = currentAnimationSpeed;
				UpdatePlayerAnimationServerRpc(0, currentAnimationSpeed);
			}
		}

		[ServerRpc]
		private void UpdatePlayerAnimationServerRpc(int animationState, float animationSpeed)
		{
			LandFromJumpClientRpc(fallHard);
		}

		[ClientRpc]
		public void LandFromJumpClientRpc(bool fallHard)
		{
			if (!base.IsOwner)
			{
				if (fallHard)
				{
				movementAudio.PlayOneShot(StartOfRound.Instance.playerHitGroundHard, 1f);
				}
				else
				{
				movementAudio.PlayOneShot(StartOfRound.Instance.playerHitGroundSoft, 0.7f);
				}
			}
		}

		public void LimpAnimationSpeed()
		{
			if (base.IsOwner)
			{
				limpMultiplier = 0.75f;
			}
		}

		public void SpawnPlayerAnimation()
		{
			UpdateSpecialAnimationValue(specialAnimation: true, 0);
			inSpecialInteractAnimation = true;
			playerBodyAnimator.ResetTrigger("SpawnPlayer");
			playerBodyAnimator.SetTrigger("SpawnPlayer");
			StartCoroutine(spawnPlayerAnimTimer());
		}

		private IEnumerator spawnPlayerAnimTimer()
		{
			yield return new WaitForSeconds(3f);
			inSpecialInteractAnimation = false;
			UpdateSpecialAnimationValue(specialAnimation: false, 0);
		}

		[ServerRpc(RequireOwnership = false)]
		private void SendNewPlayerValuesServerRpc(ulong newPlayerSteamId)
		{
			DisableJetpackModeClientRpc();
		}

		[ClientRpc]
		public void DisableJetpackModeClientRpc()
		{
			if (!base.IsOwner)
			{
				DisableJetpackControlsLocally();
			}
		}

		public void DisableJetpackControlsLocally()
		{
			jetpackControls = false;
			thisController.radius = 0.4f;
			jetpackTurnCompass.rotation = base.transform.rotation;
			startedJetpackControls = false;
			disablingJetpackControls = false;
		}

		private void Update()
		{
			if ((base.IsOwner && isPlayerControlled && (!base.IsServer || isHostPlayerObject)) || isTestingPlayer)
			{
				if (isCameraDisabled)
				{
					isCameraDisabled = false;
					Debug.Log("Taking control of player " + base.gameObject.name + " and enabling camera!");
					StartOfRound.Instance.SwitchCamera(gameplayCamera);
					thisPlayerModel.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
					mapRadarDirectionIndicator.enabled = true;
					thisPlayerModelArms.enabled = true;
					playerScreen.enabled = true;
					Cursor.lockState = CursorLockMode.Locked;
					Cursor.visible = false;
					base.gameObject.GetComponent<CharacterController>().enabled = true;
					activeAudioReverbFilter = activeAudioListener.GetComponent<AudioReverbFilter>();
					activeAudioReverbFilter.enabled = true;
					ChangeAudioListenerToObject(gameplayCamera.gameObject);
					if (playerBodyAnimator.runtimeAnimatorController != playersManager.localClientAnimatorController)
					{
						playerBodyAnimator.runtimeAnimatorController = playersManager.localClientAnimatorController;
					}
					if (justConnected)
					{
						justConnected = false;
						ConnectClientToPlayerObject();
					}
					SpawnPlayerAnimation();
					Debug.Log("!!!! ENABLING CAMERA FOR PLAYER: " + base.gameObject.name);
					Debug.Log($"!!!! connectedPlayersAmount: {playersManager.connectedPlayersAmount}");
				}
				hasBegunSpectating = false;
				playerHudUIContainer.rotation = Quaternion.Lerp(playerHudUIContainer.rotation, playerHudBaseRotation.rotation, 24f * Time.deltaTime);
				SetNightVisionEnabled(isNotLocalClient: false);
				if (!inSpecialInteractAnimation || inShockingMinigame)
				{
					if (!thisController.isGrounded)
					{
						if (jetpackControls && !disablingJetpackControls)
						{
							fallValue = Mathf.MoveTowards(fallValue, -8f, 7f * Time.deltaTime);
							fallValueUncapped = -8f;
						}
						else
						{
							fallValue = Mathf.Clamp(fallValue - 38f * Time.deltaTime, -150f, jumpForce);
							fallValueUncapped -= 38f * Time.deltaTime;
						}
						if (!isJumping && !isFallingFromJump)
						{
							if (!isFallingNoJump)
							{
								isFallingNoJump = true;
								fallValue = -7f;
							}
							else if (fallValue < -20f)
							{
								isCrouching = false;
								playerBodyAnimator.SetBool("crouching", value: false);
								playerBodyAnimator.SetBool("FallNoJump", value: true);
							}
						}
						if (fallValueUncapped < -35f)
						{
							takingFallDamage = true;
						}
					}
					else
					{
						movementHinderedPrev = isMovementHindered;
						if (!isJumping)
						{
							if (isFallingNoJump && !jetpackControls)
							{
								isFallingNoJump = false;
								if (!isCrouching && fallValue < -9f)
								{
									playerBodyAnimator.SetTrigger("ShortFallLanding");
								}
								PlayerHitGroundEffects();
							}
							fallValue = -7f;
							fallValueUncapped = -7f;
						}
						playerBodyAnimator.SetBool("FallNoJump", value: false);
					}
				}
				ForceTurnTowardsTarget();
				if (inTerminalMenu)
				{
					targetFOV = 60f;
				}
				else if (IsInspectingItem)
				{
					rightArmProceduralRig.weight = Mathf.Lerp(rightArmProceduralRig.weight, 1f, 25f * Time.deltaTime);
					targetFOV = 46f;
				}
				else
				{
					rightArmProceduralRig.weight = Mathf.Lerp(rightArmProceduralRig.weight, 0f, 25f * Time.deltaTime);
					if (isSprinting)
					{
						targetFOV = 68f;
					}
					else
					{
						targetFOV = 66f;
					}
				}
				gameplayCamera.fieldOfView = Mathf.Lerp(gameplayCamera.fieldOfView, targetFOV, 6f * Time.deltaTime);
				moveInputVector = IngamePlayerSettings.Instance.playerInput.actions.FindAction("Move").ReadValue<Vector2>();
				float num = IngamePlayerSettings.Instance.playerInput.actions.FindAction("Sprint").ReadValue<float>();
				if (quickMenuManager.isMenuOpen || isTypingChat || (inSpecialInteractAnimation && !isClimbingLadder && !inShockingMinigame))
				{
					moveInputVector = Vector2.zero;
				}
				SetFaceUnderwaterFilters();
				if (isWalking)
				{
					if (isFreeCamera || moveInputVector.sqrMagnitude <= 0.19f || (inSpecialInteractAnimation && !isClimbingLadder && !inShockingMinigame))
					{
						isWalking = false;
						isSprinting = false;
						playerBodyAnimator.SetBool("Walking", value: false);
						playerBodyAnimator.SetBool("Sprinting", value: false);
						playerBodyAnimator.SetBool("Sideways", value: false);
					}
					else if (num > 0.3f && movementHinderedPrev <= 0 && !criticallyInjured && sprintMeter > 0.1f)
					{
						if (!isSprinting && sprintMeter < 0.3f)
						{
							if (!isExhausted)
							{
								isExhausted = true;
							}
						}
						else
						{
							if (isCrouching)
							{
								Crouch(crouch: false);
							}
							isSprinting = true;
							playerBodyAnimator.SetBool("Sprinting", value: true);
						}
					}
					else
					{
						isSprinting = false;
						if (sprintMeter < 0.1f)
						{
							isExhausted = true;
						}
						playerBodyAnimator.SetBool("Sprinting", value: false);
					}
					if (isSprinting)
					{
						sprintMultiplier = Mathf.Lerp(sprintMultiplier, 2.25f, Time.deltaTime * 1f);
					}
					else
					{
						sprintMultiplier = Mathf.Lerp(sprintMultiplier, 1f, 10f * Time.deltaTime);
					}
					if (moveInputVector.y < 0.2f && moveInputVector.y > -0.2f && !inSpecialInteractAnimation)
					{
						playerBodyAnimator.SetBool("Sideways", value: true);
						isSidling = true;
					}
					else
					{
						playerBodyAnimator.SetBool("Sideways", value: false);
						isSidling = false;
					}
					if (enteringSpecialAnimation)
					{
						playerBodyAnimator.SetFloat("animationSpeed", 1f);
					}
					else if (moveInputVector.y < 0.5f && moveInputVector.x < 0.5f)
					{
						playerBodyAnimator.SetFloat("animationSpeed", -1f);
						movingForward = false;
					}
					else
					{
						playerBodyAnimator.SetFloat("animationSpeed", 1f);
						movingForward = true;
					}
				}
				else
				{
					if (enteringSpecialAnimation)
					{
						playerBodyAnimator.SetFloat("animationSpeed", 1f);
					}
					else if (isClimbingLadder)
					{
						playerBodyAnimator.SetFloat("animationSpeed", 0f);
					}
					if (!isFreeCamera && moveInputVector.sqrMagnitude >= 0.19f && (!inSpecialInteractAnimation || isClimbingLadder || inShockingMinigame))
					{
						isWalking = true;
						playerBodyAnimator.SetBool("Walking", value: true);
					}
				}
				if (performingEmote && !CheckConditionsForEmote())
				{
					performingEmote = false;
					StopPerformingEmoteServerRpc();
					timeSinceStartingEmote = 0f;
				}
				timeSinceStartingEmote += Time.deltaTime;
				playerBodyAnimator.SetBool("hinderedMovement", isMovementHindered > 0);
				if (sourcesCausingSinking == 0)
				{
					if (isSinking)
					{
						isSinking = false;
						StopSinkingServerRpc();
					}
				}
				else
				{
					if (isSinking)
					{
						GetCurrentMaterialStandingOn();
						if (!CheckConditionsForSinkingInQuicksand())
						{
							isSinking = false;
							StopSinkingServerRpc();
						}
					}
					else if (!isSinking && CheckConditionsForSinkingInQuicksand())
					{
						isSinking = true;
						StartSinkingServerRpc(sinkingSpeedMultiplier, statusEffectAudioIndex);
					}
					if (sinkingValue >= 1f)
					{
						KillPlayer(Vector3.zero, spawnBody: false, CauseOfDeath.Suffocation);
					}
					else if (sinkingValue > 0.5f)
					{
						Crouch(crouch: false);
					}
				}
				if (isCrouching)
				{
					timeSinceCrouching = 0f;
					thisController.center = Vector3.Lerp(thisController.center, new Vector3(thisController.center.x, 0.72f, thisController.center.z), 8f * Time.deltaTime);
					thisController.height = Mathf.Lerp(thisController.height, 1.5f, 8f * Time.deltaTime);
				}
				else
				{
					timeSinceCrouching += Time.deltaTime;
					thisController.center = Vector3.Lerp(thisController.center, new Vector3(thisController.center.x, 1.28f, thisController.center.z), 8f * Time.deltaTime);
					thisController.height = Mathf.Lerp(thisController.height, 2.5f, 8f * Time.deltaTime);
				}
				if (isFreeCamera)
				{
					float num2 = movementSpeed / 1.75f;
					if (num > 0.5f)
					{
						num2 *= 5f;
					}
					Vector3 vector = (playersManager.freeCinematicCameraTurnCompass.transform.right * moveInputVector.x + playersManager.freeCinematicCameraTurnCompass.transform.forward * moveInputVector.y) * num2;
					playersManager.freeCinematicCameraTurnCompass.transform.position += vector * Time.deltaTime;
					StartOfRound.Instance.freeCinematicCamera.transform.position = Vector3.Lerp(StartOfRound.Instance.freeCinematicCamera.transform.position, StartOfRound.Instance.freeCinematicCameraTurnCompass.transform.position, 3f * Time.deltaTime);
					StartOfRound.Instance.freeCinematicCamera.transform.rotation = Quaternion.Slerp(StartOfRound.Instance.freeCinematicCamera.transform.rotation, StartOfRound.Instance.freeCinematicCameraTurnCompass.rotation, 3f * Time.deltaTime);
				}
				if (jetpackControls)
				{
					if (disablingJetpackControls && thisController.isGrounded)
					{
						DisableJetpackControlsLocally();
						DisableJetpackModeServerRpc();
					}
					else if (!thisController.isGrounded)
					{
						if (!startedJetpackControls)
						{
							startedJetpackControls = true;
							jetpackTurnCompass.rotation = base.transform.rotation;
						}
						thisController.radius = Mathf.Lerp(thisController.radius, 1.25f, 10f * Time.deltaTime);
						jetpackTurnCompass.Rotate(new Vector3(0f, 0f, 0f - moveInputVector.x) * (180f * Time.deltaTime), Space.Self);
						jetpackTurnCompass.Rotate(new Vector3(moveInputVector.y, 0f, 0f) * (180f * Time.deltaTime), Space.Self);
						base.transform.rotation = Quaternion.Slerp(base.transform.rotation, jetpackTurnCompass.rotation, 8f * Time.deltaTime);
					}
				}
				else if (!isClimbingLadder)
				{
					Vector3 localEulerAngles = base.transform.localEulerAngles;
					localEulerAngles.x = Mathf.LerpAngle(localEulerAngles.x, 0f, 15f * Time.deltaTime);
					localEulerAngles.z = Mathf.LerpAngle(localEulerAngles.z, 0f, 15f * Time.deltaTime);
					base.transform.localEulerAngles = localEulerAngles;
				}
				if (!inSpecialInteractAnimation || inShockingMinigame || StartOfRound.Instance.suckingPlayersOutOfShip)
				{
					if (isFreeCamera)
					{
						moveInputVector = Vector2.zero;
					}
					CalculateGroundNormal();
					float num3 = movementSpeed / carryWeight;
					if (sinkingValue > 0.73f)
					{
						num3 = 0f;
					}
					else
					{
						if (isCrouching)
						{
							num3 /= 1.5f;
						}
						else if (criticallyInjured && !isCrouching)
						{
							num3 *= limpMultiplier;
						}
						if (isSpeedCheating)
						{
							num3 *= 15f;
						}
						if (movementHinderedPrev > 0)
						{
							num3 /= 2f * hinderedMultiplier;
						}
						if (drunkness > 0f)
						{
							num3 *= StartOfRound.Instance.drunknessSpeedEffect.Evaluate(drunkness) / 5f + 1f;
						}
						if (!isCrouching && timeSinceCrouching < 1f)
						{
							num3 *= Mathf.Min(timeSinceCrouching + 0.4f, 1f);
						}
					}
					if (isTypingChat || (jetpackControls && !thisController.isGrounded) || StartOfRound.Instance.suckingPlayersOutOfShip)
					{
						moveInputVector = Vector2.zero;
					}
					Vector3 vector2 = new Vector3(0f, 0f, 0f);
					int num4 = Physics.OverlapSphereNonAlloc(base.transform.position, 0.65f, nearByPlayers, StartOfRound.Instance.playersMask);
					for (int i = 0; i < num4; i++)
					{
						vector2 += Vector3.Normalize((base.transform.position - nearByPlayers[i].transform.position) * 100f) * 1.2f;
					}
					int num5 = Physics.OverlapSphereNonAlloc(base.transform.position, 1.25f, nearByPlayers, 524288);
					for (int j = 0; j < num5; j++)
					{
						EnemyAICollisionDetect component = nearByPlayers[j].gameObject.GetComponent<EnemyAICollisionDetect>();
						if (component != null && !component.mainScript.isEnemyDead)
						{
							vector2 += Vector3.Normalize((base.transform.position - nearByPlayers[j].transform.position) * 100f) * 0.16f;
						}
					}
					float num6 = 1f;
					walkForce = Vector3.MoveTowards(maxDistanceDelta: ((isFallingFromJump || isFallingNoJump) ? 1.33f : ((drunkness > 0.3f) ? Mathf.Clamp(Mathf.Abs(drunkness - 2.25f), 0.3f, 2.5f) : ((!isCrouching && timeSinceCrouching < 0.2f) ? 15f : ((!isSprinting) ? (10f / carryWeight) : (5f / (carryWeight * 1.5f)))))) * Time.deltaTime, current: walkForce, target: base.transform.right * moveInputVector.x + base.transform.forward * moveInputVector.y);
					Vector3 vector3 = walkForce * num3 * sprintMultiplier + new Vector3(0f, fallValue, 0f) + vector2;
					vector3 += externalForces;
					externalForces = Vector3.zero;
					if (isPlayerSliding && thisController.isGrounded)
					{
						playerSlidingTimer += Time.deltaTime;
						if (slideFriction > maxSlideFriction)
						{
							slideFriction -= 35f * Time.deltaTime;
						}
						vector3 = new Vector3(vector3.x + (1f - playerGroundNormal.y) * playerGroundNormal.x * (1f - slideFriction), vector3.y, vector3.z + (1f - playerGroundNormal.y) * playerGroundNormal.z * (1f - slideFriction));
					}
					else
					{
						playerSlidingTimer = 0f;
						slideFriction = 0f;
					}
					float magnitude = thisController.velocity.magnitude;
					thisController.Move(vector3 * Time.deltaTime);
					if (!teleportingThisFrame && teleportedLastFrame)
					{
						teleportedLastFrame = false;
					}
					if (jetpackControls || disablingJetpackControls)
					{
						if (!teleportingThisFrame && !inSpecialInteractAnimation && !enteringSpecialAnimation && !isClimbingLadder && StartOfRound.Instance.timeSinceRoundStarted > 1f)
						{
							float magnitude2 = thisController.velocity.magnitude;
							if (getAverageVelocityInterval <= 0f)
							{
								getAverageVelocityInterval = 0.04f;
								velocityAverageCount++;
								if (velocityAverageCount > velocityMovingAverageLength)
								{
									averageVelocity += (magnitude2 - averageVelocity) / (float)(velocityMovingAverageLength + 1);
								}
								else
								{
									averageVelocity += magnitude2;
									if (velocityAverageCount == velocityMovingAverageLength)
									{
										averageVelocity /= velocityAverageCount;
									}
								}
							}
							else
							{
								getAverageVelocityInterval -= Time.deltaTime;
							}
							float num7 = averageVelocity - (magnitude2 + magnitude) / 2f;
							minVelocityToTakeDamage = 15f;
							if (jetpackControls)
							{
								float num8 = Vector3.Angle(Vector3.up, base.transform.up);
								if (num8 > 65f)
								{
									minVelocityToTakeDamage = 10f;
								}
								else if (num8 > 47f)
								{
									minVelocityToTakeDamage = 12.5f;
								}
							}
							if (timeSinceTakingGravityDamage > 1f && num7 > minVelocityToTakeDamage)
							{
								if (Physics.CheckSphere(gameplayCamera.transform.position, 3f, StartOfRound.Instance.collidersAndRoomMaskAndDefault))
								{
									Physics.OverlapSphere(gameplayCamera.transform.position, 3f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore);
									averageVelocity = 0f;
									int num9 = (int)((num7 - minVelocityToTakeDamage) / 6f);
									if (jetpackControls && (!disablingJetpackControls || Vector3.Angle(Vector3.up, base.transform.up) > 50f))
									{
										num9 += 40;
									}
									DamagePlayer(Mathf.Clamp(num9, 20, 100), hasDamageSFX: true, callRPC: true, CauseOfDeath.Gravity, 0, fallDamage: true, Vector3.ClampMagnitude(velocityLastFrame, 50f));
									timeSinceTakingGravityDamage = 0f;
								}
							}
							else
							{
								timeSinceTakingGravityDamage += Time.deltaTime;
							}
							velocityLastFrame = thisController.velocity;
							previousFrameDeltaTime = Time.deltaTime;
						}
						else
						{
							teleportingThisFrame = false;
						}
					}
					isPlayerSliding = Vector3.Angle(Vector3.up, playerGroundNormal) >= thisController.slopeLimit;
				}
				else if (isClimbingLadder)
				{
					Vector3 direction = thisPlayerBody.up;
					Vector3 origin = gameplayCamera.transform.position + thisPlayerBody.up * 0.07f;
					if (moveInputVector.y < 0f)
					{
						direction = -thisPlayerBody.up;
						origin = base.transform.position;
					}
					if (!Physics.Raycast(origin, direction, 0.15f, StartOfRound.Instance.allPlayersCollideWithMask, QueryTriggerInteraction.Ignore))
					{
						thisPlayerBody.transform.position += thisPlayerBody.up * (moveInputVector.y * climbSpeed * Time.deltaTime);
					}
				}
				teleportingThisFrame = false;
				playerEye.position = gameplayCamera.transform.position;
				playerEye.rotation = gameplayCamera.transform.rotation;
				if (isHoldingObject && currentlyHeldObjectServer == null)
				{
					DropAllHeldItems();
				}
				if ((NetworkManager.Singleton != null && !base.IsServer) || (!isTestingPlayer && playersManager.connectedPlayersAmount > 0) || oldConnectedPlayersAmount >= 1)
				{
					updatePlayerLookInterval += Time.deltaTime;
					UpdatePlayerAnimationsToOtherClients(moveInputVector);
				}
				ClickHoldInteraction();
			}
			else
			{
				if (!isCameraDisabled)
				{
					isCameraDisabled = true;
					gameplayCamera.enabled = false;
					visorCamera.enabled = false;
					thisPlayerModel.shadowCastingMode = ShadowCastingMode.On;
					thisPlayerModelArms.enabled = false;
					mapRadarDirectionIndicator.enabled = false;
					base.gameObject.GetComponent<CharacterController>().enabled = false;
					if (playerBodyAnimator.runtimeAnimatorController != playersManager.otherClientsAnimatorController)
					{
						playerBodyAnimator.runtimeAnimatorController = playersManager.otherClientsAnimatorController;
					}
					if (!isPlayerDead)
					{
						for (int k = 0; k < playersManager.allPlayerObjects.Length && !playersManager.allPlayerObjects[k].GetComponent<PlayerControllerB>().gameplayCamera.enabled; k++)
						{
							if (k == 4)
							{
								Debug.LogWarning("!!! No cameras are enabled !!!");
								playerScreen.enabled = false;
							}
						}
					}
					if ((bool)base.gameObject.GetComponent<Rigidbody>())
					{
						base.gameObject.GetComponent<Rigidbody>().interpolation = RigidbodyInterpolation.None;
					}
					Debug.Log("!!!! DISABLING CAMERA FOR PLAYER: " + base.gameObject.name);
					Debug.Log($"!!!! connectedPlayersAmount: {playersManager.connectedPlayersAmount}");
				}
				SetNightVisionEnabled(isNotLocalClient: true);
				if (!isTestingPlayer && !isPlayerDead && isPlayerControlled)
				{
					if (!disableSyncInAnimation)
					{
						if (snapToServerPosition)
						{
							base.transform.localPosition = Vector3.Lerp(base.transform.localPosition, serverPlayerPosition, 16f * Time.deltaTime);
						}
						else
						{
							float num10 = 8f;
							if (jetpackControls)
							{
								num10 = 15f;
							}
							float num11 = Mathf.Clamp(num10 * Vector3.Distance(base.transform.localPosition, serverPlayerPosition), 0.9f, 300f);
							base.transform.localPosition = Vector3.MoveTowards(base.transform.localPosition, serverPlayerPosition, num11 * Time.deltaTime);
						}
					}
					gameplayCamera.transform.localEulerAngles = new Vector3(Mathf.LerpAngle(gameplayCamera.transform.localEulerAngles.x, targetLookRot, 14f * Time.deltaTime), gameplayCamera.transform.localEulerAngles.y, gameplayCamera.transform.localEulerAngles.z);
					if (jetpackControls || isClimbingLadder)
					{
						if (!disableSyncInAnimation)
						{
							RoundManager.Instance.tempTransform.rotation = Quaternion.Euler(syncFullRotation);
							base.transform.rotation = Quaternion.Lerp(Quaternion.Euler(base.transform.eulerAngles), Quaternion.Euler(syncFullRotation), 8f * Time.deltaTime);
						}
					}
					else
					{
						if (!disableSyncInAnimation)
						{
							base.transform.eulerAngles = new Vector3(base.transform.eulerAngles.x, Mathf.LerpAngle(base.transform.eulerAngles.y, targetYRot, 14f * Time.deltaTime), base.transform.eulerAngles.z);
						}
						if (!inSpecialInteractAnimation && !disableSyncInAnimation)
						{
							Vector3 localEulerAngles2 = base.transform.localEulerAngles;
							localEulerAngles2.x = Mathf.LerpAngle(localEulerAngles2.x, 0f, 25f * Time.deltaTime);
							localEulerAngles2.z = Mathf.LerpAngle(localEulerAngles2.z, 0f, 25f * Time.deltaTime);
							base.transform.localEulerAngles = localEulerAngles2;
						}
					}
					playerEye.position = gameplayCamera.transform.position;
					playerEye.localEulerAngles = new Vector3(targetLookRot, 0f, 0f);
					playerEye.eulerAngles = new Vector3(playerEye.eulerAngles.x, targetYRot, playerEye.eulerAngles.z);
				}
				else if ((isPlayerDead || !isPlayerControlled) && setPositionOfDeadPlayer)
				{
					base.transform.position = playersManager.notSpawnedPosition.position;
				}
				if (isInGameOverAnimation > 0f && deadBody != null && deadBody.gameObject.activeSelf)
				{
					isInGameOverAnimation -= Time.deltaTime;
				}
				else if (!hasBegunSpectating)
				{
					if (deadBody != null)
					{
						Debug.Log(deadBody.gameObject.activeSelf);
					}
					isInGameOverAnimation = 0f;
					hasBegunSpectating = true;
				}
			}
			timeSincePlayerMoving += Time.deltaTime;
			timeSinceMakingLoudNoise += Time.deltaTime;
			if (!inSpecialInteractAnimation)
			{
				if (playingQuickSpecialAnimation)
				{
					specialAnimationWeight = 1f;
				}
				else
				{
					specialAnimationWeight = Mathf.Lerp(specialAnimationWeight, 0f, Time.deltaTime * 12f);
				}
				if (!localArmsMatchCamera)
				{
					localArmsTransform.position = playerModelArmsMetarig.position + playerModelArmsMetarig.forward * -0.445f;
					playerModelArmsMetarig.rotation = Quaternion.Lerp(playerModelArmsMetarig.rotation, localArmsRotationTarget.rotation, 15f * Time.deltaTime);
				}
			}
			else
			{
				if (!isClimbingLadder && !inShockingMinigame)
				{
					cameraUp = Mathf.Lerp(cameraUp, 0f, 5f * Time.deltaTime);
					gameplayCamera.transform.localEulerAngles = new Vector3(cameraUp, gameplayCamera.transform.localEulerAngles.y, gameplayCamera.transform.localEulerAngles.z);
				}
				specialAnimationWeight = Mathf.Lerp(specialAnimationWeight, 1f, Time.deltaTime * 20f);
				playerModelArmsMetarig.localEulerAngles = new Vector3(-90f, 0f, 0f);
			}
			interactRay = new Ray(base.transform.position + Vector3.up * 2.3f, base.transform.forward);
			if (doingUpperBodyEmote > 0f || (!twoHanded && Physics.Raycast(interactRay, out hit, 0.53f, walkableSurfacesNoPlayersMask, QueryTriggerInteraction.Ignore)))
			{
				doingUpperBodyEmote -= Time.deltaTime;
				handsOnWallWeight = Mathf.Lerp(handsOnWallWeight, 1f, 15f * Time.deltaTime);
			}
			else
			{
				handsOnWallWeight = Mathf.Lerp(handsOnWallWeight, 0f, 15f * Time.deltaTime);
			}
			playerBodyAnimator.SetLayerWeight(playerBodyAnimator.GetLayerIndex("UpperBodyEmotes"), handsOnWallWeight);
			if (performingEmote)
			{
				emoteLayerWeight = Mathf.Lerp(emoteLayerWeight, 1f, 10f * Time.deltaTime);
			}
			else
			{
				emoteLayerWeight = Mathf.Lerp(emoteLayerWeight, 0f, 10f * Time.deltaTime);
			}
			playerBodyAnimator.SetLayerWeight(playerBodyAnimator.GetLayerIndex("EmotesNoArms"), emoteLayerWeight);
			meshContainer.position = Vector3.Lerp(base.transform.position, base.transform.position - Vector3.up * 2.8f, StartOfRound.Instance.playerSinkingCurve.Evaluate(sinkingValue));
			if (isSinking && !inSpecialInteractAnimation && inAnimationWithEnemy == null)
			{
				sinkingValue = Mathf.Clamp(sinkingValue + Time.deltaTime * sinkingSpeedMultiplier, 0f, 1f);
			}
			else
			{
				sinkingValue = Mathf.Clamp(sinkingValue - Time.deltaTime * 0.75f, 0f, 1f);
			}
			if (sinkingValue > 0.73f || isUnderwater)
			{
				if (!wasUnderwaterLastFrame)
				{
					wasUnderwaterLastFrame = true;
					if (!base.IsOwner)
					{
						waterBubblesAudio.Play();
					}
				}
				voiceMuffledByEnemy = true;
				if (!base.IsOwner)
				{
					statusEffectAudio.volume = Mathf.Lerp(statusEffectAudio.volume, 0f, 4f * Time.deltaTime);
					if (currentVoiceChatIngameSettings != null)
					{
						OccludeAudio component2 = currentVoiceChatIngameSettings.voiceAudio.GetComponent<OccludeAudio>();
						component2.overridingLowPass = true;
						component2.lowPassOverride = 600f;
						waterBubblesAudio.volume = Mathf.Clamp(currentVoiceChatIngameSettings._playerState.Amplitude * 120f, 0f, 1f);
					}
					else
					{
						StartOfRound.Instance.RefreshPlayerVoicePlaybackObjects();
					}
				}
				else if (sinkingValue > 0.73f)
				{
					HUDManager.Instance.sinkingCoveredFace = true;
				}
			}
			else if (base.IsOwner)
			{
				HUDManager.Instance.sinkingCoveredFace = false;
			}
			else if (wasUnderwaterLastFrame)
			{
				waterBubblesAudio.Stop();
				if (currentVoiceChatIngameSettings != null)
				{
					wasUnderwaterLastFrame = false;
					currentVoiceChatIngameSettings.voiceAudio.GetComponent<OccludeAudio>().overridingLowPass = false;
					voiceMuffledByEnemy = false;
				}
				else
				{
					StartOfRound.Instance.RefreshPlayerVoicePlaybackObjects();
					StartOfRound.Instance.UpdatePlayerVoiceEffects();
				}
			}
			else
			{
				statusEffectAudio.volume = Mathf.Lerp(statusEffectAudio.volume, 1f, 4f * Time.deltaTime);
			}
			if (activeAudioReverbFilter == null)
			{
				activeAudioReverbFilter = activeAudioListener.GetComponent<AudioReverbFilter>();
				activeAudioReverbFilter.enabled = true;
			}
			if (reverbPreset != null && GameNetworkManager.Instance != null && GameNetworkManager.Instance.localPlayerController != null && ((GameNetworkManager.Instance.localPlayerController == this && (!isPlayerDead || StartOfRound.Instance.overrideSpectateCamera)) || (GameNetworkManager.Instance.localPlayerController.spectatedPlayerScript == this && !StartOfRound.Instance.overrideSpectateCamera)))
			{
				activeAudioReverbFilter.dryLevel = Mathf.Lerp(activeAudioReverbFilter.dryLevel, reverbPreset.dryLevel, 15f * Time.deltaTime);
				activeAudioReverbFilter.roomLF = Mathf.Lerp(activeAudioReverbFilter.roomLF, reverbPreset.lowFreq, 15f * Time.deltaTime);
				activeAudioReverbFilter.roomLF = Mathf.Lerp(activeAudioReverbFilter.roomHF, reverbPreset.highFreq, 15f * Time.deltaTime);
				activeAudioReverbFilter.decayTime = Mathf.Lerp(activeAudioReverbFilter.decayTime, reverbPreset.decayTime, 15f * Time.deltaTime);
				activeAudioReverbFilter.room = Mathf.Lerp(activeAudioReverbFilter.room, reverbPreset.room, 15f * Time.deltaTime);
			}
			if (isHoldingObject || isGrabbingObjectAnimation || inShockingMinigame)
			{
				upperBodyAnimationsWeight = Mathf.Lerp(upperBodyAnimationsWeight, 1f, 25f * Time.deltaTime);
				playerBodyAnimator.SetLayerWeight(playerBodyAnimator.GetLayerIndex("HoldingItemsRightHand"), upperBodyAnimationsWeight);
				if (twoHandedAnimation || inShockingMinigame)
				{
					playerBodyAnimator.SetLayerWeight(playerBodyAnimator.GetLayerIndex("HoldingItemsBothHands"), upperBodyAnimationsWeight);
				}
				else
				{
					playerBodyAnimator.SetLayerWeight(playerBodyAnimator.GetLayerIndex("HoldingItemsBothHands"), Mathf.Abs(upperBodyAnimationsWeight - 1f));
				}
			}
			else
			{
				upperBodyAnimationsWeight = Mathf.Lerp(upperBodyAnimationsWeight, 0f, 25f * Time.deltaTime);
				playerBodyAnimator.SetLayerWeight(playerBodyAnimator.GetLayerIndex("HoldingItemsRightHand"), upperBodyAnimationsWeight);
				playerBodyAnimator.SetLayerWeight(playerBodyAnimator.GetLayerIndex("HoldingItemsBothHands"), upperBodyAnimationsWeight);
			}
			playerBodyAnimator.SetLayerWeight(playerBodyAnimator.GetLayerIndex("SpecialAnimations"), specialAnimationWeight);
			if (inSpecialInteractAnimation && !inShockingMinigame)
			{
				cameraLookRig1.weight = Mathf.Lerp(cameraLookRig1.weight, 0f, Time.deltaTime * 25f);
				cameraLookRig2.weight = Mathf.Lerp(cameraLookRig1.weight, 0f, Time.deltaTime * 25f);
			}
			else
			{
				cameraLookRig1.weight = 0.45f;
				cameraLookRig2.weight = 1f;
			}
			if (isExhausted)
			{
				exhaustionEffectLerp = Mathf.Lerp(exhaustionEffectLerp, 1f, 10f * Time.deltaTime);
			}
			else
			{
				exhaustionEffectLerp = Mathf.Lerp(exhaustionEffectLerp, 0f, 10f * Time.deltaTime);
			}
			playerBodyAnimator.SetFloat("tiredAmount", exhaustionEffectLerp);
			if (isPlayerDead)
			{
				drunkness = 0f;
				drunknessInertia = 0f;
			}
			else
			{
				drunkness = Mathf.Clamp(drunkness + Time.deltaTime / 12f * drunknessSpeed * drunknessInertia, 0f, 1f);
				if (!increasingDrunknessThisFrame)
				{
					if (drunkness > 0f)
					{
						drunknessInertia = Mathf.Clamp(drunknessInertia - Time.deltaTime / 3f * drunknessSpeed / Mathf.Clamp(Mathf.Abs(drunknessInertia), 0.2f, 1f), -2.5f, 2.5f);
					}
					else
					{
						drunknessInertia = 0f;
					}
				}
				else
				{
					increasingDrunknessThisFrame = false;
				}
				float num12 = StartOfRound.Instance.drunknessSideEffect.Evaluate(drunkness);
				if (num12 > 0.15f)
				{
					SoundManager.Instance.playerVoicePitchTargets[playerClientId] = 1f + num12;
				}
				else
				{
					SoundManager.Instance.playerVoicePitchTargets[playerClientId] = 1f;
				}
			}
			smoothLookMultiplier = 25f * Mathf.Clamp(Mathf.Abs(drunkness - 1.5f), 0.15f, 1f);
			if (bleedingHeavily && bloodDropTimer >= 0f)
			{
				bloodDropTimer -= Time.deltaTime;
			}
			if (Physics.Raycast(lineOfSightCube.position, lineOfSightCube.forward, out hit, 10f, playersManager.collidersAndRoomMask, QueryTriggerInteraction.Ignore))
			{
				lineOfSightCube.localScale = new Vector3(1.5f, 1.5f, hit.distance);
			}
			else
			{
				lineOfSightCube.localScale = new Vector3(1.5f, 1.5f, 10f);
			}
			SetPlayerSanityLevel();
		}

		private void SetFaceUnderwaterFilters()
		{
			if (isPlayerDead)
			{
				return;
			}
			if (isUnderwater && underwaterCollider != null && underwaterCollider.bounds.Contains(gameplayCamera.transform.position))
			{
				HUDManager.Instance.setUnderwaterFilter = true;
				statusEffectAudio.volume = Mathf.Lerp(statusEffectAudio.volume, 0f, 4f * Time.deltaTime);
				StartOfRound.Instance.drowningTimer -= Time.deltaTime / 10f;
				if (StartOfRound.Instance.drowningTimer < 0f)
				{
					StartOfRound.Instance.drowningTimer = 1f;
					KillPlayer(Vector3.zero, spawnBody: true, CauseOfDeath.Drowning);
				}
				else if (StartOfRound.Instance.drowningTimer <= 0.3f)
				{
					if (!StartOfRound.Instance.playedDrowningSFX)
					{
						StartOfRound.Instance.playedDrowningSFX = true;
						HUDManager.Instance.UIAudio.PlayOneShot(StartOfRound.Instance.HUDSystemAlertSFX);
					}
					HUDManager.Instance.DisplayStatusEffect("Oxygen critically low!");
				}
			}
			else
			{
				statusEffectAudio.volume = Mathf.Lerp(statusEffectAudio.volume, 1f, 4f * Time.deltaTime);
				StartOfRound.Instance.playedDrowningSFX = false;
				StartOfRound.Instance.drowningTimer = Mathf.Clamp(StartOfRound.Instance.drowningTimer + Time.deltaTime, 0.1f, 1f);
				HUDManager.Instance.setUnderwaterFilter = false;
			}
			if (syncUnderwaterInterval <= 0f)
			{
				if (HUDManager.Instance.setUnderwaterFilter)
				{
					if (!isFaceUnderwaterOnServer)
					{
						isFaceUnderwaterOnServer = true;
						SetFaceUnderwaterServerRpc();
					}
				}
				else if (isFaceUnderwaterOnServer)
				{
					isFaceUnderwaterOnServer = false;
					SetFaceOutOfWaterServerRpc();
				}
			}
			else
			{
				syncUnderwaterInterval = 0.5f;
			}
		}

		[ServerRpc]
		private void SetFaceUnderwaterServerRpc()
		{
			SetFaceUnderwaterClientRpc();
		}

		[ClientRpc]
		private void SetFaceUnderwaterClientRpc()
		{
			if (!base.IsOwner)
			{
				isUnderwater = true;
			}
		}

		[ServerRpc]
		private void SetFaceOutOfWaterServerRpc()
		{
			SetFaceOutOfWaterClientRpc();
		}

		[ClientRpc]
		private void SetFaceOutOfWaterClientRpc()
		{
			if (!base.IsOwner)
			{
				isUnderwater = false;
			}
		}

		public void IncreaseFearLevelOverTime(float amountMultiplier = 1f, float cap = 1f)
		{
			playersManager.fearLevelIncreasing = true;
			if (!(playersManager.fearLevel > cap))
			{
				playersManager.fearLevel += Time.deltaTime * amountMultiplier;
			}
		}

		public void JumpToFearLevel(float targetFearLevel, bool onlyGoUp = true)
		{
			if (!onlyGoUp || !(targetFearLevel - playersManager.fearLevel < 0.05f))
			{
				playersManager.fearLevel = targetFearLevel;
				playersManager.fearLevelIncreasing = true;
			}
		}

		private void SetPlayerSanityLevel()
		{
			if (StartOfRound.Instance.inShipPhase || !TimeOfDay.Instance.currentDayTimeStarted)
			{
				insanityLevel = 0f;
				return;
			}
			if (!NearOtherPlayers(this, 17f) && !PlayerIsHearingOthersThroughWalkieTalkie(this))
			{
				if (isInsideFactory)
				{
					insanitySpeedMultiplier = 0.8f;
				}
				else if (isInHangarShipRoom)
				{
					insanitySpeedMultiplier = 0.2f;
				}
				else if (StartOfRound.Instance.connectedPlayersAmount == 0)
				{
					insanitySpeedMultiplier = -2f;
				}
				else if (TimeOfDay.Instance.dayMode >= DayMode.Sundown)
				{
					insanitySpeedMultiplier = 0.5f;
				}
				else
				{
					insanitySpeedMultiplier = 0.3f;
				}
				isPlayerAlone = true;
			}
			else
			{
				insanitySpeedMultiplier = -3f;
				isPlayerAlone = false;
			}
			if (insanitySpeedMultiplier < 0f)
			{
				insanityLevel = Mathf.MoveTowards(insanityLevel, 0f, Time.deltaTime * (0f - insanitySpeedMultiplier));
				return;
			}
			if (insanityLevel > maxInsanityLevel)
			{
				insanityLevel = Mathf.MoveTowards(insanityLevel, maxInsanityLevel, Time.deltaTime * 2f);
				return;
			}
			if (StartOfRound.Instance.connectedPlayersAmount == 0)
			{
				insanitySpeedMultiplier /= 1.6f;
			}
			insanityLevel = Mathf.MoveTowards(insanityLevel, maxInsanityLevel, Time.deltaTime * insanitySpeedMultiplier);
		}

		private void SetNightVisionEnabled(bool isNotLocalClient)
		{
			nightVision.enabled = false;
			if (!(GameNetworkManager.Instance == null) && !(GameNetworkManager.Instance.localPlayerController == null) && (!isNotLocalClient || GameNetworkManager.Instance.localPlayerController.spectatedPlayerScript == this) && isInsideFactory)
			{
				nightVision.enabled = true;
			}
		}

		public void ClickHoldInteraction()
		{
			if (!(isHoldingInteract = IngamePlayerSettings.Instance.playerInput.actions.FindAction("Interact").IsPressed()))
			{
				StopHoldInteractionOnTrigger();
			}
			else if (hoveringOverTrigger == null || !hoveringOverTrigger.interactable)
			{
				StopHoldInteractionOnTrigger();
			}
			else if (hoveringOverTrigger == null || !hoveringOverTrigger.gameObject.activeInHierarchy || !hoveringOverTrigger.holdInteraction || hoveringOverTrigger.currentCooldownValue > 0f || (isHoldingObject && !hoveringOverTrigger.oneHandedItemAllowed) || (twoHanded && !hoveringOverTrigger.twoHandedItemAllowed))
			{
				StopHoldInteractionOnTrigger();
			}
			else if (isGrabbingObjectAnimation || isTypingChat || inSpecialInteractAnimation || throwingObject)
			{
				StopHoldInteractionOnTrigger();
			}
			else if (!HUDManager.Instance.HoldInteractionFill(hoveringOverTrigger.timeToHold, hoveringOverTrigger.timeToHoldSpeedMultiplier))
			{
				hoveringOverTrigger.HoldInteractNotFilled();
			}
			else
			{
				hoveringOverTrigger.Interact(thisPlayerBody);
			}
		}

		private void StopHoldInteractionOnTrigger()
		{
			HUDManager.Instance.holdFillAmount = 0f;
			if (previousHoveringOverTrigger != null)
			{
				previousHoveringOverTrigger.StopInteraction();
			}
			if (hoveringOverTrigger != null)
			{
				hoveringOverTrigger.StopInteraction();
			}
		}

		public void CancelSpecialTriggerAnimations()
		{
			Terminal terminal = UnityEngine.Object.FindObjectOfType<Terminal>();
			if (terminal.terminalInUse)
			{
				terminal.QuitTerminal();
			}
			else if (currentTriggerInAnimationWith != null)
			{
				currentTriggerInAnimationWith.StopSpecialAnimation();
			}
		}

		public void TeleportPlayer(Vector3 pos, bool withRotation = false, float rot = 0f, bool allowInteractTrigger = false, bool enableController = true)
		{
			if (base.IsOwner && !allowInteractTrigger)
			{
				CancelSpecialTriggerAnimations();
			}
			else if (!allowInteractTrigger && currentTriggerInAnimationWith != null)
			{
				currentTriggerInAnimationWith.onCancelAnimation.Invoke(this);
				currentTriggerInAnimationWith.SetInteractTriggerNotInAnimation();
			}
			if ((bool)inAnimationWithEnemy)
			{
				inAnimationWithEnemy.CancelSpecialAnimationWithPlayer();
			}
			StartOfRound.Instance.playerTeleportedEvent.Invoke(this);
			if (withRotation)
			{
				targetYRot = rot;
				base.transform.eulerAngles = new Vector3(0f, targetYRot, 0f);
			}
			serverPlayerPosition = pos;
			thisController.enabled = false;
			base.transform.position = pos;
			if (enableController)
			{
				thisController.enabled = true;
			}
			teleportingThisFrame = true;
			teleportedLastFrame = true;
			timeSinceTakingGravityDamage = 1f;
			averageVelocity = 0f;
			if (!isUnderwater && !isSinking)
			{
				return;
			}
			QuicksandTrigger[] array = UnityEngine.Object.FindObjectsByType<QuicksandTrigger>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i].sinkingLocalPlayer)
				{
					array[i].OnExit(base.gameObject.GetComponent<Collider>());
					break;
				}
			}
		}

		public void KillPlayer(Vector3 bodyVelocity, bool spawnBody = true, CauseOfDeath causeOfDeath = CauseOfDeath.Unknown, int deathAnimation = 0)
		{
			if (base.IsOwner && !isPlayerDead && AllowPlayerDeath())
			{
				isPlayerDead = true;
				isPlayerControlled = false;
				thisPlayerModelArms.enabled = false;
				localVisor.position = playersManager.notSpawnedPosition.position;
				DisablePlayerModel(base.gameObject);
				isInsideFactory = false;
				IsInspectingItem = false;
				inTerminalMenu = false;
				twoHanded = false;
				carryWeight = 1f;
				fallValue = 0f;
				fallValueUncapped = 0f;
				takingFallDamage = false;
				isSinking = false;
				isUnderwater = false;
				StartOfRound.Instance.drowningTimer = 1f;
				HUDManager.Instance.setUnderwaterFilter = false;
				wasUnderwaterLastFrame = false;
				sourcesCausingSinking = 0;
				sinkingValue = 0f;
				hinderedMultiplier = 1f;
				isMovementHindered = 0;
				inAnimationWithEnemy = null;
				UnityEngine.Object.FindObjectOfType<Terminal>().terminalInUse = false;
				ChangeAudioListenerToObject(playersManager.spectateCamera.gameObject);
				SoundManager.Instance.SetDiageticMixerSnapshot();
				HUDManager.Instance.SetNearDepthOfFieldEnabled(enabled: true);
				HUDManager.Instance.HUDAnimator.SetBool("biohazardDamage", value: false);
				Debug.Log("Running kill player function for LOCAL client, player object: " + base.gameObject.name);
				HUDManager.Instance.gameOverAnimator.SetTrigger("gameOver");
				HUDManager.Instance.HideHUD(hide: true);
				StopHoldInteractionOnTrigger();
				KillPlayerServerRpc((int)playerClientId, spawnBody, bodyVelocity, (int)causeOfDeath, deathAnimation);
				if (spawnBody)
				{
					SpawnDeadBody((int)playerClientId, bodyVelocity, (int)causeOfDeath, this, deathAnimation);
				}
				StartOfRound.Instance.SwitchCamera(StartOfRound.Instance.spectateCamera);
				isInGameOverAnimation = 1.5f;
				cursorTip.text = "";
				cursorIcon.enabled = false;
				DropAllHeldItems(spawnBody);
				DisableJetpackControlsLocally();
			}
		}

		[ServerRpc]
		private void KillPlayerServerRpc(int playerId, bool spawnBody, Vector3 bodyVelocity, int causeOfDeath, int deathAnimation)
		{
			DestroyItemInSlotClientRpc(itemSlot);
		}

		[ClientRpc]
		public void DestroyItemInSlotClientRpc(int itemSlot)
		{
			if (!base.IsOwner)
			{
				DestroyItemInSlot(itemSlot);
			}
		}

		public void DestroyItemInSlot(int itemSlot)
		{
			if (GameNetworkManager.Instance.localPlayerController == null || NetworkManager.Singleton == null || NetworkManager.Singleton.ShutdownInProgress)
			{
				return;
			}
			Debug.Log($"Destroying item in slot {itemSlot}; {currentItemSlot}; is currentlyheldobjectserver null: {currentlyHeldObjectServer == null}");
			if (currentlyHeldObjectServer != null)
			{
				Debug.Log("currentlyHeldObjectServer: " + currentlyHeldObjectServer.itemProperties.itemName);
			}
			GrabbableObject grabbableObject = ItemSlots[itemSlot];
			if (isHoldingObject)
			{
				if (currentItemSlot == itemSlot)
				{
					carryWeight -= Mathf.Clamp(currentlyHeldObjectServer.itemProperties.weight - 1f, 0f, 10f);
					isHoldingObject = false;
					twoHanded = false;
					if (base.IsOwner)
					{
						playerBodyAnimator.SetBool("cancelHolding", value: true);
						playerBodyAnimator.SetTrigger("Throw");
						HUDManager.Instance.holdingTwoHandedItem.enabled = false;
						HUDManager.Instance.ClearControlTips();
						activatingItem = false;
					}
				}
				HUDManager.Instance.itemSlotIcons[itemSlot].enabled = false;
				if (currentlyHeldObjectServer != null && currentlyHeldObjectServer == ItemSlots[itemSlot])
				{
					if (base.IsOwner)
					{
						SetSpecialGrabAnimationBool(setTrue: false, currentlyHeldObjectServer);
						currentlyHeldObjectServer.DiscardItemOnClient();
					}
					currentlyHeldObjectServer = null;
				}
			}
			ItemSlots[itemSlot] = null;
			if (base.IsServer)
			{
				grabbableObject.NetworkObject.Despawn();
			}
		}

		public void DropAllHeldItems(bool itemsFall = true, bool disconnecting = false)
		{
			for (int i = 0; i < ItemSlots.Length; i++)
			{
				GrabbableObject grabbableObject = ItemSlots[i];
				if (!(grabbableObject != null))
				{
					continue;
				}
				if (itemsFall)
				{
					grabbableObject.parentObject = null;
					grabbableObject.heldByPlayerOnServer = false;
					if (isInElevator)
					{
						grabbableObject.transform.SetParent(playersManager.elevatorTransform, worldPositionStays: true);
					}
					else
					{
						grabbableObject.transform.SetParent(playersManager.propsContainer, worldPositionStays: true);
					}
					SetItemInElevator(isInHangarShipRoom, isInElevator, grabbableObject);
					grabbableObject.EnablePhysics(enable: true);
					grabbableObject.EnableItemMeshes(enable: true);
					grabbableObject.transform.localScale = grabbableObject.originalScale;
					grabbableObject.isHeld = false;
					grabbableObject.isPocketed = false;
					grabbableObject.startFallingPosition = grabbableObject.transform.parent.InverseTransformPoint(grabbableObject.transform.position);
					grabbableObject.FallToGround(randomizePosition: true);
					grabbableObject.fallTime = UnityEngine.Random.Range(-0.3f, 0.05f);
					if (base.IsOwner)
					{
						grabbableObject.DiscardItemOnClient();
					}
					else if (!grabbableObject.itemProperties.syncDiscardFunction)
					{
						grabbableObject.playerHeldBy = null;
					}
				}
				if (base.IsOwner && !disconnecting)
				{
					HUDManager.Instance.holdingTwoHandedItem.enabled = false;
					HUDManager.Instance.itemSlotIcons[i].enabled = false;
					HUDManager.Instance.ClearControlTips();
					activatingItem = false;
				}
				ItemSlots[i] = null;
			}
			if (isHoldingObject)
			{
				isHoldingObject = false;
				if (currentlyHeldObjectServer != null)
				{
					SetSpecialGrabAnimationBool(setTrue: false, currentlyHeldObjectServer);
				}
				playerBodyAnimator.SetBool("cancelHolding", value: true);
				playerBodyAnimator.SetTrigger("Throw");
			}
			activatingItem = false;
			twoHanded = false;
			carryWeight = 1f;
			currentlyHeldObjectServer = null;
		}

		public void DropAllHeldItemsAndSync()
		{
			DropAllHeldItems();
			DropAllHeldItemsServerRpc();
		}

		[ServerRpc(RequireOwnership = false)]
		public void DropAllHeldItemsServerRpc()
		{
			DropAllHeldItemsClientRpc();
		}

		[ClientRpc]
		public void DropAllHeldItemsClientRpc()
		{
			DropAllHeldItems();
		}

		private bool NearOtherPlayers(PlayerControllerB playerScript = null, float checkRadius = 10f)
		{
			if (playerScript == null)
			{
				playerScript = this;
			}
			base.gameObject.layer = 0;
			bool result = Physics.CheckSphere(playerScript.transform.position, checkRadius, 8, QueryTriggerInteraction.Ignore);
			base.gameObject.layer = 3;
			return result;
		}

		private bool PlayerIsHearingOthersThroughWalkieTalkie(PlayerControllerB playerScript = null)
		{
			if (playerScript == null)
			{
				playerScript = this;
			}
			if (!playerScript.holdingWalkieTalkie)
			{
				return false;
			}
			for (int i = 0; i < WalkieTalkie.allWalkieTalkies.Count; i++)
			{
				if (WalkieTalkie.allWalkieTalkies[i].clientIsHoldingAndSpeakingIntoThis && WalkieTalkie.allWalkieTalkies[i] != playerScript.currentlyHeldObjectServer as WalkieTalkie)
				{
					return true;
				}
			}
			return false;
		}

		public void DisablePlayerModel(GameObject playerObject, bool enable = false, bool disableLocalArms = false)
		{
			SkinnedMeshRenderer[] componentsInChildren = playerObject.GetComponentsInChildren<SkinnedMeshRenderer>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].enabled = enable;
			}
			if (disableLocalArms)
			{
				thisPlayerModelArms.enabled = false;
			}
		}

		public void SyncBodyPositionWithClients()
		{
			if (deadBody != null)
			{
				SyncBodyPositionClientRpc(deadBody.transform.position);
			}
		}

		[ClientRpc]
		public void SyncBodyPositionClientRpc(Vector3 newBodyPosition)
		{
			if (!(Vector3.Distance(deadBody.transform.position, newBodyPosition) < 1.5f))
			{
				StartCoroutine(WaitUntilPlayerHasLeftBodyToTeleport(newBodyPosition));
			}
		}

		private IEnumerator WaitUntilPlayerHasLeftBodyToTeleport(Vector3 newBodyPosition)
		{
			yield return new WaitUntil(() => deadBody == null || !Physics.CheckSphere(deadBody.transform.position, 8f, playersManager.playersMask));
			if (!(deadBody == null))
			{
				deadBody.SetRagdollPositionSafely(newBodyPosition);
			}
		}

		private void LateUpdate()
		{
			if (isFirstFrameLateUpdate)
			{
				isFirstFrameLateUpdate = false;
				previousElevatorPosition = playersManager.elevatorTransform.position;
			}
			else if (base.IsOwner && isPlayerControlled && (!base.IsServer || isHostPlayerObject))
			{
				if (isInElevator)
				{
					if (!wasInElevatorLastFrame)
					{
						wasInElevatorLastFrame = true;
						base.transform.SetParent(playersManager.elevatorTransform);
					}
				}
				else if (wasInElevatorLastFrame)
				{
					wasInElevatorLastFrame = false;
					base.transform.SetParent(playersManager.playersContainer);
				}
			}
			previousElevatorPosition = playersManager.elevatorTransform.position;
			if (!isTestingPlayer)
			{
				if (NetworkManager.Singleton == null)
				{
					return;
				}
				if (!base.IsOwner && usernameAlpha.alpha >= 0f && GameNetworkManager.Instance.localPlayerController != null)
				{
					usernameAlpha.alpha -= Time.deltaTime;
					usernameBillboard.LookAt(GameNetworkManager.Instance.localPlayerController.localVisorTargetPoint);
				}
				else if (usernameCanvas.gameObject.activeSelf)
				{
					usernameCanvas.gameObject.SetActive(value: false);
				}
			}
			if (base.IsOwner && (!base.IsServer || isHostPlayerObject))
			{
				PlayerLookInput();
				if (isPlayerControlled && !isPlayerDead)
				{
					if (GameNetworkManager.Instance != null)
					{
						float num = 0.14f;
						num = (inSpecialInteractAnimation ? 0.06f : ((!NearOtherPlayers(this)) ? 0.24f : 0.1f));
						if ((oldPlayerPosition - base.transform.localPosition).sqrMagnitude > num || updatePositionForNewlyJoinedClient)
						{
							updatePositionForNewlyJoinedClient = false;
							if (!playersManager.newGameIsLoading)
							{
								UpdatePlayerPositionServerRpc(thisPlayerBody.localPosition, isInElevator, isInHangarShipRoom, isExhausted, thisController.isGrounded);
								oldPlayerPosition = base.transform.localPosition;
							}
						}
						if (currentlyHeldObjectServer != null && isHoldingObject && grabbedObjectValidated)
						{
							currentlyHeldObjectServer.transform.localPosition = currentlyHeldObjectServer.itemProperties.positionOffset;
							currentlyHeldObjectServer.transform.localEulerAngles = currentlyHeldObjectServer.itemProperties.rotationOffset;
						}
					}
					localVisor.position = localVisorTargetPoint.position;
					localVisor.rotation = Quaternion.Lerp(localVisor.rotation, localVisorTargetPoint.rotation, 53f * Mathf.Clamp(Time.deltaTime, 0.0167f, 20f));
					float num2 = 1f;
					if (drunkness > 0.02f)
					{
						num2 *= Mathf.Abs(StartOfRound.Instance.drunknessSpeedEffect.Evaluate(drunkness) - 1.25f);
					}
					if (isSprinting)
					{
						sprintMeter = Mathf.Clamp(sprintMeter - Time.deltaTime / sprintTime * carryWeight * num2, 0f, 1f);
					}
					else if (isMovementHindered > 0)
					{
						if (isWalking)
						{
							sprintMeter = Mathf.Clamp(sprintMeter - Time.deltaTime / sprintTime * num2 * 0.5f, 0f, 1f);
						}
					}
					else
					{
						if (!isWalking)
						{
							sprintMeter = Mathf.Clamp(sprintMeter + Time.deltaTime / (sprintTime + 4f) * num2, 0f, 1f);
						}
						else
						{
							sprintMeter = Mathf.Clamp(sprintMeter + Time.deltaTime / (sprintTime + 9f) * num2, 0f, 1f);
						}
						if (isExhausted && sprintMeter > 0.2f)
						{
							isExhausted = false;
						}
					}
					sprintMeterUI.fillAmount = sprintMeter;
					float num3;
					if (isHoldingObject && currentlyHeldObjectServer != null && currentlyHeldObjectServer.itemProperties.requiresBattery)
					{
						HUDManager.Instance.batteryMeter.fillAmount = currentlyHeldObjectServer.insertedBattery.charge / 1.3f;
						HUDManager.Instance.batteryMeter.gameObject.SetActive(value: true);
						HUDManager.Instance.batteryIcon.enabled = true;
						num3 = currentlyHeldObjectServer.insertedBattery.charge / 1.3f;
					}
					else if (helmetLight.enabled)
					{
						HUDManager.Instance.batteryMeter.fillAmount = pocketedFlashlight.insertedBattery.charge / 1.3f;
						HUDManager.Instance.batteryMeter.gameObject.SetActive(value: true);
						HUDManager.Instance.batteryIcon.enabled = true;
						num3 = pocketedFlashlight.insertedBattery.charge / 1.3f;
					}
					else
					{
						HUDManager.Instance.batteryMeter.gameObject.SetActive(value: false);
						HUDManager.Instance.batteryIcon.enabled = false;
						num3 = 1f;
					}
					HUDManager.Instance.batteryBlinkUI.SetBool("blink", num3 < 0.2f && num3 > 0f);
					timeSinceSwitchingSlots += Time.deltaTime;
					if (limpMultiplier > 0f)
					{
						limpMultiplier -= Time.deltaTime / 2f;
					}
					if (health < 20)
					{
						if (healthRegenerateTimer <= 0f)
						{
							healthRegenerateTimer = 1f;
							health++;
							if (health >= 20)
							{
								MakeCriticallyInjured(enable: false);
							}
							HUDManager.Instance.UpdateHealthUI(health, hurtPlayer: false);
						}
						else
						{
							healthRegenerateTimer -= Time.deltaTime;
						}
					}
					SetHoverTipAndCurrentInteractTrigger();
				}
			}
			if (!inSpecialInteractAnimation && localArmsMatchCamera)
			{
				localArmsTransform.position = cameraContainerTransform.transform.position + gameplayCamera.transform.up * -0.5f;
				playerModelArmsMetarig.rotation = localArmsRotationTarget.rotation;
			}
			if (playersManager.overrideSpectateCamera || !base.IsOwner || !isPlayerDead || (base.IsServer && !isHostPlayerObject))
			{
				return;
			}
			if (isInGameOverAnimation > 0f && deadBody != null)
			{
				spectateCameraPivot.position = deadBody.bodyParts[0].position;
				RaycastSpectateCameraAroundPivot();
			}
			else if (spectatedPlayerScript != null)
			{
				if (spectatedPlayerScript.isPlayerDead)
				{
					if (StartOfRound.Instance.allPlayersDead)
					{
						StartOfRound.Instance.SetSpectateCameraToGameOverMode(enableGameOver: true);
					}
					if (!(spectatedPlayerDeadTimer >= 1.5f))
					{
						spectatedPlayerDeadTimer += Time.deltaTime;
						if (spectatedPlayerScript.deadBody != null)
						{
							spectateCameraPivot.position = spectatedPlayerScript.deadBody.bodyParts[0].position;
							RaycastSpectateCameraAroundPivot();
						}
						return;
					}
					spectatedPlayerDeadTimer = 0f;
					SpectateNextPlayer();
				}
				spectateCameraPivot.position = spectatedPlayerScript.lowerSpine.position + Vector3.up * 0.7f;
				RaycastSpectateCameraAroundPivot();
			}
			else if (StartOfRound.Instance.allPlayersDead)
			{
				StartOfRound.Instance.SetSpectateCameraToGameOverMode(enableGameOver: true);
				SetSpectatedPlayerEffects(allPlayersDead: true);
			}
			else
			{
				SpectateNextPlayer();
			}
		}

		private void RaycastSpectateCameraAroundPivot()
		{
			interactRay = new Ray(spectateCameraPivot.position, -spectateCameraPivot.forward);
			if (Physics.Raycast(interactRay, out hit, 1.4f, walkableSurfacesNoPlayersMask, QueryTriggerInteraction.Ignore))
			{
				playersManager.spectateCamera.transform.position = interactRay.GetPoint(hit.distance - 0.25f);
			}
			else
			{
				playersManager.spectateCamera.transform.position = interactRay.GetPoint(1.3f);
			}
			playersManager.spectateCamera.transform.LookAt(spectateCameraPivot);
		}

		private void SetHoverTipAndCurrentInteractTrigger()
		{
			if (!isGrabbingObjectAnimation)
			{
				interactRay = new Ray(gameplayCamera.transform.position, gameplayCamera.transform.forward);
				if (Physics.Raycast(interactRay, out hit, grabDistance, interactableObjectsMask) && hit.collider.gameObject.layer != 8)
				{
					string text = hit.collider.tag;
					if (!(text == "PhysicsProp"))
					{
						if (text == "InteractTrigger")
						{
							InteractTrigger component = hit.transform.gameObject.GetComponent<InteractTrigger>();
							if (component != previousHoveringOverTrigger && previousHoveringOverTrigger != null)
							{
								previousHoveringOverTrigger.isBeingHeldByPlayer = false;
							}
							if (!(component == null))
							{
								hoveringOverTrigger = component;
								if (!component.interactable)
								{
									cursorIcon.sprite = component.disabledHoverIcon;
									cursorIcon.enabled = component.disabledHoverIcon != null;
									cursorTip.text = component.disabledHoverTip;
								}
								else if (component.isPlayingSpecialAnimation)
								{
									cursorIcon.enabled = false;
									cursorTip.text = "";
								}
								else if (isHoldingInteract)
								{
									if (twoHanded)
									{
										cursorTip.text = "[Hands full]";
									}
									else if (!string.IsNullOrEmpty(component.holdTip))
									{
										cursorTip.text = component.holdTip;
									}
								}
								else
								{
									cursorIcon.enabled = true;
									cursorIcon.sprite = component.hoverIcon;
									cursorTip.text = component.hoverTip;
								}
							}
						}
					}
					else
					{
						if (FirstEmptyItemSlot() == -1)
						{
							cursorTip.text = "Inventory full!";
						}
						else
						{
							GrabbableObject component2 = hit.collider.gameObject.GetComponent<GrabbableObject>();
							if (!GameNetworkManager.Instance.gameHasStarted && !component2.itemProperties.canBeGrabbedBeforeGameStart && StartOfRound.Instance.testRoom == null)
							{
								cursorTip.text = "(Cannot hold until ship has landed)";
								goto IL_02ea;
							}
							if (component2 != null && !string.IsNullOrEmpty(component2.customGrabTooltip))
							{
								cursorTip.text = component2.customGrabTooltip;
							}
							else
							{
								cursorTip.text = "Grab : [E]";
							}
						}
						cursorIcon.enabled = true;
						cursorIcon.sprite = grabItemIcon;
					}
				}
				else
				{
					cursorIcon.enabled = false;
					cursorTip.text = "";
					if (hoveringOverTrigger != null)
					{
						previousHoveringOverTrigger = hoveringOverTrigger;
					}
					hoveringOverTrigger = null;
				}
				goto IL_02ea;
			}
			goto IL_0335;
			IL_0335:
			if (StartOfRound.Instance.localPlayerUsingController)
			{
				StringBuilder stringBuilder = new StringBuilder(cursorTip.text);
				stringBuilder.Replace("[E]", "[X]");
				stringBuilder.Replace("[LMB]", "[X]");
				stringBuilder.Replace("[RMB]", "[R-Trigger]");
				stringBuilder.Replace("[F]", "[R-Shoulder]");
				stringBuilder.Replace("[Z]", "[L-Shoulder]");
				cursorTip.text = stringBuilder.ToString();
			}
			else
			{
				cursorTip.text = cursorTip.text.Replace("[LMB]", "[E]");
			}
			return;
			IL_02ea:
			if (!isFreeCamera && Physics.Raycast(interactRay, out hit, 5f, playerMask))
			{
				PlayerControllerB component3 = hit.collider.gameObject.GetComponent<PlayerControllerB>();
				if (component3 != null)
				{
					component3.ShowNameBillboard();
				}
			}
			goto IL_0335;
		}

		public void ShowNameBillboard()
		{
			usernameAlpha.alpha = 1f;
			usernameCanvas.gameObject.SetActive(value: true);
		}

		public bool IsPlayerServer()
		{
			return base.IsServer;
		}

		private void SpectateNextPlayer()
		{
			int num = 0;
			if (spectatedPlayerScript != null)
			{
				num = (int)spectatedPlayerScript.playerClientId;
			}
			for (int i = 0; i < 4; i++)
			{
				num = (num + 1) % 4;
				if (!playersManager.allPlayerScripts[num].isPlayerDead && playersManager.allPlayerScripts[num].isPlayerControlled && playersManager.allPlayerScripts[num] != this)
				{
					spectatedPlayerScript = playersManager.allPlayerScripts[num];
					SetSpectatedPlayerEffects();
					return;
				}
			}
			if (deadBody != null && deadBody.gameObject.activeSelf)
			{
				spectateCameraPivot.position = deadBody.bodyParts[0].position;
				RaycastSpectateCameraAroundPivot();
			}
			StartOfRound.Instance.SetPlayerSafeInShip();
		}

		public void SetSpectatedPlayerEffects(bool allPlayersDead = false)
		{
			try
			{
				if (spectatedPlayerScript != null)
				{
					HUDManager.Instance.SetSpectatingTextToPlayer(spectatedPlayerScript);
				}
				else
				{
					HUDManager.Instance.spectatingPlayerText.text = "";
				}
				TimeOfDay timeOfDay = UnityEngine.Object.FindObjectOfType<TimeOfDay>();
				if (allPlayersDead)
				{
					for (int i = 0; i < timeOfDay.effects.Length; i++)
					{
						timeOfDay.effects[i].effectEnabled = false;
					}
					if (timeOfDay.sunDirect != null)
					{
						timeOfDay.sunDirect.enabled = true;
						timeOfDay.sunIndirect.GetComponent<HDAdditionalLightData>().lightDimmer = 1f;
					}
					AudioReverbPresets audioReverbPresets = UnityEngine.Object.FindObjectOfType<AudioReverbPresets>();
					if (audioReverbPresets != null && audioReverbPresets.audioPresets.Length > 3)
					{
						GameNetworkManager.Instance.localPlayerController.reverbPreset = audioReverbPresets.audioPresets[3].reverbPreset;
					}
					return;
				}
				AudioReverbTrigger audioReverbTrigger = spectatedPlayerScript.currentAudioTrigger;
				if (audioReverbTrigger == null)
				{
					TimeOfDay.Instance.SetInsideLightingDimness(doNotLerp: true, spectatedPlayerScript.isInsideFactory || spectatedPlayerScript.isInHangarShipRoom);
					return;
				}
				if (audioReverbTrigger.localFog != null)
				{
					if (audioReverbTrigger.toggleLocalFog)
					{
						audioReverbTrigger.localFog.parameters.meanFreePath = audioReverbTrigger.fogEnabledAmount;
					}
					else
					{
						audioReverbTrigger.localFog.parameters.meanFreePath = 200f;
					}
				}
				TimeOfDay.Instance.SetInsideLightingDimness(doNotLerp: true, audioReverbTrigger.setInsideAtmosphere && audioReverbTrigger.insideLighting);
				if (audioReverbTrigger.disableAllWeather || spectatedPlayerScript.isInsideFactory)
				{
					TimeOfDay.Instance.DisableAllWeather();
				}
				else
				{
					if (audioReverbTrigger.enableCurrentLevelWeather && TimeOfDay.Instance.currentLevelWeather != LevelWeatherType.None)
					{
						TimeOfDay.Instance.effects[(int)TimeOfDay.Instance.currentLevelWeather].effectEnabled = true;
					}
					if (audioReverbTrigger.weatherEffect != -1)
					{
						TimeOfDay.Instance.effects[audioReverbTrigger.weatherEffect].effectEnabled = audioReverbTrigger.effectEnabled;
					}
				}
				StartOfRound.Instance.UpdatePlayerVoiceEffects();
			}
			catch (Exception arg)
			{
				Debug.LogError($"Error caught in SpectatedPlayerEffects: {arg}");
			}
		}

		public void AddBloodToBody()
		{
			for (int i = 0; i < bodyBloodDecals.Length; i++)
			{
				if (!bodyBloodDecals[i].activeSelf)
				{
					bodyBloodDecals[i].SetActive(value: true);
					break;
				}
			}
		}

		public void RemoveBloodFromBody()
		{
			for (int i = 0; i < bodyBloodDecals.Length; i++)
			{
				bodyBloodDecals[i].SetActive(value: false);
			}
		}

		bool IHittable.Hit(int force, Vector3 hitDirection, PlayerControllerB playerWhoHit, bool playHitSFX = false)
		{
			if (!AllowPlayerDeath())
			{
				return false;
			}
			CentipedeAI[] array = UnityEngine.Object.FindObjectsByType<CentipedeAI>(FindObjectsSortMode.None);
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i].clingingToPlayer == this)
				{
					return false;
				}
			}
			if ((bool)inAnimationWithEnemy)
			{
				return false;
			}
			if (force <= 2)
			{
				DamagePlayerFromOtherClientServerRpc(10, hitDirection, (int)playerWhoHit.playerClientId);
			}
			else if (force <= 4)
			{
				DamagePlayerFromOtherClientServerRpc(30, hitDirection, (int)playerWhoHit.playerClientId);
			}
			else
			{
				DamagePlayerFromOtherClientServerRpc(100, hitDirection, (int)playerWhoHit.playerClientId);
			}
			return true;
		}

		[ServerRpc(RequireOwnership = false)]
		public void DamagePlayerFromOtherClientServerRpc(int damageAmount, Vector3 hitDirection, int playerWhoHit)
		{
			DamagePlayerFromOtherClientClientRpc(damageAmount, hitDirection, playerWhoHit, health - damageAmount);
		}

		[ClientRpc]
		public void DamagePlayerFromOtherClientClientRpc(int damageAmount, Vector3 hitDirection, int playerWhoHit, int newHealthAmount)
		{
			{
				ClientRpcParams clientRpcParams = default(ClientRpcParams);
				BytePacker.WriteValueBitPacked(bufferWriter, damageAmount);
				bufferWriter.WriteValueSafe(in hitDirection);
				BytePacker.WriteValueBitPacked(bufferWriter, playerWhoHit);
				BytePacker.WriteValueBitPacked(bufferWriter, newHealthAmount);
			}
			DamageOnOtherClients(damageAmount, newHealthAmount);
			if (base.IsOwner && isPlayerControlled)
			{
				CentipedeAI[] array = UnityEngine.Object.FindObjectsByType<CentipedeAI>(FindObjectsSortMode.None);
				for (int i = 0; i < array.Length; i++)
				{
					if (array[i].clingingToPlayer == this)
					{
						return;
					}
				}
				DamagePlayer(damageAmount, hasDamageSFX: true, callRPC: false, CauseOfDeath.Bludgeoning);
			}
			movementAudio.PlayOneShot(StartOfRound.Instance.hitPlayerSFX);
			if (health < 6)
			{
				DropBlood(hitDirection);
				bodyBloodDecals[0].SetActive(value: true);
				playersManager.allPlayerScripts[playerWhoHit].AddBloodToBody();
				playersManager.allPlayerScripts[playerWhoHit].movementAudio.PlayOneShot(StartOfRound.Instance.bloodGoreSFX);
				WalkieTalkie.TransmitOneShotAudio(playersManager.allPlayerScripts[playerWhoHit].movementAudio, StartOfRound.Instance.bloodGoreSFX);
			}
		}

		public bool HasLineOfSightToPosition(Vector3 pos, float width = 45f, int range = 60, float proximityAwareness = -1f)
		{
			float num = Vector3.Distance(base.transform.position, pos);
			if (num < (float)range && (Vector3.Angle(playerEye.transform.forward, pos - gameplayCamera.transform.position) < width || num < proximityAwareness) && !Physics.Linecast(playerEye.transform.position, pos, StartOfRound.Instance.collidersRoomDefaultAndFoliage, QueryTriggerInteraction.Ignore))
			{
				return true;
			}
			return false;
		}

		public float LineOfSightToPositionAngle(Vector3 pos, int range = 60, float proximityAwareness = -1f)
		{
			if (Vector3.Distance(base.transform.position, pos) < (float)range && !Physics.Linecast(playerEye.transform.position, pos, StartOfRound.Instance.collidersRoomDefaultAndFoliage, QueryTriggerInteraction.Ignore))
			{
				return Vector3.Angle(playerEye.transform.forward, pos - gameplayCamera.transform.position);
			}
			return -361f;
		}

		float IShockableWithGun.GetDifficultyMultiplier()
		{
			return 1f;
		}

		Vector3 IShockableWithGun.GetShockablePosition()
		{
			return base.transform.position;
		}

		Transform IShockableWithGun.GetShockableTransform()
		{
			return base.transform;
		}

		NetworkObject IShockableWithGun.GetNetworkObject()
		{
			return base.NetworkObject;
		}

		bool IShockableWithGun.CanBeShocked()
		{
			return !isPlayerDead;
		}

		void IShockableWithGun.StopShockingWithGun()
		{
			// No-op for player
		}

		void IShockableWithGun.ShockWithGun(PlayerControllerB shockedByPlayer)
		{
			// Player shocked by patcher tool
		}

		public void Crouch(bool crouch)
		{
			isCrouching = crouch;
			playerBodyAnimator.SetBool("Crouching", crouch);
		}

		private void Crouch_performed(UnityEngine.InputSystem.InputAction.CallbackContext context)
		{
			if (context.performed && !inSpecialInteractAnimation && thisPlayerMovementEnabled && !isTypingChat)
			{
				Crouch(!isCrouching);
			}
		}

		public void SetItemInElevator(bool droppedInShipRoom, bool droppedInElevator, GrabbableObject gObject)
		{
			if (gObject != null)
			{
				gObject.isInShipRoom = droppedInShipRoom;
				gObject.isInElevator = droppedInElevator;
			}
		}

		public void UpdateSpecialAnimationValue(bool specialAnimation, short clampValue = 0, float terpAmount = 0f, bool clampWithNormalizedTime = false)
		{
			playerBodyAnimator.SetBool("SA_inAnimation", specialAnimation);
			if (clampValue > 0)
			{
				playerBodyAnimator.SetFloat("SA_VarA", terpAmount);
			}
		}

	}
}
