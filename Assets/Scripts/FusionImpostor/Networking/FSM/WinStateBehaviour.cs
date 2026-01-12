using Fusion;
using Fusion.Addons.FSM;
using UnityEngine;

namespace FusionImpostor
{
	/// <summary>
	/// State that handles displaying the win screen.
	/// </summary>
	public class WinStateBehaviour : StateBehaviour
	{
		[Networked, Tooltip("If true, the crew won; otherwise, the impostor(s) did.")]
		public NetworkBool crewWin { get; set; }

		float stateLimit = 3f;

		protected override void OnFixedUpdate()
		{
			if (Machine.StateTime < stateLimit)
				return;

			Machine.ForceActivateState(Machine.GetState<PregameStateBehaviour>());
		}

		protected override void OnEnterStateRender()
		{
			if (crewWin)
				GameManager.im.gameUI.CrewmateWinOverlay();
			else
				GameManager.im.gameUI.ImpostorWinOverlay();

			GameManager.vm.SetTalkChannel(VoiceManager.GLOBAL);
		}

		protected override void OnExitStateRender()
		{
			GameManager.im.gameUI.CloseOverlay();
		}
	}
}