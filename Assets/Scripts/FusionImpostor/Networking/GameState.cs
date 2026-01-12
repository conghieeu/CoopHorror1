using Fusion;
using Fusion.Addons.FSM;
using System.Collections.Generic;
using UnityEngine;

namespace FusionImpostor
{
	/// <summary>
	/// Handles the main state of the game and contains the networked FSM.
	/// </summary>
	public class GameState : NetworkBehaviour, IStateMachineOwner
	{
		public StateBehaviour ActiveState => stateMachine.ActiveState;
		public bool AllowInput => stateMachine.ActiveStateId == playState.StateId || stateMachine.ActiveStateId == pregameState.StateId;
		public bool IsInGame => stateMachine.ActiveStateId == playState.StateId || stateMachine.ActiveStateId == meetingState.StateId || stateMachine.ActiveStateId == voteResultsState.StateId;

		[Networked] TickTimer Delay { get; set; }
		[Networked] int DelayedStateId { get; set; }

		[Header("Game States Reference")]
		public PregameStateBehaviour pregameState;
		public PlayStateBehaviour playState;
		public MeetingStateBehaviour meetingState;
		public VotingResultsStateBehaviour voteResultsState;
		public WinStateBehaviour winState;

		private StateMachine<StateBehaviour> stateMachine;

		public override void FixedUpdateNetwork()
		{
			if (DelayedStateId >= 0 && Delay.ExpiredOrNotRunning(Runner))
			{
				stateMachine.ForceActivateState(DelayedStateId);
				DelayedStateId = -1;
			}
		}

		public void Server_SetState<T>() where T : StateBehaviour
		{
			Assert.Check(HasStateAuthority, "Clients cannot set GameState");

			Delay = TickTimer.None;
			DelayedStateId = stateMachine.GetState<T>().StateId;
		}

		public void Server_DelaySetState<T>(float delay) where T : StateBehaviour
		{
			Assert.Check(HasStateAuthority, "Clients cannot set GameState");

			Debug.Log($"Delay state change to {nameof(T)} for {delay}s");
			Delay = TickTimer.CreateFromSeconds(Runner, delay);
			DelayedStateId = stateMachine.GetState<T>().StateId;
		}

		public void CollectStateMachines(List<IStateMachine> stateMachines)
		{
			stateMachine = new StateMachine<StateBehaviour>("Game State", pregameState, playState, meetingState, voteResultsState, winState);
			stateMachines.Add(stateMachine);
		}
	}
}