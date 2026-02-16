using Unity.Netcode;
using UnityEngine;

public class RandomPeriodicAudioPlayer : NetworkBehaviour
{
	public GrabbableObject attachedGrabbableObject;

	public AudioClip[] randomClips;

	public AudioSource thisAudio;

	public float audioMinInterval;

	public float audioMaxInterval;

	public float audioChancePercent;

	private float currentInterval;

	private float lastIntervalTime;

	private void Update()
	{
		if (base.IsServer && !(GameNetworkManager.Instance.localPlayerController == null) && (!(attachedGrabbableObject != null) || !attachedGrabbableObject.deactivated) && Time.realtimeSinceStartup - lastIntervalTime > currentInterval)
		{
			lastIntervalTime = Time.realtimeSinceStartup;
			currentInterval = Time.realtimeSinceStartup + Random.Range(audioMinInterval, audioMaxInterval);
			if (Random.Range(0f, 100f) < audioChancePercent)
			{
				PlayRandomAudioClientRpc(Random.Range(0, randomClips.Length));
			}
		}
	}

	[ClientRpc]
	public void PlayRandomAudioClientRpc(int clipIndex)
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Client && (networkManager.IsServer || networkManager.IsHost))
			{
				ClientRpcParams clientRpcParams = default(ClientRpcParams);
				FastBufferWriter bufferWriter = __beginSendClientRpc(1557920159u, clientRpcParams, RpcDelivery.Reliable);
				BytePacker.WriteValueBitPacked(bufferWriter, clipIndex);
				__endSendClientRpc(ref bufferWriter, 1557920159u, clientRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Client && (networkManager.IsClient || networkManager.IsHost))
			{
				PlayAudio(clipIndex);
			}
		}
	}

	private void PlayAudio(int clipIndex)
	{
		AudioClip clip = randomClips[clipIndex];
		thisAudio.PlayOneShot(clip, 1f);
		WalkieTalkie.TransmitOneShotAudio(thisAudio, clip);
		RoundManager.Instance.PlayAudibleNoise(thisAudio.transform.position, 7f, 0.6f);
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	[RuntimeInitializeOnLoadMethod]
	internal static void InitializeRPCS_RandomPeriodicAudioPlayer()
	{
		NetworkManager.__rpc_func_table.Add(1557920159u, __rpc_handler_1557920159);
	}

	private static void __rpc_handler_1557920159(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			target.__rpc_exec_stage = __RpcExecStage.Client;
			((RandomPeriodicAudioPlayer)target).PlayRandomAudioClientRpc(value);
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	protected internal override string __getTypeName()
	{
		return "RandomPeriodicAudioPlayer";
	}
}
