using Photon.Voice;
using Photon.Voice.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DeviceInfo = Photon.Voice.DeviceInfo;

namespace FusionImpostor
{
	/// <summary>
	/// Class for handling the dropdown menu for microphones.
	/// </summary>
	public class MicrophoneDropdownFiller : VoiceComponent
	{
		public Dropdown dropdown;

		readonly List<DeviceInfo> availableDevices = new List<DeviceInfo>();

		public DeviceInfo CurrentDevice => availableDevices.Count == 0 ? default : availableDevices[dropdown.value];

		bool initialized;

		protected override void Awake()
		{
			base.Awake();

			StartCoroutine(WaitForVM());
		}

		IEnumerator WaitForVM()
		{
			while (GameManager.vm == null)
				yield return null;

			Debug.Log("Voice Manager Ready.");

			Init();
		}

		public void Init()
		{
			if (initialized)
				return;

			FillDropdown();
			dropdown.onValueChanged.AddListener(AssignMicrophone);

			initialized = true;
		}

		void AssignMicrophone(int index)
		{
			if (!initialized || GameManager.vm.Rec == null)
				return;

			GameManager.vm.Rec.MicrophoneDevice = availableDevices[index];
		}

		void FillDropdown()
		{
			availableDevices.Clear();
			dropdown.ClearOptions();

			List<string> opts = new List<string>();
			foreach(DeviceInfo item in Platform.CreateAudioInEnumerator(this.Logger))
			{
				availableDevices.Add(item);
				opts.Add(item.Name);
			}

			dropdown.AddOptions(opts);
		}
	}
}