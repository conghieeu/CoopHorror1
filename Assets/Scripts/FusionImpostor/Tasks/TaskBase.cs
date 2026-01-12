using UnityEngine;

namespace FusionImpostor
{
	/// <summary>
	/// Base class for different tasks.
	/// </summary>
	public abstract class TaskBase : MonoBehaviour
	{
		public TaskUI taskUI;
		public abstract string Name { get; }

		public abstract void ResetTask();
		public virtual void Completed()
		{
			AudioManager.Play("taskCompleteUI", AudioManager.MixerTarget.UI);
			GameManager.Instance.CompleteTask();
			taskUI.Complete();
		}
	}
}