namespace FusionImpostor
{
	using Fusion;
	/// <summary>
	/// Interactable used for opening up the player color selector.
	/// </summary>
	public class ColorKiosk : Interactable
	{
		public ColorSelectionUI colorUI;

		public override bool CanInteract(PlayerMovement player) => true;

		public override void Interact(NetworkObject interactor)
		{
			if (!interactor.HasInputAuthority)
				return;

			colorUI.Open();
		}
	}
}