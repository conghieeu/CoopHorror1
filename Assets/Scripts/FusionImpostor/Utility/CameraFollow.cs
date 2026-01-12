using UnityEngine;

namespace FusionImpostor
{
	/// <summary>
	/// Helper class for having the camera following the main, local player.
	/// </summary>
	public class CameraFollow : MonoBehaviour
	{
		public Transform listenerTf;
		public Vector3 posOffset;
		public Vector3 lookOffset;

		private void LateUpdate()
		{
			if (PlayerMovement.Local is PlayerMovement p && p != null)
			{
				transform.position = p.transform.position + posOffset;
				transform.LookAt(p.transform.position + lookOffset);
				listenerTf.position = p.transform.position + lookOffset;
			}
		}
	}
}