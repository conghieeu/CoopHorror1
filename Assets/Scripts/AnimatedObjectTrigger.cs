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
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Server && (networkManager.IsClient || networkManager.IsHost))
			{
				ServerRpcParams serverRpcParams = default(ServerRpcParams);
				FastBufferWriter bufferWriter = __beginSendServerRpc(1461767556u, serverRpcParams, RpcDelivery.Reliable);
				bufferWriter.WriteValueSafe(in setBool, default(FastBufferWriter.ForPrimitives));
				bufferWriter.WriteValueSafe(in playSecondaryAudios, default(FastBufferWriter.ForPrimitives));
				BytePacker.WriteValueBitPacked(bufferWriter, playerWhoTriggered);
				__endSendServerRpc(ref bufferWriter, 1461767556u, serverRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Server && (networkManager.IsServer || networkManager.IsHost))
			{
				UpdateAnimClientRpc(setBool, playSecondaryAudios, playerWhoTriggered);
			}
		}
	}

	[ClientRpc]
	private void UpdateAnimClientRpc(bool setBool, bool playSecondaryAudios = false, int playerWhoTriggered = -1)
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Client && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default(ClientRpcParams);
			FastBufferWriter bufferWriter = __beginSendClientRpc(848048148u, clientRpcParams, RpcDelivery.Reliable);
			bufferWriter.WriteValueSafe(in setBool, default(FastBufferWriter.ForPrimitives));
			bufferWriter.WriteValueSafe(in playSecondaryAudios, default(FastBufferWriter.ForPrimitives));
			BytePacker.WriteValueBitPacked(bufferWriter, playerWhoTriggered);
			__endSendClientRpc(ref bufferWriter, 848048148u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage != __RpcExecStage.Client || (!networkManager.IsClient && !networkManager.IsHost) || GameNetworkManager.Instance.localPlayerController == null || (playerWhoTriggered != -1 && (int)GameNetworkManager.Instance.localPlayerController.playerClientId == playerWhoTriggered))
		{
			return;
		}
		if (isBool)
		{
			if (triggerAnimatorB != null)
			{
				triggerAnimatorB.SetBool("on", setBool);
			}
			boolValue = setBool;
			if (triggerAnimator != null)
			{
				triggerAnimator.SetBool(animationString, setBool);
			}
			onTriggerBool.Invoke(boolValue);
		}
		else
		{
			triggerAnimator.SetTrigger(animationString);
		}
		SetParticleBasedOnBoolean();
		PlayAudio(setBool, playSecondaryAudios);
	}

	public void SetBoolOnClientOnly(bool setTo)
	{
		if (isBool)
		{
			boolValue = setTo;
			if (triggerAnimator != null)
			{
				triggerAnimator.SetBool(animationString, boolValue);
			}
			SetParticleBasedOnBoolean();
		}
		PlayAudio(boolValue);
	}

	public void SetBoolOnClientOnlyInverted(bool setTo)
	{
		if (isBool)
		{
			boolValue = !setTo;
			if (triggerAnimator != null)
			{
				triggerAnimator.SetBool(animationString, boolValue);
			}
			SetParticleBasedOnBoolean();
		}
		PlayAudio(boolValue);
	}

	private void SetParticleBasedOnBoolean()
	{
		if (!(playParticle == null))
		{
			if (boolValue)
			{
				playParticle.Play(withChildren: true);
			}
			else
			{
				playParticle.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
			}
		}
	}

	[ServerRpc(RequireOwnership = false)]
	private void UpdateAnimTriggerServerRpc()
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Server && (networkManager.IsClient || networkManager.IsHost))
			{
				ServerRpcParams serverRpcParams = default(ServerRpcParams);
				FastBufferWriter bufferWriter = __beginSendServerRpc(2219526317u, serverRpcParams, RpcDelivery.Reliable);
				__endSendServerRpc(ref bufferWriter, 2219526317u, serverRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Server && (networkManager.IsServer || networkManager.IsHost))
			{
				UpdateAnimTriggerClientRpc();
			}
		}
	}

	[ClientRpc]
	private void UpdateAnimTriggerClientRpc()
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Client && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default(ClientRpcParams);
			FastBufferWriter bufferWriter = __beginSendClientRpc(1023577379u, clientRpcParams, RpcDelivery.Reliable);
			__endSendClientRpc(ref bufferWriter, 1023577379u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage != __RpcExecStage.Client || (!networkManager.IsClient && !networkManager.IsHost))
		{
			return;
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

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	[RuntimeInitializeOnLoadMethod]
	internal static void InitializeRPCS_AnimatedObjectTrigger()
	{
		NetworkManager.__rpc_func_table.Add(1461767556u, __rpc_handler_1461767556);
		NetworkManager.__rpc_func_table.Add(848048148u, __rpc_handler_848048148);
		NetworkManager.__rpc_func_table.Add(2219526317u, __rpc_handler_2219526317);
		NetworkManager.__rpc_func_table.Add(1023577379u, __rpc_handler_1023577379);
	}

	private static void __rpc_handler_1461767556(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out bool value, default(FastBufferWriter.ForPrimitives));
			reader.ReadValueSafe(out bool value2, default(FastBufferWriter.ForPrimitives));
			ByteUnpacker.ReadValueBitPacked(reader, out int value3);
			target.__rpc_exec_stage = __RpcExecStage.Server;
			((AnimatedObjectTrigger)target).UpdateAnimServerRpc(value, value2, value3);
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	private static void __rpc_handler_848048148(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			reader.ReadValueSafe(out bool value, default(FastBufferWriter.ForPrimitives));
			reader.ReadValueSafe(out bool value2, default(FastBufferWriter.ForPrimitives));
			ByteUnpacker.ReadValueBitPacked(reader, out int value3);
			target.__rpc_exec_stage = __RpcExecStage.Client;
			((AnimatedObjectTrigger)target).UpdateAnimClientRpc(value, value2, value3);
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	private static void __rpc_handler_2219526317(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Server;
			((AnimatedObjectTrigger)target).UpdateAnimTriggerServerRpc();
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	private static void __rpc_handler_1023577379(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Client;
			((AnimatedObjectTrigger)target).UpdateAnimTriggerClientRpc();
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	protected internal override string __getTypeName()
	{
		return "AnimatedObjectTrigger";
	}
}
