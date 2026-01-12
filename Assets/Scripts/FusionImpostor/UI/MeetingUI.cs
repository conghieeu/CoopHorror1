using Fusion;
using Helpers.Common;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace FusionImpostor
{
	/// <summary>
	/// Networked UI that handles the meeting state of the game.
	/// </summary>
	public class MeetingUI : NetworkBehaviour
	{
		[Header("References")]
		public TMP_Text meetingHeader;
		public Transform votingUIHolder;
		public TMP_Text votingCountdownText;
		public CanvasGroup cg;

		[Header("Prefabs")]
		public PlayerVotingUI votingUIPrefab;

		[Networked]
		TickTimer discussionTimer { get; set; } = TickTimer.None;
		[Networked]
		TickTimer votingTimer { get; set; } = TickTimer.None;

		readonly Dictionary<PlayerRef, byte> votes = new Dictionary<PlayerRef, byte>();
		readonly List<PlayerRef> voted = new List<PlayerRef>();

		void OnEnable()
		{
			cg.interactable = false;
			votingUIHolder.DestroyAllChildren();

			PlayerRegistry.ForEach(p =>
			{
				PlayerVotingUI el = Instantiate(votingUIPrefab, votingUIHolder);
				el.Init(p, this);
			});

			AudioManager.Play("SFX_MeetingCalled", AudioManager.MixerTarget.UI);
			if (GameManager.Instance.MeetingContext)
			{
				meetingHeader.text = $"{GameManager.Instance.MeetingCaller.GetStyledNickname} found {GameManager.Instance.MeetingContext.GetStyledNickname}'s body.";
			}
			else
			{
				meetingHeader.text = $"{GameManager.Instance.MeetingCaller.GetStyledNickname} called an emergency meeting.";
			}
		}

		public override void Spawned()
		{
			Runner.SetIsSimulated(Object, this);
		}

		public override void FixedUpdateNetwork()
		{
			if (GameManager.State.ActiveState is not MeetingStateBehaviour) return;

			base.FixedUpdateNetwork();

			if (cg.interactable == false && discussionTimer.Expired(Runner))
			{
				if (PlayerMovement.Local.IsDead == false)
					cg.interactable = true;

				if (Runner.IsServer)
					discussionTimer = TickTimer.None;
			}

			if (Runner.IsServer && votingTimer.Expired(Runner))
			{
				votingTimer = TickTimer.None;
				EvalVotes();
			}
		}

		public override void Render()
		{
			votingCountdownText.text = !discussionTimer.ExpiredOrNotRunning(Runner) ?
				$"Voting starts in {discussionTimer.RemainingTime(Runner):0}s..." :
				!votingTimer.ExpiredOrNotRunning(Runner) ?
				$"Voting ends in {votingTimer.RemainingTime(Runner):0}s..." :
				"";
		}

		public void Server_SetTimer(Tick tick)
		{
			Debug.Assert(Runner.IsServer);

			int tickDiff = Runner.Tick.Raw - tick.Raw;
			float timeDiff = (tickDiff) * Runner.DeltaTime;
			discussionTimer = TickTimer.CreateFromSeconds(Runner,
				GameManager.Instance.Settings.discussionTime - timeDiff);
			votingTimer = TickTimer.CreateFromSeconds(Runner,
				GameManager.Instance.Settings.discussionTime + GameManager.Instance.Settings.votingTime - timeDiff);
		}

		void Server_EndMeeting()
		{
			// cleanup
			discussionTimer = TickTimer.None;
			votingTimer = TickTimer.None;
			votes.Clear();
			voted.Clear();

			GameManager.State.Server_DelaySetState<VotingResultsStateBehaviour>(1);
		}

		[Rpc(RpcSources.All, RpcTargets.StateAuthority, HostMode = RpcHostMode.SourceIsHostPlayer)]
		public void Rpc_VoteFor(PlayerObject votee, PlayerObject voter)
		{
			if (voted.Contains(voter.Ref))
			{
				Debug.LogWarning($"{voter.GetStyledNickname} double-voted. Ignoring vote");
				return;
			}

			PlayerRef voteeRef = votee ? votee.Ref : PlayerRef.None;

			AddVote(voteeRef);
			voted.Add(voter.Ref);

			Rpc_Server_VoteCast(votee, voter);

			byte count = 0;
			foreach (var v in votes)
			{
				count += v.Value;
			}

			if (count == PlayerRegistry.CountWhere(p => !p.Controller.IsDead))
			{
				EvalVotes();
			}
		}

		void AddVote(PlayerRef key)
		{
			votes[key] = (byte)((votes.ContainsKey(key) ? votes[key] : 0) + 1);
		}

		public void VoteFor(PlayerObject player)
		{
			cg.interactable = false;
			Rpc_VoteFor(player, PlayerObject.Local);

		}

		public void Abstain()
		{
			VoteFor(null);
		}

		void EvalVotes()
		{
			// for any alive players who did not vote, count their vote as a 'no vote'
			PlayerRegistry.ForEachWhere(p => !p.Controller.IsDead && !voted.Contains(p.Ref), p => AddVote(PlayerRef.None));

			List<(PlayerRef player, byte count)> list = new List<(PlayerRef, byte)>();
			foreach (var kvp in votes)
			{
				list.Add((kvp.Key, kvp.Value));
			}
			list = list.OrderByDescending(e => e.count).ToList();

			if (list.Count > 0 && (list.Count == 1 || list[0].count > list[1].count))
			{	// outcome positive: use index 0 as the vote
				GameManager.Instance.VoteResult = PlayerRegistry.GetPlayer(list[0].player);
			}
			else
			{	// outcome negative: there were either no votes or a tie
				GameManager.Instance.VoteResult = null;
			}

			Server_EndMeeting();
		}

		[Rpc(RpcSources.StateAuthority, RpcTargets.All, HostMode = RpcHostMode.SourceIsServer)]
		void Rpc_Server_VoteCast(PlayerObject votee, PlayerObject voter)
		{
			Debug.Log($"{voter.GetStyledNickname} voted for {(votee ? votee.GetStyledNickname : "nobody")}");

			if (votee)
			{
				foreach (var el in votingUIHolder.GetComponentsInChildren<PlayerVotingUI>())
				{
					if (el.player == votee)
					{
						el.AddVote(voter);
						break;
					}
				}
			}
			else
			{
				foreach (var el in votingUIHolder.GetComponentsInChildren<PlayerVotingUI>())
				{
					if (el.player == voter)
					{
						el.Abstain();
						break;
					}
				}
			}
		}
	}
}