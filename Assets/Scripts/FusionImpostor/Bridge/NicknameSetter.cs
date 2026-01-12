using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace FusionImpostor
{
	/// <summary>
	/// Class that handles the setting of player's nickname
	/// </summary>
	public class NicknameSetter : MonoBehaviour
	{
		public TMP_InputField nicknameField;

		private void OnEnable()
		{
			GetNickname();
		}

		public void GetNickname()
		{
			if (PlayerPrefs.HasKey("nickname"))
			{
				string nickname = PlayerPrefs.GetString("nickname");
				nicknameField.SetTextWithoutNotify(nickname);
			}
			else
			{
				SetNickname("Player");
			}
		}

		public void SetNickname(string nickname)
		{
			if (string.IsNullOrEmpty(nickname))
			{
				PlayerPrefs.DeleteKey("nickname");
			}
			else
			{
				PlayerPrefs.SetString("nickname", nickname);
			}
		}
	}
}
