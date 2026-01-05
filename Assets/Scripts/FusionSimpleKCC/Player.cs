using UnityEngine;
using Fusion;
using Fusion.Addons.SimpleKCC;

namespace Example
{
	/// <summary>
	/// Player implementation, processes input and controls KCC.
	/// </summary>
	[DefaultExecutionOrder(-5)]
	public sealed class Player : NetworkBehaviour
	{
		public SimpleKCC   KCC;
		public PlayerInput Input;
		public Transform   CameraPivot;
		public Transform   CameraHandle;

		[Header("Movement")]
		public float MoveSpeed          = 10.0f;
		public float JumpImpulse        = 10.0f;
		public float UpGravity          = -25.0f;
		public float DownGravity        = -40.0f;
		public float GroundAcceleration = 55.0f;
		public float GroundDeceleration = 25.0f;
		public float AirAcceleration    = 25.0f;
		public float AirDeceleration    = 1.3f;

		[Networked]
		private Vector3 _moveVelocity { get; set; }

		public override void FixedUpdateNetwork()
		{
			// Apply look rotation delta. This propagates to Transform component immediately.
			KCC.AddLookRotation(Input.CurrentInput.LookRotationDelta);

			// Set default world space input direction and jump impulse.
			Vector3 inputDirection = KCC.TransformRotation * new Vector3(Input.CurrentInput.MoveDirection.x, 0.0f, Input.CurrentInput.MoveDirection.y);
			float   jumpImpulse    = default;

			// Comparing current input to previous input - this prevents glitches when input is lost.
			if (Input.CurrentInput.Actions.WasPressed(Input.PreviousInput.Actions, GameplayInput.JUMP_BUTTON) == true)
			{
				if (KCC.IsGrounded == true)
				{
					// Set world space jump vector.
					jumpImpulse = JumpImpulse;
				}
			}

			// It feels better when the player falls quicker.
			KCC.SetGravity(KCC.RealVelocity.y >= 0.0f ? UpGravity : DownGravity);

			Vector3 desiredMoveVelocity = inputDirection * MoveSpeed;

			if (KCC.ProjectOnGround(desiredMoveVelocity, out Vector3 projectedDesiredMoveVelocity) == true)
			{
				desiredMoveVelocity = Vector3.Normalize(projectedDesiredMoveVelocity) * MoveSpeed;
			}

			float acceleration;
			if (desiredMoveVelocity == Vector3.zero)
			{
				// No desired move velocity - we are stopping.
				acceleration = KCC.IsGrounded == true ? GroundDeceleration : AirDeceleration;
			}
			else
			{
				acceleration = KCC.IsGrounded == true ? GroundAcceleration : AirAcceleration;
			}

			_moveVelocity = Vector3.Lerp(_moveVelocity, desiredMoveVelocity, acceleration * Runner.DeltaTime);

			KCC.Move(_moveVelocity, jumpImpulse);
		}

		private void LateUpdate()
		{
			// Only InputAuthority needs to update camera.
			if (HasInputAuthority == false)
				return;

			// Update camera pivot and transfer properties from camera handle to Main Camera.
			// Render() is executed before KCC because of [OrderBefore(typeof(KCC))].
			// So we have to do it from LateUpdate() - which is called after Render().

			Vector2 pitchRotation = KCC.GetLookRotation(true, false);
			CameraPivot.localRotation = Quaternion.Euler(pitchRotation);

			Camera.main.transform.SetPositionAndRotation(CameraHandle.position, CameraHandle.rotation);
		}
	}
}
