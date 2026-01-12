using Helpers.Common;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FusionImpostor
{
	/// <summary>
	/// Class that maintains the game's UI and its various references.
	/// </summary>
	public class GameUI : MonoBehaviour
	{
		[Header("UI References")]
		public TMP_Text pingText;
		public TMP_Text roomNameText;
		public TMP_Text regionText;
		public Image totalTaskBarFill;
		public Transform taskHolder;
		public Button mapButton;
		public Button optionsButton;
		public Button chatButton;
		public Button reportButton;
		public Button useButton;
		public Button killButton;
		public Image killTimerFill;
		public GameObject totalTaskBarObject;
		public GameObject taskListObject;
		public GameObject messageScreen;
		public GameObject votingScreen;
		public GameObject mapScreen;
		public GameObject muteButtonOverlay;
		public TMP_Text messageText;
		public TMP_Text messageTypeText;
		public TMP_Text messageNicknameText;
		public CanvasGroup messageTextCG;
		public CanvasGroup messageScreenCG;
		public MeetingUI meetingUI;
		public ColorSelectionUI colorUI;
		public Image messageScreenPanel, messageScreenGlow;
		[Header("Game Settings References")]
		public GameObject gameSettingsObject;
		[Header("Prefabs")]
		public TaskItemUI taskItem;

		private void Awake()
		{
			SetRegionText(Fusion.Photon.Realtime.PhotonAppSettings.Global.AppSettings.FixedRegion);
		}

		public void SetRegionText(string region)
		{
			regionText.text = $"Region: <color=#FFAD00>{region.ToUpper()}</color>";
		}


		public void InitPregame(Fusion.NetworkRunner runner)
		{
			totalTaskBarObject.SetActive(false);
			taskListObject.SetActive(false);
			reportButton.gameObject.SetActive(false);
			mapButton.gameObject.SetActive(false);
			killButton.gameObject.SetActive(false);

			useButton.interactable = false;

			SetRoomText(runner.SessionInfo.Name);
			useButton.gameObject.SetActive(true);
			roomNameText.gameObject.SetActive(true);
			gameSettingsObject.SetActive(true);
			pingText.gameObject.SetActive(true);
			chatButton.gameObject.SetActive(true);
			GameManager.im.nicknameHolder.gameObject.SetActive(true);
		}

		public void InitGame()
		{
			colorUI.Close();
			gameSettingsObject.SetActive(false);
			totalTaskBarObject.SetActive(true);
			taskListObject.SetActive(true);
			reportButton.gameObject.SetActive(true);
			mapButton.gameObject.SetActive(true);
			GameManager.im.nicknameHolder.gameObject.SetActive(false);
			AudioManager.Play("SFX_RoundStarted");
			if (PlayerObject.Local.Controller.IsSuspect)
			{
				killButton.gameObject.SetActive(true);
				ImpostorOverlay();
			}
			else
			{
				CrewmateOverlay();
			}
			Instantiate(GameManager.rm.playerMapIconPrefab, GameManager.im.mapIconHolder).Init(PlayerObject.Local);
		}

		public void SetRoomText(string roomText)
		{
			roomNameText.text = roomText;
		}

		public void InitTaskUI()
		{
			taskHolder.DestroyAllChildren();

			List<TaskBase> uniqueTasks = new List<TaskBase>();
			foreach (TaskStation task in GameManager.Instance.taskDisplayList)
			{
				if (uniqueTasks.Contains(task.taskUI.task))
					continue;
				uniqueTasks.Add(task.taskUI.task);
			}

			foreach (TaskBase task in uniqueTasks)
			{
				Instantiate(taskItem, taskHolder).SetTask(task);
			}

			foreach (var ts in GameManager.Instance.mapData.GetComponentsInChildren<TaskStation>())
			{
				if (PlayerMovement.Local.tasks.Contains(ts))
				{
					Instantiate(GameManager.rm.taskMapIconPrefab, GameManager.im.mapIconHolder).Init(ts);
				}
			}

			UpdateTaskUI();
		}

		public void UpdateTaskUI()
		{
			var tasks = GameManager.Instance.taskDisplayList;

			foreach (TaskItemUI taskItem in transform.GetComponentsInChildren<TaskItemUI>())
			{
				byte numTotal = GameManager.Instance.taskDisplayAmounts[taskItem.Task];
				int count = numTotal - tasks.Count(ts => ts.taskUI.task == taskItem.Task);
				taskItem.SetCount(count, numTotal);
			}

			foreach (TaskMapIcon icon in GameManager.im.mapIconHolder.GetComponentsInChildren<TaskMapIcon>())
			{
				if (tasks.Contains(icon.task) == false)
				{
					Destroy(icon.gameObject);
				}
			}
		}

		public void StartKillTimer()
		{
			StartCoroutine(KillTimerRoutine());
		}

		IEnumerator KillTimerRoutine()
		{
			while (PlayerMovement.Local.KillTimer.RemainingTime(GameManager.Instance.Runner) is float remainingTime && remainingTime > 0)
			{
				killTimerFill.fillAmount = remainingTime / 30f;
				yield return new WaitForEndOfFrame();
			}
			killTimerFill.fillAmount = 0;
		}

		public void ToggleMap()
		{
			mapScreen.SetActive(!mapScreen.activeSelf);
			GameManager.im.mapIconHolder.gameObject.SetActive(mapScreen.activeSelf);
		}

		public void Pause()
		{
			if (GameManager.Instance.Runner?.IsRunning == true)
			{
				UIScreen.Focus(GameManager.Instance.pauseScreen);
			}
			else
			{
				UIScreen.Focus(GameManager.Instance.optionsScreen);
			}
		}

		public void ToggleMute()
		{
			GameManager.vm.Rec.TransmitEnabled ^= true;
			muteButtonOverlay.SetActive(!GameManager.vm.Rec.TransmitEnabled);
		}

		public void CrewmateOverlay()
		{
			SetOverlay($"You are a crewmate. Complete your tasks, but be wary; {GameManager.Instance.Settings.numImposters} of us are not what they seem.", "Crewmate");
			messageNicknameText.text = "";
			messageScreenPanel.color = PlayerObject.Local.GetColor;
			messageScreenGlow.color = Color.blue;
		}

		public void ImpostorOverlay()
		{
			string impostorsText = "";
			int numSuspect = PlayerRegistry.CountWhere(pObj => pObj.Controller.IsSuspect);
			if (numSuspect == 1)
			{
				impostorsText = "You are the only impostor.";
			}
			else if (numSuspect == 2)
			{
				PlayerObject other = PlayerRegistry.Where(pObj => pObj.Controller.IsSuspect && pObj != PlayerObject.Local).Single();
				impostorsText = $"You and {other.GetStyledNickname} are impostors.";
			}
			else
			{
				IEnumerable<PlayerObject> others = PlayerRegistry.Where(pObj => pObj.Controller.IsSuspect && pObj != PlayerObject.Local);
				impostorsText = $"{string.Join(", ", others.Select(pObj => pObj.GetStyledNickname))}, and yourself are impostors.";
			}
			SetOverlay($"{impostorsText}\nKill the crew without getting discovered", "Impostor");
			messageNicknameText.text = "";
			messageScreenPanel.color = PlayerObject.Local.GetColor;
			messageScreenGlow.color = Color.red;

		}

		public void CrewmateWinOverlay()
		{
			SetOverlay("Crewmates Win", PlayerMovement.Local.IsSuspect ? "Lose" : "Victory");
			messageNicknameText.text = "";
			messageScreenPanel.color = PlayerObject.Local.GetColor;
			messageScreenGlow.color = Color.blue;

		}

		public void ImpostorWinOverlay()
		{
			SetOverlay("Impostors Win", PlayerMovement.Local.IsSuspect ? "Victory" : "Lose");

			List<PlayerObject> imposters = new List<PlayerObject>();
			PlayerRegistry.ForEachWhere(pObj => pObj.Controller.IsSuspect, pObj => imposters.Add(pObj));

			if (imposters.Count > 0)
			{
				messageNicknameText.text = string.Join(", ", imposters.Select(pObj => pObj.GetStyledNickname));
				messageScreenPanel.color = imposters[0].GetColor;
			}
			else
			{
				messageNicknameText.text = "There were no impostors.";
				messageScreenPanel.color = Color.red;
			}
			messageScreenGlow.color = Color.red;
		}

		public void EjectOverlay(PlayerObject ejected)
		{
			if (ejected)
			{
				messageNicknameText.text = ejected.Nickname.Value;
				messageScreenPanel.color = ejected.GetColor;
				SetOverlay($"{ejected.Nickname} {(ejected.Controller.IsSuspect ? "was" : "was not")} an imposter", $"Ejected {ejected.Nickname}");
			}
			else
			{
				messageNicknameText.text = "";
				messageScreenPanel.color = PlayerObject.Local.GetColor;
				SetOverlay("You couldn't reach a consensus, so everyone is safe.", "No Vote");
			}
		}

		public void CloseOverlay(float delay = 0)
		{
			StartCoroutine(OverlayTimeout(delay, 0.5f));
		}

		void SetOverlay(string text, string typeText, float timeout = 1)
		{
			StartCoroutine(OverlayFadeIn(2.5f));
			messageText.text = text;
			messageTypeText.text = typeText;
			messageScreenCG.alpha = 0;
		}

		IEnumerator OverlayFadeIn(float duration)
		{
			// Enables the meeting room screen.
			if (GameManager.Instance.HasStateAuthority)
			{
				GameManager.Instance.MeetingScreenActive = true;
			}

			messageScreen.SetActive(true);
			float val = 0;
			while(val < duration)
			{
				val += Time.deltaTime;
				messageScreenCG.alpha = val;
				yield return null;
			}
			messageScreenCG.alpha = 1;
		}

		IEnumerator OverlayTimeout(float delay, float duration)
		{
			if (delay > 0) yield return new WaitForSeconds(delay);

			float val = duration;
			while (val > 0)
			{
				val -= Time.deltaTime;
				messageScreenCG.alpha = val;
				yield return null;
			}
			messageScreenCG.alpha = 0;
			yield return new WaitForSeconds(0.25f);
			messageScreen.SetActive(false);

			// Sets that the meeting room screen is disabled.
			if (GameManager.Instance.HasStateAuthority)
			{
				GameManager.Instance.MeetingScreenActive = false;
			}
		}
	}
}