using System;
using System.Collections;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.HighDefinition;

public class TimeOfDay : NetworkBehaviour
{
	[Header("Time")]
	public SelectableLevel currentLevel;

	public float globalTimeSpeedMultiplier = 1f;

	public float currentDayTime;

	public int hour;

	private int previousHour;

	public float normalizedTimeOfDay;

	[Space(5f)]
	public float globalTime;

	public float globalTimeAtEndOfDay;

	public bool movingGlobalTimeForward;

	[Space(10f)]
	private bool reachedQuota;

	public QuotaSettings quotaVariables;

	public int profitQuota;

	public int quotaFulfilled;

	public int timesFulfilledQuota;

	public float timeUntilDeadline;

	public int daysUntilDeadline;

	public int hoursUntilDeadline;

	[Space(5f)]
	public float lengthOfHours = 100f;

	public int numberOfHours = 7;

	public float totalTime;

	public const int startingGlobalTime = 100;

	[Space(3f)]
	public float shipLeaveAutomaticallyTime = 0.996f;

	[Space(5f)]
	public bool currentDayTimeStarted;

	private bool timeStartedThisFrame = true;

	public StartOfRound playersManager;

	public Animator sunAnimator;

	public Light sunIndirect;

	public Light sunDirect;

	public bool insideLighting = true;

	public DayMode dayMode;

	private DayMode dayModeLastTimePlayerWasOutside;

	public AudioClip[] timeOfDayCues;

	public AudioSource TimeOfDayMusic;

	private HDAdditionalLightData indirectLightData;

	[Header("Weather")]
	public WeatherEffect[] effects;

	public LevelWeatherType currentLevelWeather = LevelWeatherType.None;

	public float currentWeatherVariable;

	public float currentWeatherVariable2;

	[Space(4f)]
	public CompanyMood currentCompanyMood;

	public CompanyMood[] CommonCompanyMoods;

	[Space(4f)]
	private float changeHUDTimeInterval;

	private float nextTimeSync;

	public bool shipLeavingAlertCalled;

	public DialogueSegment[] shipLeavingSoonDialogue;

	public DialogueSegment[] shipLeavingEarlyDialogue;

	private bool shipLeavingOnMidnight;

	private bool shipFullCapacityAtMidnightMessage;

	private Coroutine playDelayedMusicCoroutine;

	public int votesForShipToLeaveEarly;

	public bool votedShipToLeaveEarlyThisRound;

	public UnityEvent onTimeSync = new UnityEvent();

	public static TimeOfDay Instance { get; private set; }

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
			return;
		}
		UnityEngine.Object.Destroy(Instance.gameObject);
		Instance = this;
	}

	private void Start()
	{
		playersManager = UnityEngine.Object.FindObjectOfType<StartOfRound>();
		totalTime = lengthOfHours * (float)numberOfHours;
		SetCompanyMood();
	}

	private void Update()
	{
		if (GameNetworkManager.Instance == null || GameNetworkManager.Instance.localPlayerController == null)
		{
			return;
		}
		_ = movingGlobalTimeForward;
		if (currentDayTimeStarted)
		{
			if (timeStartedThisFrame)
			{
				timeStartedThisFrame = false;
				TimeOfDayMusic.volume = 0.7f;
				dayModeLastTimePlayerWasOutside = DayMode.None;
				shipLeavingOnMidnight = false;
				shipLeavingAlertCalled = false;
				votedShipToLeaveEarlyThisRound = false;
				shipLeaveAutomaticallyTime = 0.998f;
				votesForShipToLeaveEarly = 0;
				currentDayTime = CalculatePlanetTime(currentLevel);
				hour = (int)(currentDayTime / lengthOfHours);
				previousHour = hour;
				indirectLightData = null;
				globalTimeAtEndOfDay = globalTime + (totalTime - currentDayTime) / currentLevel.DaySpeedMultiplier;
				normalizedTimeOfDay = currentDayTime / totalTime;
				RefreshClockUI();
			}
			else
			{
				MoveTimeOfDay();
				TimeOfDayEvents();
				SetWeatherEffects();
			}
		}
		else
		{
			timeStartedThisFrame = true;
		}
	}

	public void MoveGlobalTime()
	{
		float num = globalTime;
		globalTime = Mathf.Clamp(globalTime + Time.deltaTime * globalTimeSpeedMultiplier, 0f, globalTimeAtEndOfDay);
		num = globalTime - num;
		timeUntilDeadline -= num;
	}

	public float CalculatePlanetTime(SelectableLevel level)
	{
		return (globalTime + level.OffsetFromGlobalTime) * level.DaySpeedMultiplier % (totalTime + 1f);
	}

	public float CalculatePlanetTimeClampToEndOfDay(SelectableLevel level)
	{
		return (Mathf.Clamp(globalTime, 0f, globalTimeAtEndOfDay) + level.OffsetFromGlobalTime) * level.DaySpeedMultiplier % (totalTime + 1f);
	}

	private void MoveTimeOfDay()
	{
		try
		{
			MoveGlobalTime();
			SyncGlobalTimeOnNetwork();
		}
		catch (Exception arg)
		{
			Debug.LogError($"Error updating time of day: {arg}");
		}
		currentDayTime = CalculatePlanetTime(currentLevel);
		hour = (int)(currentDayTime / lengthOfHours);
		if (hour != previousHour)
		{
			previousHour = hour;
			OnHourChanged();
			StartOfRound.Instance.SetDiscordStatusDetails();
		}
		if (sunAnimator != null)
		{
			normalizedTimeOfDay = currentDayTime / totalTime;
			sunAnimator.SetFloat("timeOfDay", Mathf.Clamp(normalizedTimeOfDay, 0f, 0.99f));
			if (changeHUDTimeInterval > 3f)
			{
				changeHUDTimeInterval = 0f;
				HUDManager.Instance.SetClock(normalizedTimeOfDay, numberOfHours);
			}
			else
			{
				changeHUDTimeInterval += Time.deltaTime;
			}
			SetInsideLightingDimness();
		}
	}

	public void SetInsideLightingDimness(bool doNotLerp = false, bool setValueTo = false)
	{
		if (sunDirect == null || sunIndirect == null)
		{
			return;
		}
		if (indirectLightData == null)
		{
			indirectLightData = sunIndirect.GetComponent<HDAdditionalLightData>();
		}
		HUDManager.Instance.SetClockVisible(!insideLighting);
		if (GameNetworkManager.Instance != null)
		{
			if (GameNetworkManager.Instance.localPlayerController.isPlayerDead)
			{
				if (GameNetworkManager.Instance.localPlayerController.spectatedPlayerScript != null)
				{
					sunDirect.enabled = !GameNetworkManager.Instance.localPlayerController.spectatedPlayerScript.isInsideFactory;
				}
			}
			else
			{
				sunDirect.enabled = !GameNetworkManager.Instance.localPlayerController.isInsideFactory;
			}
		}
		PlayerControllerB playerControllerB = GameNetworkManager.Instance.localPlayerController;
		if (GameNetworkManager.Instance.localPlayerController.isPlayerDead && GameNetworkManager.Instance.localPlayerController.spectatedPlayerScript != null)
		{
			playerControllerB = GameNetworkManager.Instance.localPlayerController.spectatedPlayerScript;
		}
		if (playerControllerB.isInsideFactory)
		{
			sunIndirect.enabled = false;
		}
		if (insideLighting)
		{
			indirectLightData.lightDimmer = Mathf.Lerp(indirectLightData.lightDimmer, 0f, 5f * Time.deltaTime);
			return;
		}
		sunIndirect.enabled = true;
		indirectLightData.lightDimmer = Mathf.Lerp(indirectLightData.lightDimmer, 1f, 5f * Time.deltaTime);
	}

	private int RoundUpToNearestTen(float x)
	{
		return (int)(x / 10f) * 10;
	}

	private void SyncGlobalTimeOnNetwork()
	{
		if (base.IsServer && (float)RoundUpToNearestTen(globalTime) >= nextTimeSync)
		{
			nextTimeSync = RoundUpToNearestTen(globalTime + 10f);
			SyncTimeClientRpc(globalTime, (int)timeUntilDeadline);
		}
	}

	[ClientRpc]
	public void SyncTimeClientRpc(float time, int deadline)
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Client && (networkManager.IsServer || networkManager.IsHost))
			{
				ClientRpcParams clientRpcParams = default(ClientRpcParams);
				FastBufferWriter bufferWriter = __beginSendClientRpc(3168707752u, clientRpcParams, RpcDelivery.Reliable);
				bufferWriter.WriteValueSafe(in time, default(FastBufferWriter.ForPrimitives));
				BytePacker.WriteValueBitPacked(bufferWriter, deadline);
				__endSendClientRpc(ref bufferWriter, 3168707752u, clientRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Client && (networkManager.IsClient || networkManager.IsHost))
			{
				globalTime = time;
				timeUntilDeadline = deadline;
				onTimeSync.Invoke();
			}
		}
	}

	public void TimeOfDayEvents()
	{
		dayMode = GetDayPhase(currentDayTime / totalTime);
		if (currentLevel.planetHasTime && !StartOfRound.Instance.shipIsLeaving)
		{
			if (!shipLeavingAlertCalled && currentDayTime / totalTime > 0.9f)
			{
				shipLeavingAlertCalled = true;
				HUDManager.Instance.ReadDialogue(shipLeavingSoonDialogue);
				HUDManager.Instance.shipLeavingEarlyIcon.enabled = true;
			}
			if (base.IsServer && !shipLeavingOnMidnight && currentDayTime / totalTime >= shipLeaveAutomaticallyTime)
			{
				shipLeavingOnMidnight = true;
				SetShipToLeaveOnMidnightClientRpc();
			}
		}
		if (dayMode > dayModeLastTimePlayerWasOutside)
		{
			PlayerSeesNewTimeOfDay();
		}
	}

	public void SetNewProfitQuota()
	{
		if (base.IsServer)
		{
			timesFulfilledQuota++;
			int num = quotaFulfilled - profitQuota;
			float num2 = Mathf.Clamp(1f + (float)timesFulfilledQuota * ((float)timesFulfilledQuota / quotaVariables.increaseSteepness), 0f, 10000f);
			num2 = quotaVariables.baseIncrease * num2 * (quotaVariables.randomizerCurve.Evaluate(UnityEngine.Random.Range(0f, 1f)) * quotaVariables.randomizerMultiplier + 1f);
			profitQuota = (int)Mathf.Clamp((float)profitQuota + num2, 0f, 1E+09f);
			quotaFulfilled = 0;
			timeUntilDeadline = totalTime * 4f;
			int overtimeBonus = num / 5 + 15 * daysUntilDeadline;
			SyncNewProfitQuotaClientRpc(profitQuota, overtimeBonus, timesFulfilledQuota);
		}
	}

	[ClientRpc]
	public void SyncNewProfitQuotaClientRpc(int newProfitQuota, int overtimeBonus, int fulfilledQuota)
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Client && (networkManager.IsServer || networkManager.IsHost))
			{
				ClientRpcParams clientRpcParams = default(ClientRpcParams);
				FastBufferWriter bufferWriter = __beginSendClientRpc(1041683203u, clientRpcParams, RpcDelivery.Reliable);
				BytePacker.WriteValueBitPacked(bufferWriter, newProfitQuota);
				BytePacker.WriteValueBitPacked(bufferWriter, overtimeBonus);
				BytePacker.WriteValueBitPacked(bufferWriter, fulfilledQuota);
				__endSendClientRpc(ref bufferWriter, 1041683203u, clientRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Client && (networkManager.IsClient || networkManager.IsHost))
			{
				quotaFulfilled = 0;
				profitQuota = newProfitQuota;
				timeUntilDeadline = totalTime * (float)quotaVariables.deadlineDaysAmount;
				timesFulfilledQuota = fulfilledQuota;
				StartOfRound.Instance.companyBuyingRate = 0.3f;
				Terminal terminal = UnityEngine.Object.FindObjectOfType<Terminal>();
				terminal.groupCredits = Mathf.Clamp(terminal.groupCredits + overtimeBonus, terminal.groupCredits, 100000000);
				terminal.RotateShipDecorSelection();
				HUDManager.Instance.DisplayNewDeadline(overtimeBonus);
			}
		}
	}

	public void UpdateProfitQuotaCurrentTime()
	{
		daysUntilDeadline = (int)Mathf.Floor(timeUntilDeadline / totalTime);
		hoursUntilDeadline = (int)(timeUntilDeadline / lengthOfHours) - daysUntilDeadline * numberOfHours;
		if (StartOfRound.Instance.isChallengeFile)
		{
			StartOfRound.Instance.deadlineMonitorBGImage.color = new Color(0.5294118f, 1f / 51f, 0.8f, 1f);
			StartOfRound.Instance.profitQuotaMonitorBGImage.color = new Color(0.5294118f, 1f / 51f, 0.8f, 1f);
			StartOfRound.Instance.deadlineMonitorText.text = "AS MUCH PROFIT AS POSSIBLE";
			StartOfRound.Instance.profitQuotaMonitorText.text = "Welcome to\n" + GameNetworkManager.Instance.GetNameForWeekNumber();
			StartOfRound.Instance.profitQuotaMonitorText.fontSize = 62f;
		}
		else
		{
			if (timeUntilDeadline <= 0f)
			{
				StartOfRound.Instance.deadlineMonitorText.text = "DEADLINE:\n NOW";
			}
			else
			{
				StartOfRound.Instance.deadlineMonitorText.text = $"DEADLINE:\n{daysUntilDeadline} Days";
			}
			StartOfRound.Instance.profitQuotaMonitorText.text = $"PROFIT QUOTA:\n${quotaFulfilled} / ${profitQuota}";
		}
	}

	[ClientRpc]
	public void SetShipToLeaveOnMidnightClientRpc()
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Client && (networkManager.IsServer || networkManager.IsHost))
			{
				ClientRpcParams clientRpcParams = default(ClientRpcParams);
				FastBufferWriter bufferWriter = __beginSendClientRpc(749416460u, clientRpcParams, RpcDelivery.Reliable);
				__endSendClientRpc(ref bufferWriter, 749416460u, clientRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Client && (networkManager.IsClient || networkManager.IsHost))
			{
				StartOfRound.Instance.ShipLeaveAutomatically(leavingOnMidnight: true);
			}
		}
	}

	public void VoteShipToLeaveEarly()
	{
		if (!votedShipToLeaveEarlyThisRound)
		{
			votedShipToLeaveEarlyThisRound = true;
			SetShipLeaveEarlyServerRpc();
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void SetShipLeaveEarlyServerRpc()
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Server && (networkManager.IsClient || networkManager.IsHost))
		{
			ServerRpcParams serverRpcParams = default(ServerRpcParams);
			FastBufferWriter bufferWriter = __beginSendServerRpc(543987598u, serverRpcParams, RpcDelivery.Reliable);
			__endSendServerRpc(ref bufferWriter, 543987598u, serverRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Server && (networkManager.IsServer || networkManager.IsHost))
		{
			votesForShipToLeaveEarly++;
			int num = StartOfRound.Instance.connectedPlayersAmount + 1 - StartOfRound.Instance.livingPlayers;
			if (votesForShipToLeaveEarly >= num)
			{
				SetShipLeaveEarlyClientRpc(normalizedTimeOfDay + 0.1f, votesForShipToLeaveEarly);
			}
			else
			{
				AddVoteForShipToLeaveEarlyClientRpc();
			}
		}
	}

	[ClientRpc]
	public void AddVoteForShipToLeaveEarlyClientRpc()
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Client && (networkManager.IsServer || networkManager.IsHost))
			{
				ClientRpcParams clientRpcParams = default(ClientRpcParams);
				FastBufferWriter bufferWriter = __beginSendClientRpc(1359513530u, clientRpcParams, RpcDelivery.Reliable);
				__endSendClientRpc(ref bufferWriter, 1359513530u, clientRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Client && (networkManager.IsClient || networkManager.IsHost) && !base.IsServer)
			{
				votesForShipToLeaveEarly++;
				HUDManager.Instance.SetShipLeaveEarlyVotesText(votesForShipToLeaveEarly);
			}
		}
	}

	[ClientRpc]
	public void SetShipLeaveEarlyClientRpc(float timeToLeaveEarly, int votes)
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Client && (networkManager.IsServer || networkManager.IsHost))
			{
				ClientRpcParams clientRpcParams = default(ClientRpcParams);
				FastBufferWriter bufferWriter = __beginSendClientRpc(3001101610u, clientRpcParams, RpcDelivery.Reliable);
				bufferWriter.WriteValueSafe(in timeToLeaveEarly, default(FastBufferWriter.ForPrimitives));
				BytePacker.WriteValueBitPacked(bufferWriter, votes);
				__endSendClientRpc(ref bufferWriter, 3001101610u, clientRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Client && (networkManager.IsClient || networkManager.IsHost))
			{
				votesForShipToLeaveEarly = votes;
				HUDManager.Instance.SetShipLeaveEarlyVotesText(votes);
				shipLeaveAutomaticallyTime = timeToLeaveEarly;
				shipLeavingAlertCalled = true;
				shipLeavingEarlyDialogue[0].bodyText = "WARNING! Please return by " + HUDManager.Instance.SetClock(timeToLeaveEarly, numberOfHours, createNewLine: false) + ". A vote has been cast, and the autopilot ship will leave early.";
				HUDManager.Instance.ReadDialogue(shipLeavingEarlyDialogue);
				HUDManager.Instance.shipLeavingEarlyIcon.enabled = true;
			}
		}
	}

	[ClientRpc]
	public void ShipFullCapacityMidnightClientRpc()
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Client && (networkManager.IsServer || networkManager.IsHost))
			{
				ClientRpcParams clientRpcParams = default(ClientRpcParams);
				FastBufferWriter bufferWriter = __beginSendClientRpc(711575688u, clientRpcParams, RpcDelivery.Reliable);
				__endSendClientRpc(ref bufferWriter, 711575688u, clientRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Client && (networkManager.IsClient || networkManager.IsHost))
			{
				shipLeavingEarlyDialogue[0].bodyText = "ALERT! The ship has reached full carrying capacity and cannot leave until items are removed!";
				HUDManager.Instance.ReadDialogue(shipLeavingEarlyDialogue);
			}
		}
	}

	public DayMode GetDayPhase(float time)
	{
		if (time >= 0.9f)
		{
			return DayMode.Midnight;
		}
		if (time >= 0.63f)
		{
			return DayMode.Sundown;
		}
		if (time >= 0.33f)
		{
			return DayMode.Noon;
		}
		return DayMode.Dawn;
	}

	private void PlayerSeesNewTimeOfDay()
	{
		if (!GameNetworkManager.Instance.localPlayerController.isInsideFactory && !GameNetworkManager.Instance.localPlayerController.isInHangarShipRoom && playersManager.shipHasLanded)
		{
			dayModeLastTimePlayerWasOutside = dayMode;
			HUDManager.Instance.SetClockIcon(dayMode);
			if (currentLevel.planetHasTime)
			{
				PlayTimeMusicDelayed(timeOfDayCues[(int)dayMode], 0.5f, playRandomDaytimeMusic: true);
			}
		}
	}

	public void PlayTimeMusicDelayed(AudioClip clip, float delay, bool playRandomDaytimeMusic = false)
	{
		if (playDelayedMusicCoroutine != null)
		{
			Debug.Log("Already playing music; cancelled starting new music");
		}
		else
		{
			playDelayedMusicCoroutine = StartCoroutine(playSoundDelayed(clip, delay, playRandomDaytimeMusic));
		}
	}

	private IEnumerator playSoundDelayed(AudioClip clip, float delay, bool playRandomDaytimeMusic)
	{
		Debug.Log("Play time of day sfx");
		yield return new WaitForSeconds(delay);
		TimeOfDayMusic.PlayOneShot(clip, 1f);
		Debug.Log($"Play music!; {TimeOfDayMusic.clip}; {TimeOfDayMusic.volume}");
		if (!playRandomDaytimeMusic || !currentLevel.planetHasTime)
		{
			yield break;
		}
		yield return new WaitForSeconds(3f);
		yield return new WaitForSeconds(UnityEngine.Random.Range(2f, 8f));
		if (insideLighting || GameNetworkManager.Instance.localPlayerController.isInHangarShipRoom || StartOfRound.Instance.fearLevel > 0.03f)
		{
			yield break;
		}
		if (UnityEngine.Random.Range(0, 100) < 20 || ES3.Load("TimesLanded", "LCGeneralSaveData", 0) <= 1)
		{
			if (ES3.Load("TimesLanded", "LCGeneralSaveData", 0) <= 1)
			{
				ES3.Save("TimesLanded", 2, "LCGeneralSaveData");
			}
			SoundManager.Instance.PlayRandomOutsideMusic(dayMode >= DayMode.Sundown);
		}
		playDelayedMusicCoroutine = null;
	}

	private IEnumerator fadeOutEffect(WeatherEffect effect, Vector3 moveFromPosition)
	{
		if (effect.effectObject != null)
		{
			for (int i = 0; i < 270; i++)
			{
				effect.effectObject.transform.position = Vector3.Lerp(effect.effectObject.transform.position, moveFromPosition - Vector3.up * 50f, (float)i / 270f);
				yield return null;
				if (effect.effectObject == null || !effect.transitioning)
				{
					yield break;
				}
			}
		}
		DisableWeatherEffect(effect);
	}

	private void SetWeatherEffects()
	{
		Vector3 vector = ((!GameNetworkManager.Instance.localPlayerController.isPlayerDead) ? StartOfRound.Instance.localPlayerController.transform.position : StartOfRound.Instance.spectateCamera.transform.position);
		for (int i = 0; i < effects.Length; i++)
		{
			if (effects[i].effectEnabled)
			{
				if (!string.IsNullOrEmpty(effects[i].sunAnimatorBool) && sunAnimator != null)
				{
					sunAnimator.SetBool(effects[i].sunAnimatorBool, value: true);
				}
				effects[i].transitioning = false;
				if (effects[i].effectObject != null)
				{
					effects[i].effectObject.SetActive(value: true);
					if (effects[i].lerpPosition)
					{
						effects[i].effectObject.transform.position = Vector3.Lerp(effects[i].effectObject.transform.position, vector, Time.deltaTime);
					}
					else
					{
						effects[i].effectObject.transform.position = vector;
					}
				}
			}
			else if (!effects[i].transitioning)
			{
				effects[i].transitioning = true;
				if (effects[i].lerpPosition)
				{
					StartCoroutine(fadeOutEffect(effects[i], vector));
				}
				else
				{
					DisableWeatherEffect(effects[i]);
				}
			}
		}
	}

	private void DisableWeatherEffect(WeatherEffect effect)
	{
		if (!(effect.effectObject == null))
		{
			effect.effectObject.SetActive(value: false);
		}
	}

	public void DisableAllWeather(bool deactivateObjects = false)
	{
		for (int i = 0; i < effects.Length; i++)
		{
			effects[i].effectEnabled = false;
		}
		if (!deactivateObjects)
		{
			return;
		}
		for (int j = 0; j < effects.Length; j++)
		{
			if (effects[j].effectObject != null)
			{
				effects[j].effectObject.SetActive(value: false);
			}
		}
	}

	public void RefreshClockUI()
	{
		HUDManager.Instance.SetClockIcon(dayMode);
		HUDManager.Instance.SetClock(normalizedTimeOfDay, numberOfHours);
	}

	public void OnHourChanged(int amount = 1)
	{
	}

	public void OnDayChanged()
	{
		if (!StartOfRound.Instance.isChallengeFile)
		{
			StartOfRound.Instance.SetPlanetsWeather();
			SetBuyingRateForDay();
			SetCompanyMood();
		}
	}

	public void SetCompanyMood()
	{
		if (timesFulfilledQuota <= 0)
		{
			currentCompanyMood = CommonCompanyMoods[0];
			return;
		}
		System.Random random = new System.Random(StartOfRound.Instance.randomMapSeed + 164);
		currentCompanyMood = CommonCompanyMoods[random.Next(0, CommonCompanyMoods.Length)];
	}

	public void SetBuyingRateForDay()
	{
		daysUntilDeadline = (int)Mathf.Floor(timeUntilDeadline / totalTime);
		if (daysUntilDeadline == 0)
		{
			StartOfRound.Instance.companyBuyingRate = 1f;
			return;
		}
		float num = 0.3f;
		float num2 = (1f - num) / (float)quotaVariables.deadlineDaysAmount;
		StartOfRound.Instance.companyBuyingRate = num2 * (float)(quotaVariables.deadlineDaysAmount - daysUntilDeadline) + num;
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	[RuntimeInitializeOnLoadMethod]
	internal static void InitializeRPCS_TimeOfDay()
	{
		NetworkManager.__rpc_func_table.Add(3168707752u, __rpc_handler_3168707752);
		NetworkManager.__rpc_func_table.Add(1041683203u, __rpc_handler_1041683203);
		NetworkManager.__rpc_func_table.Add(749416460u, __rpc_handler_749416460);
		NetworkManager.__rpc_func_table.Add(543987598u, __rpc_handler_543987598);
		NetworkManager.__rpc_func_table.Add(1359513530u, __rpc_handler_1359513530);
		NetworkManager.__rpc_func_table.Add(3001101610u, __rpc_handler_3001101610);
		NetworkManager.__rpc_func_table.Add(711575688u, __rpc_handler_711575688);
	}

	private static void __rpc_handler_3168707752(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out float value, default(FastBufferWriter.ForPrimitives));
			ByteUnpacker.ReadValueBitPacked(reader, out int value2);
			target.__rpc_exec_stage = __RpcExecStage.Client;
			((TimeOfDay)target).SyncTimeClientRpc(value, value2);
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	private static void __rpc_handler_1041683203(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			ByteUnpacker.ReadValueBitPacked(reader, out int value2);
			ByteUnpacker.ReadValueBitPacked(reader, out int value3);
			target.__rpc_exec_stage = __RpcExecStage.Client;
			((TimeOfDay)target).SyncNewProfitQuotaClientRpc(value, value2, value3);
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	private static void __rpc_handler_749416460(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Client;
			((TimeOfDay)target).SetShipToLeaveOnMidnightClientRpc();
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	private static void __rpc_handler_543987598(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Server;
			((TimeOfDay)target).SetShipLeaveEarlyServerRpc();
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	private static void __rpc_handler_1359513530(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Client;
			((TimeOfDay)target).AddVoteForShipToLeaveEarlyClientRpc();
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	private static void __rpc_handler_3001101610(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out float value, default(FastBufferWriter.ForPrimitives));
			ByteUnpacker.ReadValueBitPacked(reader, out int value2);
			target.__rpc_exec_stage = __RpcExecStage.Client;
			((TimeOfDay)target).SetShipLeaveEarlyClientRpc(value, value2);
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	private static void __rpc_handler_711575688(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Client;
			((TimeOfDay)target).ShipFullCapacityMidnightClientRpc();
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	protected internal override string __getTypeName()
	{
		return "TimeOfDay";
	}
}
