using Fusion.Addons.FSM;

namespace FusionImpostor
{
	/// <summary>
	/// State for handles the game once voting has finished
	/// </summary>
	public class VotingResultsStateBehaviour : StateBehaviour
	{
		/// <summary>
		/// Which state will we go to next
		/// </summary>
		private StateBehaviour nextState;

		/// <summary>
		/// How long we will wait before going to the next state in seconds.
		/// </summary>
		private float nextStateDelay;

		protected override void OnEnterState()
		{
			// If a player has been ejected...
			if (GameManager.Instance.VoteResult is PlayerObject pObj)
			{
				pObj.Controller.IsDead = true;
				pObj.Controller.Server_UpdateDeadState();

				int numCrew = PlayerRegistry.CountWhere(p => !p.Controller.IsDead && p.Controller.IsSuspect == false);
				int numSus = PlayerRegistry.CountWhere(p => !p.Controller.IsDead && p.Controller.IsSuspect == true);

				if (numCrew <= numSus)
				{   // impostors win if they can't be outvoted in a meeting
					WinStateBehaviour winState = Machine.GetState<WinStateBehaviour>();
					winState.crewWin = false;
					nextState = winState;
				}
				else if (numSus == 0)
				{   // crew wins if all impostors have been ejected
					WinStateBehaviour winState = Machine.GetState<WinStateBehaviour>();
					winState.crewWin = true;
					nextState = winState;
				}
				else
				{   // return to play if the game isn't over
					nextState = Machine.GetState<PlayStateBehaviour>();
				}

				nextStateDelay = 3f;
			}
			else
			{   // return to play if there was nobody ejected
				nextState = Machine.GetState<PlayStateBehaviour>();
				nextStateDelay = 2f;
			}
		}
		protected override void OnEnterStateRender()
		{
			GameManager.im.gameUI.EjectOverlay(GameManager.Instance.VoteResult);
		}

		protected override void OnFixedUpdate()
		{
			if (Machine.StateTime > nextStateDelay)
			{
				Machine.ForceActivateState(nextState);
			}
		}
	}
}