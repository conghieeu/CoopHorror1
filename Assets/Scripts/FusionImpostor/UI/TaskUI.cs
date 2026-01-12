using UnityEngine;

namespace FusionImpostor
	{
	/// <summary>
	/// Handles the display of a task once it has been started by the player.
	/// </summary>
	public class TaskUI : MonoBehaviour
	{
		public static TaskUI ActiveUI { get; protected set; } = null;

		public TaskBase task;
		public Sprite taskIcon;

		public void Begin()
		{
			if (task == null)
				task = GetComponent<TaskBase>();
			Debug.Assert(task != null);

			task.ResetTask();
			gameObject.SetActive(true);
			ActiveUI = this;
			AudioManager.Play("SFX_OpenTask", AudioManager.MixerTarget.UI);
		}

		public void Complete()
		{
			CloseTask(false);
		}

		public void CloseTask()
		{
			CloseTask(true);
		}

		public void CloseTask(bool playSound)
		{
			if (playSound) AudioManager.Play("SFX_CloseTask", AudioManager.MixerTarget.UI);
			gameObject.SetActive(false);
			task.ResetTask();
			PlayerObject.Local.Controller.RPC_EndInteraction();
			ActiveUI = null;
		}
	}
}