using UnityEngine;

namespace FusionImpostor
{
	/// <summary>
	/// Class that rotates the specific tranform by a curve and rate.
	/// </summary>
	public class Spin : MonoBehaviour
	{
		public Vector3 axis = Vector3.up;
		public float rate;
		public AnimationCurve curve;

		private void Update()
		{
			transform.localRotation = Quaternion.AngleAxis(curve.Evaluate(Time.time * rate) * 360, axis);
		}
	}
}