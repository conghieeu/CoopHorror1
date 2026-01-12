using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

namespace FusionImpostor
{
	/// <summary>
	/// The door's NetworkBehaviour that handles their opening and closing.
	/// </summary>
	public class Door : NetworkBehaviour
	{
		public BoxCollider activationArea;
		public Transform leftDoor;
		public Transform rightDoor;
		public float openDistance = 2f;
		public float openDuration = 1f;
		public float holdDuration = 1f;
		public AnimationCurve openCurve = AnimationCurve.Linear(0, 0, 1, 1);
		public AnimationCurve closeCurve = AnimationCurve.Linear(0, 0, 1, 1);

		[Networked]
		public bool IsOpen { get; set; }

		[Networked, OnChangedRender(nameof(TickActivatedChanged))]
		public int TickActivated { get; set; }

		bool isAnimating = false;

		public override void Render()
		{
			if (isAnimating)
			{
				float animProgress = (float)(Runner.LocalRenderTime - TickActivated * Runner.DeltaTime) / openDuration;

				float sourcePos = IsOpen ? 0 : openDistance;
				float targetPos = IsOpen ? openDistance : 0;
				leftDoor.localPosition = new Vector3(
					Mathf.Lerp(sourcePos, targetPos, 
						(IsOpen ? openCurve : closeCurve).Evaluate(animProgress)),
					0, 0);
				rightDoor.localPosition = new Vector3(
					Mathf.Lerp(-sourcePos, -targetPos,
						(IsOpen ? openCurve : closeCurve).Evaluate(animProgress)),
					0, 0);

				if (animProgress >= 1)
				{
					leftDoor.localPosition = new Vector3(targetPos, 0, 0);
					rightDoor.localPosition = new Vector3(-targetPos, 0, 0);
					isAnimating = false;
				}
			}
		}

		public override void FixedUpdateNetwork()
		{
			// kiểm tra mỗi 10 tick để giảm tải
			if (Runner.Tick % 10 == 0)
			{
				// chỉ cho phép thay đổi trạng thái nếu cửa không đang mở/đóng
				if ((Runner.Tick - TickActivated) * Runner.DeltaTime > openDuration + holdDuration)
				{
					bool playerNearby = PlayerRegistry.Any(p =>
						p.Controller.IsDead == false &&
						activationArea.ClosestPoint(p.transform.position) == p.transform.position);

					if (IsOpen != playerNearby)
					{
						IsOpen = playerNearby;
						TickActivated = Runner.Tick;
					}
				}
			}
		}

		void TickActivatedChanged()
		{
			isAnimating = true;

			AudioManager.Play($"SFX_Door{(IsOpen ? "Open" : "Close")}", AudioManager.MixerTarget.SFX, transform.position);
		}
	}
}