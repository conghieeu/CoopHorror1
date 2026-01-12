using UnityEngine;

namespace FusionImpostor
{
	/// <summary>
	/// Handles displaying the location of tasks on the map.
	/// </summary>
	public class TaskMapIcon : MonoBehaviour
	{
		[HideInInspector] public Transform target;
		public TaskStation task;
		public Vector3 offset;

		public void Init(TaskStation taskStation)
		{
			task = taskStation;
			target = taskStation.transform;
			transform.position = target.position + offset;
			transform.rotation = Quaternion.LookRotation(-Vector3.up, Vector3.forward);
		}
	}
}