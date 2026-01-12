using System.Collections;
using TMPro;
using UnityEngine;
using Toggle = UnityEngine.UI.Toggle;

namespace FusionImpostor
{
	/// <summary>
	/// Class for the pregame settings screen where the host sets different aspects of the game.
	/// </summary>
	public class SettingsUI : MonoBehaviour
	{
		public TMP_Text impostersText;
		public TMP_Text tasksText;
		public TMP_Text meetingsText;
		public TMP_Text discussTimeText;
		public TMP_Text voteTimeText;
		public TMP_Text walkSpeedText;
		public Toggle playerCollisionToggle;

		GameSettings workingSettings;
		int delta = 0;

		public void Open()
		{
			workingSettings = GameManager.Instance.Settings;
			impostersText.text = $"{workingSettings.numImposters}";
			tasksText.text = $"{workingSettings.numTasks}";
			meetingsText.text = $"{workingSettings.numEmergencyMeetings}";
			discussTimeText.text = $"{workingSettings.discussionTime}";
			voteTimeText.text = $"{workingSettings.votingTime}";
			walkSpeedText.text = $"{workingSettings.walkSpeed}";
			playerCollisionToggle.isOn = workingSettings.playerCollision;
			gameObject.SetActive(true);
		}

		public void Close(bool saveChanges)
		{
			if (saveChanges)
				GameManager.Instance.Settings = workingSettings;
			gameObject.SetActive(false);
			PlayerMovement.Local.RPC_EndInteraction();
		}

		public void StartGame()
		{
			Close(true);
			GameManager.Instance.Server_StartGame();
		}

		public void Release()
		{
			delta = 0;
		}

		public void IncreaseImposters()
		{
			delta = 1;
			StartCoroutine(UpdateSetting(DoImposters));
		}

		public void IncreaseTasks()
		{
			delta = 1;
			StartCoroutine(UpdateSetting(DoTasks));
		}

		public void IncreaseMeetings()
		{
			delta = 1;
			StartCoroutine(UpdateSetting(DoMeetings));
		}

		public void IncreaseDiscussion()
		{
			delta = 5;
			StartCoroutine(UpdateSetting(DoDiscussion));
		}

		public void IncreaseVoting()
		{
			delta = 5;
			StartCoroutine(UpdateSetting(DoVoting));
		}

		public void IncreaseWalkSpeed()
		{
			delta = 1;
			StartCoroutine(UpdateSetting(DoWalkSpeed));
		}

		public void DecreaseImposters()
		{
			delta = -1;
			StartCoroutine(UpdateSetting(DoImposters));
		}

		public void DecreaseTasks()
		{
			delta = -1;
			StartCoroutine(UpdateSetting(DoTasks));
		}

		public void DecreaseMeetings()
		{
			delta = -1;
			StartCoroutine(UpdateSetting(DoMeetings));
		}

		public void DecreaseDiscussion()
		{
			delta = -5;
			StartCoroutine(UpdateSetting(DoDiscussion));
		}

		public void DecreaseVoting()
		{
			delta = -5;
			StartCoroutine(UpdateSetting(DoVoting));
		}

		public void DecreaseWalkSpeed()
		{
			delta = -1;
			StartCoroutine(UpdateSetting(DoWalkSpeed));
		}

		public void SetPlayerCollision(bool on)
		{
			workingSettings.playerCollision = on;
		}

		void DoImposters()
		{
			if (delta < 0 && workingSettings.numImposters == GameSettings.MIN_IMPOSTERS) return;
			workingSettings.numImposters = Clamp(
				GameSettings.MIN_IMPOSTERS,
				(byte)Mathf.Min(PlayerRegistry.Count, GameSettings.MAX_IMPOSTERS),
				(byte)(workingSettings.numImposters + delta));
			impostersText.text = $"{workingSettings.numImposters}";
		}

		void DoTasks()
		{
			workingSettings.numTasks = Clamp(
				GameSettings.MIN_TASKS,
				GameSettings.MAX_TASKS,
				(byte)(workingSettings.numTasks + delta));
			tasksText.text = $"{workingSettings.numTasks}";
		}

		void DoMeetings()
		{
			if (delta < 0 && workingSettings.numEmergencyMeetings == GameSettings.MIN_MEETINGS) return;
			workingSettings.numEmergencyMeetings = Clamp(
				GameSettings.MIN_MEETINGS,
				GameSettings.MAX_MEETINGS,
				(byte)(workingSettings.numEmergencyMeetings + delta));
			meetingsText.text = $"{workingSettings.numEmergencyMeetings}";
		}

		void DoDiscussion()
		{
			if (delta < 0 && workingSettings.discussionTime == GameSettings.MIN_DISCUSSION) return;
			workingSettings.discussionTime = Clamp(
				GameSettings.MIN_DISCUSSION,
				GameSettings.MAX_DISCUSSION,
				(ushort)(workingSettings.discussionTime + delta));
			discussTimeText.text = $"{workingSettings.discussionTime}";
		}

		void DoVoting()
		{
			workingSettings.votingTime = Clamp(
				GameSettings.MIN_VOTING,
				GameSettings.MAX_VOTING,
				(ushort)(workingSettings.votingTime + delta));
			voteTimeText.text = $"{workingSettings.votingTime}";
		}

		void DoWalkSpeed()
		{
			workingSettings.walkSpeed = Clamp(
				GameSettings.MIN_WALK_SPEED,
				GameSettings.MAX_WALK_SPEED,
				(byte)(workingSettings.walkSpeed + delta));
			walkSpeedText.text = $"{workingSettings.walkSpeed}";
		}

		IEnumerator UpdateSetting(System.Action action)
		{
			float tDown = Time.time;
			while (delta != 0)
			{
				float t = (Time.time - tDown);

				action.Invoke();

				yield return new WaitForSeconds(t == 0 ? 0.5f : t < 1.5f ? 0.25f : 0.15f);
			}
		}

		T Clamp<T>(T min, T max, T val) where T : System.IComparable<T>
		{
			return min.CompareTo(val) > 0 ? min : max.CompareTo(val) < 0 ? max : val;
		}
	}
}