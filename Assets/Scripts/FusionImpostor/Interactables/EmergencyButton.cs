using Fusion;

namespace FusionImpostor
{
	/// <summary>
	/// Interactable that handled triggering emergency meetings
	/// </summary>
	public class EmergencyButton : Interactable
	{
		public override bool CanInteract(PlayerMovement player)
		{
			return !player.IsDead && player.EmergencyMeetingUses > 0;
		}

		public override void Interact(NetworkObject interactor)
		{
			GameManager.Instance.CallMeeting(Runner.LocalPlayer, null, Runner.Tick);
		}
	}
}