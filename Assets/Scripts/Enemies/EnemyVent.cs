using Unity.Netcode;
using UnityEngine;

public class EnemyVent : NetworkBehaviour
{
	public float spawnTime;

	public bool occupied;

	[Space(5f)]
	public EnemyType enemyType;

	public int enemyTypeIndex;

	[Space(10f)]
	public AudioSource ventAudio;

	public AudioLowPassFilter lowPassFilter;

	public AudioClip ventCrawlSFX;

	public Transform floorNode;

	private bool isPlayingAudio;

	private RoundManager roundManager;

	public Animator ventAnimator;

	public bool ventIsOpen;

	private void Start()
	{
		roundManager = Object.FindObjectOfType<RoundManager>();
	}

	private void BeginVentSFX()
	{
		if (enemyType.overrideVentSFX != null)
		{
			ventAudio.clip = enemyType.overrideVentSFX;
		}
		else
		{
			ventAudio.clip = ventCrawlSFX;
		}
		ventAudio.Play();
		ventAudio.volume = 0f;
	}

	[ClientRpc]
	public void OpenVentClientRpc()
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Client && (networkManager.IsServer || networkManager.IsHost))
		{
			ClientRpcParams clientRpcParams = default(ClientRpcParams);
			FastBufferWriter bufferWriter = __beginSendClientRpc(2182253155u, clientRpcParams, RpcDelivery.Reliable);
			__endSendClientRpc(ref bufferWriter, 2182253155u, clientRpcParams, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Client && (networkManager.IsClient || networkManager.IsHost))
		{
			if (!ventIsOpen)
			{
				ventIsOpen = true;
				ventAnimator.SetTrigger("openVent");
				lowPassFilter.lowpassResonanceQ = 0f;
			}
			occupied = false;
		}
	}

	[ClientRpc]
	public void SyncVentSpawnTimeClientRpc(int time, int enemyIndex)
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (__rpc_exec_stage != __RpcExecStage.Client && (networkManager.IsServer || networkManager.IsHost))
			{
				ClientRpcParams clientRpcParams = default(ClientRpcParams);
				FastBufferWriter bufferWriter = __beginSendClientRpc(3841281693u, clientRpcParams, RpcDelivery.Reliable);
				BytePacker.WriteValueBitPacked(bufferWriter, time);
				BytePacker.WriteValueBitPacked(bufferWriter, enemyIndex);
				__endSendClientRpc(ref bufferWriter, 3841281693u, clientRpcParams, RpcDelivery.Reliable);
			}
			if (__rpc_exec_stage == __RpcExecStage.Client && (networkManager.IsClient || networkManager.IsHost))
			{
				enemyTypeIndex = enemyIndex;
				enemyType = roundManager.currentLevel.Enemies[enemyIndex].enemyType;
				spawnTime = time;
				occupied = true;
			}
		}
	}

	private void Update()
	{
		if (occupied)
		{
			if (!isPlayingAudio)
			{
				if (spawnTime - roundManager.timeScript.currentDayTime < enemyType.timeToPlayAudio)
				{
					isPlayingAudio = true;
					BeginVentSFX();
				}
			}
			else
			{
				ventAudio.volume = Mathf.Abs((spawnTime - roundManager.timeScript.currentDayTime) / enemyType.timeToPlayAudio - 1f);
				lowPassFilter.lowpassResonanceQ = Mathf.Abs(ventAudio.volume * 2f - 2f);
			}
		}
		else if (isPlayingAudio)
		{
			isPlayingAudio = false;
			ventAudio.Stop();
		}
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	[RuntimeInitializeOnLoadMethod]
	internal static void InitializeRPCS_EnemyVent()
	{
		NetworkManager.__rpc_func_table.Add(2182253155u, __rpc_handler_2182253155);
		NetworkManager.__rpc_func_table.Add(3841281693u, __rpc_handler_3841281693);
	}

	private static void __rpc_handler_2182253155(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			target.__rpc_exec_stage = __RpcExecStage.Client;
			((EnemyVent)target).OpenVentClientRpc();
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	private static void __rpc_handler_3841281693(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
	{
		NetworkManager networkManager = target.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			ByteUnpacker.ReadValueBitPacked(reader, out int value);
			ByteUnpacker.ReadValueBitPacked(reader, out int value2);
			target.__rpc_exec_stage = __RpcExecStage.Client;
			((EnemyVent)target).SyncVentSpawnTimeClientRpc(value, value2);
			target.__rpc_exec_stage = __RpcExecStage.None;
		}
	}

	protected internal override string __getTypeName()
	{
		return "EnemyVent";
	}
}
