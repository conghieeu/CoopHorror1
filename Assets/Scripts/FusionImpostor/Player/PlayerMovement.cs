using Fusion;
using Fusion.Addons.SimpleKCC;
using Helpers.Bits;
using Helpers.Physics;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace FusionImpostor
{
	/// <summary>
	/// Handles the movement of player
	/// </summary>
	public class PlayerMovement : NetworkBehaviour
	{
		/// <summary>
		/// Reference to the local player
		/// </summary>
		public static PlayerMovement Local { get; protected set; }

		[Tooltip("The rate at which the player looks when rotating")]
		public float lookTurnRate = 1.5f;

		private List<Interactable> nearbyInteractables = new List<Interactable>(16);

		private List<PlayerObject> nearbyPlayers = new List<PlayerObject>(10);

		[Networked, Capacity(10), Tooltip("The Networked list of tasks this player is in charge of.")] 
		public NetworkLinkedList<TaskStation> tasks => default;

		[Networked, Tooltip("Which interactable is currently being interacted with.")]
		public Interactable activeInteractable { get; set; }

		[Networked, OnChangedRender(nameof(OnDeadChanged)), Tooltip("Is the current player dead?")]
		public bool IsDead { get; set; }

		[Networked, Tooltip("Is the current player the impostor?")]
		public bool IsSuspect { get; set; }

		[Networked, Tooltip("How many times has this player pressed the emergy button.")]
		public byte EmergencyMeetingUses { get; set; }

		[Networked, OnChangedRender(nameof(OnKillTimerChanged)), Tooltip("If suspect, this timer determine how long they have to wait before killing again.")]
		public TickTimer KillTimer { get; set; }

		/// <summary>
		/// They layermask used for interactables.
		/// </summary>
		private LayerMask _interactableLayerMask;

		int _playerRadiusLayer;

		public KCC cc { get; protected set; }
		public SimpleKCC simpleCC { get; protected set; }

		public bool TransformLocal = false;

		[Networked]
		public float Speed { get; set; } = 6f;

		/// <summary>
		/// The list of lag compenstated hits.
		/// </summary>
		List<LagCompensatedHit> lagCompensatedHits = new List<LagCompensatedHit>();

		/// <summary>
		/// The current player data.
		/// </summary>
		PlayerData playerData;

		// This will prevent players from killing and calling a meeting at the same time.
		private bool actionPerformed = false;

		private void Awake()
		{
			_interactableLayerMask = LayerMask.GetMask("Interactable", "PlayerRadius");
			_playerRadiusLayer = LayerMask.NameToLayer("PlayerRadius");
			playerData = GetComponent<PlayerData>();
		}

		void OnDeadChanged()
		{
			if (HasInputAuthority)
			{
				if (IsDead && TaskUI.ActiveUI) TaskUI.ActiveUI.CloseTask();

				// show nicknames to ghosts
				if (IsDead) GameManager.im.nicknameHolder.gameObject.SetActive(true);

				// update ghost visibility
				PlayerRegistry.ForEachWhere(p => p.Controller.IsDead, p => p.GetComponent<PlayerData>().SetGhost(true));

				// set voice channels
				if (IsDead)
				{
					GameManager.vm.SetTalkChannel(VoiceManager.GHOST);
					GameManager.vm.JoinListenChannel(VoiceManager.GHOST);
				}
				else
				{
					GameManager.vm.SetTalkChannel(VoiceManager.GLOBAL);
					GameManager.vm.ClearListenChannel(VoiceManager.GHOST);
				}
			}

			playerData.SetGhost(IsDead);

			if (GameManager.State.ActiveState is PlayStateBehaviour && (Local.IsDead || Local.IsSuspect))
			{
				AudioManager.Play("SFX_Kill", AudioManager.MixerTarget.SFX, transform.position);
			}
		}

		void OnKillTimerChanged()
		{
			if (this == Local)
			{
				GameManager.im.gameUI.StartKillTimer();
			}
		}

		/// <summary>
		/// Defines which task this player will be in charge of.
		/// </summary>
		/// <param name="taskStations"></param>
		internal void DefineTasks(List<TaskStation> taskStations)
		{
			if (HasStateAuthority)
			{
				tasks.Clear();
				for (int i = 0; i < taskStations.Count; i++)
				{
					tasks.Add(taskStations[i]);
				}
			}
		}

		public void Server_UpdateDeadState()
		{
			cc.SetColliderLayer(LayerMask.NameToLayer(IsDead ? "Ghost" : "Player"));
			cc.SetCollisionLayerMask(cc.Settings.CollisionLayerMask.Flag("GhostPassable", !IsDead));
			cc.SetCollisionLayerMask(cc.Settings.CollisionLayerMask.Flag("Player", GameManager.Instance.Settings.playerCollision & !IsDead));
		}

		public override void Spawned()
		{
			if (HasInputAuthority)
			{
				Local = this;
			}

			cc = GetComponent<KCC>();
			simpleCC = cc as SimpleKCC;

			if (HasStateAuthority)
			{
				GameSettings settings = GameManager.Instance.Settings;

				int playerLayer = LayerMask.NameToLayer("Player");
				cc.SetColliderLayer(LayerMask.NameToLayer("Player"));
				cc.SetCollisionLayerMask(cc.Settings.CollisionLayerMask.value.OverrideBit(playerLayer, settings.playerCollision));
				Speed = settings.walkSpeed;

				Server_UpdateDeadState();
			}
		}

		public override void Render()
		{
			playerData.UpdateAnimation(this);
		}

		public override void FixedUpdateNetwork()
		{
			bool hasInput = GetInput(out PlayerInput input);

			if (hasInput && input.IsDown(PlayerInputBehaviour.BUTTON_START_GAME))
			{
				GameManager.Instance.Server_StartGame();
			}

			Vector3 direction = default;
			bool canMoveOrUseInteractables = activeInteractable == null && GameManager.Instance.MeetingScreenActive == false && GameManager.Instance.VotingScreenActive == false && hasInput;

			if (canMoveOrUseInteractables)
			{
				// BUTTON_WALK is representing left mouse button
				if (input.IsDown(PlayerInputBehaviour.BUTTON_WALK))
				{
					direction = new Vector3(
						Mathf.Cos((float)input.Yaw * Mathf.Deg2Rad),
						0,
						Mathf.Sin((float)input.Yaw * Mathf.Deg2Rad)
					);
				}
				else
				{
					if (input.IsDown(PlayerInputBehaviour.BUTTON_FORWARD))
					{
						direction += TransformLocal ? transform.forward : Vector3.forward;
					}

					if (input.IsDown(PlayerInputBehaviour.BUTTON_BACKWARD))
					{
						direction -= TransformLocal ? transform.forward : Vector3.forward;
					}

					if (input.IsDown(PlayerInputBehaviour.BUTTON_LEFT))
					{
						direction -= TransformLocal ? transform.right : Vector3.right;
					}

					if (input.IsDown(PlayerInputBehaviour.BUTTON_RIGHT))
					{
						direction += TransformLocal ? transform.right : Vector3.right;
					}

					direction = direction.normalized;
				}
			}

			simpleCC.Move(direction * Speed);

			if (direction != Vector3.zero)
			{
				Quaternion targetQ = Quaternion.AngleAxis(Mathf.Atan2(direction.z, direction.x) * Mathf.Rad2Deg - 90, Vector3.down);
				cc.SetLookRotation(Quaternion.RotateTowards(transform.rotation, targetQ, lookTurnRate * 360 * Runner.DeltaTime));
			}

			// Performs an overlap sphere test to see if the player is close enough to interactables
			int lagHit = Runner.LagCompensation.OverlapSphere(transform.position, cc.Settings.Radius, Object.InputAuthority, lagCompensatedHits, _interactableLayerMask,
				options: HitOptions.IncludePhysX);

			// Can the player report, kill, or use the interactable.
			bool canReport = false, canKill = false, canUse = false;

			// The lists of nearby players and interactables are cleared with every check.
			nearbyInteractables.Clear();
			nearbyPlayers.Clear();

			// Iterates through the results
			for (int i = 0; i < lagHit; i++)
			{
				if (lagCompensatedHits[i].Hitbox is Hitbox hb)
				{
					// We don't bother tryingt to find nearby players if we are the suspect.
					if (IsSuspect && !hb.transform.IsChildOf(transform) && hb.gameObject.layer == _playerRadiusLayer && hb.GetComponentInParent<PlayerObject>() is PlayerObject player)
					{
						nearbyPlayers.Add(player);
						canKill = true;
					}
					continue;
				}

				GameObject hitGameObject = lagCompensatedHits[i].Collider.gameObject;

				if (hitGameObject.TryGetComponent<Interactable>(out var hitInteractable))
				{
					if (!nearbyInteractables.Contains(hitInteractable))
						nearbyInteractables.Add(hitInteractable);

					if (hitInteractable is DeadPlayer)
						canReport = true;
					else
						canUse = hitInteractable.CanInteract(this);
				}
			}

			if (HasInputAuthority)
			{
				GameManager.im.gameUI.reportButton.interactable = canReport;
				GameManager.im.gameUI.killButton.interactable = canKill;
				GameManager.im.gameUI.useButton.interactable = canUse;
			}


			if (!canMoveOrUseInteractables)
				return;

			actionPerformed = false;

			// When pressing the interact button, there's no clear way to know what action is being done, so this order is used.
			if (input.IsDown(PlayerInputBehaviour.BUTTON_REPORT) || input.IsDown(PlayerInputBehaviour.BUTTON_INTERACT))
				TryToReportDeadPlayer();

			if (input.IsDown(PlayerInputBehaviour.BUTTON_USE) || input.IsDown(PlayerInputBehaviour.BUTTON_INTERACT))
				TryToUseStation();

			if (input.IsDown(PlayerInputBehaviour.BUTTON_KILL) || input.IsDown(PlayerInputBehaviour.BUTTON_INTERACT))
				TryKill();
		}
		void TryToUseStation()
		{
			if (actionPerformed || nearbyInteractables.Count <= 0)
				return;

			if (GetNearestTask(out Interactable interactable))
			{
				if (interactable is TaskStation station)
				{
					if (interactable.CanInteract(this) && (!IsDead || station.isGhostAccessible))
					{
						activeInteractable = interactable;
						activeInteractable.Interact(Object);
						if (activeInteractable.isInteractionInstant)
						{
							activeInteractable = null;
						}
						actionPerformed = true;
					}
				}
				else if (interactable is SettingsKiosk && Runner.IsServer)
				{
					activeInteractable = interactable;
					activeInteractable.Interact(Object);
					if (activeInteractable.isInteractionInstant)
					{
						activeInteractable = null;
					}
					actionPerformed = true;
				}
				else
				{
					if (!IsDead || interactable.isGhostAccessible)
					{
						activeInteractable = interactable;
						activeInteractable.Interact(Object);
						if (activeInteractable.isInteractionInstant)
						{
							activeInteractable = null;
						}
						actionPerformed = true;
					}
				}
			}
		}

		void TryToReportDeadPlayer()
		{
			if (actionPerformed)
				return;

			if (GetNearestDeadPlayer(out Interactable interactable) && (!IsDead || interactable.isGhostAccessible))
			{
				activeInteractable = interactable;
				activeInteractable.Interact(Object);
				if (activeInteractable != null && activeInteractable.isInteractionInstant)
					activeInteractable = null;
				actionPerformed = true;
			}
		}

		public bool GetNearestTask(out Interactable result)
		{
			return GetNearestInteractable(false, out result);
		}

		public bool GetNearestDeadPlayer(out Interactable result)
		{
			return GetNearestInteractable(true, out result);
		}
		public bool GetNearestInteractable(bool getDeadPlayers, out Interactable result)
		{
			result = null;
			float dist = float.MaxValue;
			foreach (Interactable intertactable in nearbyInteractables)
			{
				if (intertactable is DeadPlayer != getDeadPlayers)
					continue;

				float sqrMag = Vector3.SqrMagnitude(transform.position - intertactable.transform.position);
				if (sqrMag < dist)
				{
					dist = sqrMag;
					result = intertactable;
				}
			}

			return result != null;
		}

		/// <summary>
		/// Tells both the local player and the state authority that the active interactable is no longer.
		/// </summary>
		[Rpc(RpcSources.All, RpcTargets.StateAuthority | RpcTargets.InputAuthority, Channel = RpcChannel.Reliable)]
		public void RPC_EndInteraction()
		{
			if (activeInteractable && activeInteractable.CanInteract(this) == false && HasInputAuthority)
			{
				GameManager.im.gameUI.useButton.interactable = false;
			}

			if (HasStateAuthority)
				activeInteractable = null;
		}

		public void TryKill()
		{
			// The IsSuspect feels redundant but keeping for now.
			if (actionPerformed || !IsSuspect || !KillTimer.ExpiredOrNotRunning(Runner))
				return;

			for (int i = 0; i < nearbyPlayers.Count; i++)
			{
				if (nearbyPlayers[i].Controller.IsSuspect == false
					&& nearbyPlayers[i].Controller.IsDead == false)
				{
					nearbyPlayers[i].AttemptKill(Object.InputAuthority);
					actionPerformed = true;
					break;
				}
			}
		}
	}
}