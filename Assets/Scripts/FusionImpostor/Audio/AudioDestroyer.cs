using UnityEngine;

namespace FusionImpostor
{
	/// <summary>
	/// Destroys the AudioSource component after it has finished playing.
	/// </summary>
	public class AudioDestroyer : MonoBehaviour
	{
		private AudioSource audioSource;

		private void Awake()
		{
			audioSource = GetComponent<AudioSource>();
		}

		private void Update()
		{
			if (audioSource != null && !audioSource.isPlaying)
			{
				Destroy(audioSource);
				Destroy(this);
			}
		}
	}
}