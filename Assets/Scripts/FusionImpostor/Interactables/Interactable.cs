using Fusion;

namespace FusionImpostor
{
	/// <summary>
	/// The base class used for all interactables such as the color kiosk, tasks, and dead bodies.
	/// </summary>
	public abstract class Interactable : NetworkBehaviour
	{
		public bool isInteractionInstant = false;
		public bool isGhostAccessible = true;

		public abstract bool CanInteract(PlayerMovement player);

		public abstract void Interact(NetworkObject interactor);
	}
}