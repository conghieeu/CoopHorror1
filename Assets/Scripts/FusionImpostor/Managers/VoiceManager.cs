using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Photon.Voice.Unity;

namespace FusionImpostor
{
	/// <summary>
	/// Handles switching the voice channels so the proper players can interact.
	/// </summary>
	public class VoiceManager : MonoBehaviour
	{
		public const byte GLOBAL = 0;
		public const byte GHOST = 1;
		public const byte NONE = 255;

		[field: SerializeField] public UnityEvent OnVoiceInit { get; private set; }

		public VoiceConnection VoiceConn { get; private set; }
		public Recorder Rec { get; private set; }

		public MicrophoneDropdownFiller microphoneDropdown;

		public void Init(VoiceConnection voiceConn, Recorder rec)
		{
			VoiceConn = voiceConn;
			Rec = rec;

			Rec.MicrophoneDevice = microphoneDropdown.CurrentDevice;

			if (OnVoiceInit != null) OnVoiceInit.Invoke();
		}

		public void JoinListenChannel(byte ch)
		{
			VoiceConn.Client.OpChangeGroups(null, new byte[] { ch });
		}

		public void ClearListenChannel(byte ch)
		{
			VoiceConn.Client.OpChangeGroups(new byte[] { ch }, null);
		}

		public void SetTalkChannel(byte ch)
		{
			Rec.InterestGroup = ch;
		}
	}
}