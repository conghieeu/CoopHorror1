using Unity.Netcode;
using UnityEngine;

public class HighAndLowAltitudeAudio : MonoBehaviour
{
	public AudioSource HighAudio;

	public AudioSource LowAudio;

	public float maxAltitude;

	public float minAltitude;

	private void Update()
	{
		if (!(GameNetworkManager.Instance.localPlayerController == null) && !(NetworkManager.Singleton == null))
		{
			if (GameNetworkManager.Instance.localPlayerController.isInsideFactory)
			{
				HighAudio.volume = 0f;
				LowAudio.volume = 0f;
			}
			else if (!GameNetworkManager.Instance.localPlayerController.isPlayerDead)
			{
				SetAudioVolumeBasedOnAltitude(GameNetworkManager.Instance.localPlayerController.transform.position.y);
			}
			else if (GameNetworkManager.Instance.localPlayerController.spectatedPlayerScript != null)
			{
				SetAudioVolumeBasedOnAltitude(GameNetworkManager.Instance.localPlayerController.spectatedPlayerScript.transform.position.y);
			}
		}
	}

	private void SetAudioVolumeBasedOnAltitude(float playerHeight)
	{
		HighAudio.volume = Mathf.Clamp((playerHeight - minAltitude) / maxAltitude, 0f, 1f);
		LowAudio.volume = Mathf.Abs(HighAudio.volume - 1f);
	}
}
