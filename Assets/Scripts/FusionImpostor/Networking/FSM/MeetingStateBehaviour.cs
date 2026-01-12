using Fusion.Addons.FSM;

namespace FusionImpostor
{
	/// <summary>
	/// Behaviour that handles the meeting state
	/// </summary>
	public class MeetingStateBehaviour : StateBehaviour
	{
		protected override void OnEnterState()
		{
			GameManager.Instance.VotingScreenActive = true;
		}

		protected override void OnEnterStateRender()
		{
			if (TaskUI.ActiveUI)
				TaskUI.ActiveUI.CloseTask();

			GameManager.im.gameUI.votingScreen.SetActive(true);

			if (PlayerMovement.Local.IsDead == false)
				GameManager.vm.SetTalkChannel(VoiceManager.GLOBAL);
		}

		protected override void OnExitState()
		{
			GameManager.Instance.MeetingCaller = null;

			GameManager.Instance.VotingScreenActive = false;
		}

		protected override void OnExitStateRender()
		{
			GameManager.im.gameUI.votingScreen.SetActive(false);
			if (PlayerMovement.Local.IsDead == false)
				GameManager.vm.SetTalkChannel(VoiceManager.NONE);
		}
	}
}