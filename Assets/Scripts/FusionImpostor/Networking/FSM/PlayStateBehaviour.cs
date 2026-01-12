using Fusion;
using Fusion.Addons.FSM;
using System.Collections.Generic;
using UnityEngine;

namespace FusionImpostor
{
	/// <summary>
	/// State entered when a gameplay session starts.
	/// </summary>
	public class PlayStateBehaviour : StateBehaviour
	{
		[SerializeField, Tooltip("The amoount of time, in seconds, that an impostor has to wait before being able to kill a crewmate.")]
		float initialKillTimer = 30;

		protected override void OnEnterState()
		{
			// Sets the initial position for all players
			PlayerRegistry.ForEach(
				obj => obj.Controller.cc.SetPosition(GameManager.Instance.mapData.GetSpawnPosition(obj.Index)));

			// Defines who is the impostier and sets the number of emergency meeting uses, but only if we were previously in the main game.
			if (Machine.PreviousState is PregameStateBehaviour)
			{
				if (GameManager.Instance.Settings.numImposters > 0)
				{
					PlayerObject[] objs = PlayerRegistry.GetRandom(GameManager.Instance.Settings.numImposters);
					foreach (PlayerObject p in objs)
					{
						p.Controller.IsSuspect = true;
						Debug.Log($"[SPOILER]\n\n{p.GetStyledNickname} is suspect");
					}
				}

				PlayerRegistry.ForEach(pObj =>
				{
					pObj.Controller.EmergencyMeetingUses = GameManager.Instance.Settings.numEmergencyMeetings;
				});
			}

			// Defines the tasks for each player
			PlayerRegistry.ForEach(p => p.Controller.DefineTasks(GetRandomTasks(GameManager.Instance.Settings.numTasks)));

			// Sets the kill timer for each suspect
			PlayerRegistry.ForEachWhere(
				p => p.Controller.IsSuspect,
				p => p.Controller.KillTimer = TickTimer.CreateFromSeconds(GameManager.Instance.Runner, initialKillTimer));
		}

		protected override void OnEnterStateRender()
		{
			GameManager.im.gameUI.CloseOverlay(Machine.PreviousState is VotingResultsStateBehaviour ? 0 : 3);

			// Renders the players' local tasks.
			if (Machine.PreviousState is PregameStateBehaviour)
			{
				GameManager.Instance.mapData.hull.SetActive(false);
				GameManager.im.gameUI.InitGame();

				GameManager.Instance.taskDisplayList.Clear();
				foreach (TaskStation playerTask in PlayerRegistry.GetPlayer(Runner.LocalPlayer).Controller.tasks)
					GameManager.Instance.taskDisplayList.Add(playerTask);

				TaskBase[] foundTasks = FindObjectsOfType<TaskBase>(true);

				foreach (TaskBase task in foundTasks)
				{
					GameManager.Instance.taskDisplayAmounts.Add(task, (byte)GameManager.Instance.TaskCount(task));
				}

				GameManager.im.gameUI.InitTaskUI();

				GameManager.vm.SetTalkChannel(VoiceManager.NONE);
			}
		}

		List<TaskStation> GetRandomTasks(byte taskNumber)
		{
			// Gets all of the current tasks stations in the scene and creates a new list.
			List<TaskStation> taskList = new List<TaskStation>(FindObjectsOfType<TaskStation>());

			// Randomizes the task list
			int count = taskList.Count;
			int last = count - 1;
			for (int i = 0; i < last; ++i)
			{
				int r = Random.Range(i, count);
				TaskStation tmp = taskList[i];
				taskList[i] = taskList[r];
				taskList[r] = tmp;
			}

			taskList.RemoveRange(0, taskList.Count - taskNumber);

			return taskList;
		}
	}
}