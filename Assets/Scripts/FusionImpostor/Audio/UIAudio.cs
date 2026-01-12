using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FusionImpostor
{
	/// <summary>
	/// Plays UI audio clips on button clicks and other UI events.
	/// </summary>
	[RequireComponent(typeof(AudioSource))]
	public class UIAudio : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler
	{
		[Header("Audio Clips")]
		[Tooltip("Audio clip to play on button click.")]
		public AudioClip clickClip;

		[Tooltip("Audio clip to play on pointer enter.")]
		public AudioClip hoverClip;

		private AudioSource audioSource;

		private void Awake()
		{
			audioSource = GetComponent<AudioSource>();
		}

		public void OnPointerClick(PointerEventData eventData)
		{
			PlaySound(clickClip);
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			PlaySound(hoverClip);
		}

		private void PlaySound(AudioClip clip)
		{
			if (clip == null) return;

			AudioSource newAudioSource = gameObject.AddComponent<AudioSource>();
			newAudioSource.clip = clip;
			newAudioSource.Play();

			// Add AudioDestroyer to clean up after the sound has played
			gameObject.AddComponent<AudioDestroyer>();
		}
	}
}