using System;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

public class AnimatedObjectTrigger : NetworkBehaviour
{
	public Animator triggerAnimator;

	public Animator triggerAnimatorB;

	public bool isBool = true;

	public string animationString;

	public bool boolValue;

	public bool setInitialState;

	public bool initialBoolState;

	[Space(5f)]
	public AudioSource thisAudioSource;

	public AudioClip[] boolFalseAudios;

	public AudioClip[] boolTrueAudios;

	public AudioClip[] secondaryAudios;

	[Space(4f)]
	public AudioClip playWhileTrue;

	public bool resetAudioWhenFalse;

	public bool makeAudibleNoise;

	public float noiseLoudness = 0.7f;

	[Space(3f)]
	public ParticleSystem playParticle;

	[Space(4f)]
	private StartOfRound playersManager;

	private bool localPlayerTriggered;

	public BooleanEvent onTriggerBool;

	[Space(5f)]
	public bool playAudiosInSequence;

	private int timesTriggered;

	public bool triggerByChance;

	public float chancePercent = 5f;

	private bool hasInitializedRandomSeed;

	public System.Random triggerRandom;

	private float audioTime;

	public void Start()
	{
		if (setInitialState)
		{
			boolValue = initialBoolState;
			triggerAnimator.SetBool(animationString, boolValue);
			if (triggerAnimatorB != null)
			{
				triggerAnimatorB.SetBool("on", boolValue);
			}
		}
	}

	public void TriggerAnimation(PlayerControllerB playerWhoTriggered)
	{
		if (triggerByChance)
		{
			InitializeRandomSeed();
			if ((float)triggerRandom.Next(100) >= chancePercent)
			{
				return;
			}
		}
		if (isBool)
		{
			Debug.Log($"Triggering animated object trigger bool: setting to {!boolValue}");
			boolValue = !boolValue;
			if (triggerAnimator != null)
			{
				triggerAnimator.SetBool(animationString, boolValue);
			}
			if (triggerAnimatorB != null)
			{
				triggerAnimatorB.SetBool("on", boolValue);
			}
		}
		else if (triggerAnimator != null)
		{
			triggerAnimator.SetTrigger(animationString);
		}
		SetParticleBasedOnBoolean();
		PlayAudio(boolValue);
		localPlayerTriggered = true;
		if (isBool)
		{
			onTriggerBool.Invoke(boolValue);
			UpdateAnimServerRpc(boolValue, playSecondaryAudios: false, (int)playerWhoTriggered.playerClientId);
		}
		else
		{
			UpdateAnimTriggerServerRpc();
		}
	}

	public void TriggerAnimationNonPlayer(bool playSecondaryAudios = false, bool overrideBool = false, bool setBoolFalse = false)
	{
		if (overrideBool && setBoolFalse && !boolValue)
		{
			return;
		}
		if (triggerByChance)
		{
			InitializeRandomSeed();
			if ((float)triggerRandom.Next(100) >= chancePercent)
			{
				return;
			}
		}
		if (isBool)
		{
			boolValue = !boolValue;
			triggerAnimator.SetBool(animationString, boolValue);
		}
		else
		{
			triggerAnimator.SetTrigger(animationString);
		}
		SetParticleBasedOnBoolean();
		PlayAudio(boolValue, playSecondaryAudios);
		localPlayerTriggered = true;
		if (isBool)
		{
			UpdateAnimServerRpc(boolValue, playSecondaryAudios);
		}
		else
		{
			UpdateAnimTriggerServerRpc();
		}
	}

	public void InitializeRandomSeed()
	{
		if (!hasInitializedRandomSeed)
		{
			hasInitializedRandomSeed = true;
			triggerRandom = new System.Random(playersManager.randomMapSeed);
		}
	}

	[ServerRpc(RequireOwnership = false)]
	private void UpdateAnimServerRpc(bool setBool, bool playSecondaryAudios = false, int playerWhoTriggered = -1)
	{
		UpdateAnimClientRpc(setBool, playSecondaryAudios, playerWhoTriggered);
	}

	[ClientRpc]
	private void UpdateAnimClientRpc(bool setBool, bool playSecondaryAudios = false, int playerWhoTriggered = -1)
	{
		UpdateAnimTriggerClientRpc();
	}

	[ClientRpc]
	private void UpdateAnimTriggerClientRpc()
	{
		{
			ClientRpcParams clientRpcParams = default(ClientRpcParams);
		}
		onTriggerBool.Invoke(arg0: false);
		if (localPlayerTriggered)
		{
			localPlayerTriggered = false;
			return;
		}
		if (triggerAnimator != null)
		{
			triggerAnimator.SetTrigger(animationString);
		}
		PlayAudio(boolVal: false);
	}

	private void PlayAudio(bool boolVal, bool playSecondaryAudios = false)
	{
		if (GameNetworkManager.Instance.localPlayerController == null || thisAudioSource == null)
		{
			return;
		}
		Debug.Log($"bool val: {boolVal}");
		if (playWhileTrue != null)
		{
			thisAudioSource.clip = playWhileTrue;
			if (boolVal)
			{
				thisAudioSource.Play();
				if (!resetAudioWhenFalse)
				{
					thisAudioSource.time = audioTime;
				}
			}
			else
			{
				audioTime = thisAudioSource.time;
				thisAudioSource.Stop();
			}
		}
		AudioClip audioClip = null;
		if (playSecondaryAudios)
		{
			audioClip = secondaryAudios[UnityEngine.Random.Range(0, secondaryAudios.Length)];
		}
		else if (boolVal && boolTrueAudios.Length != 0)
		{
			audioClip = boolTrueAudios[UnityEngine.Random.Range(0, boolTrueAudios.Length)];
		}
		else if (boolFalseAudios.Length != 0)
		{
			if (playAudiosInSequence)
			{
				if (timesTriggered >= boolFalseAudios.Length)
				{
					return;
				}
				audioClip = boolFalseAudios[timesTriggered];
			}
			else
			{
				audioClip = boolFalseAudios[UnityEngine.Random.Range(0, boolFalseAudios.Length)];
			}
		}
		if (!(audioClip == null))
		{
			thisAudioSource.PlayOneShot(audioClip, 1f);
			WalkieTalkie.TransmitOneShotAudio(thisAudioSource, audioClip);
			if (makeAudibleNoise)
			{
				RoundManager.Instance.PlayAudibleNoise(thisAudioSource.transform.position, 18f, noiseLoudness, 0, StartOfRound.Instance.hangarDoorsClosed, 400);
			}
		}
	}


}
