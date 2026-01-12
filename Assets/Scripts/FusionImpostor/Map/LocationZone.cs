using UnityEngine;

namespace FusionImpostor
{
	/// <summary>
	/// Behaviour that handles the changing the display name of the room the player has entered.
	/// </summary>
	[RequireComponent(typeof(Collider))]
	public class LocationZone : MonoBehaviour
	{
		public string displayName;

		private void OnTriggerEnter(Collider other)
		{
			if (other.transform.parent && other.transform.parent.TryGetComponent(out PlayerObject pObj) && pObj == PlayerObject.Local)
			{
				GameManager.im.gameUI.SetRoomText(displayName);
			}
		}
	}
}