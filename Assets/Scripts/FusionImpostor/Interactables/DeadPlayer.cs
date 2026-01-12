using Fusion;
using UnityEngine;

namespace FusionImpostor
{
	/// <summary>
	/// Interactable that represents dead players
	/// </summary>
	public class DeadPlayer : Interactable
	{
		[Tooltip("Array of renders that are used to recolor the dead player based on who died.")]
		public Renderer[] modelMeshes;

		[Networked, OnChangedRender(nameof(OnRefChanged))]
		public PlayerRef Ref { get; set; }

		public override void Spawned()
		{
			base.Spawned();
			GameManager.rm.Manage(Object);

			OnRefChanged();
		}

		public void SetColour(Color col)
		{
			Debug.Log($"{this} : set color {col}");

			Material bodyColorMat = Instantiate(GameManager.rm.playerBodyMaterial);
			bodyColorMat.color = col;
			
			foreach (var renderer in modelMeshes)
			{
				Material[] mats = renderer.sharedMaterials;
				for (int i = 0; i < mats.Length; i++)
				{
					if (mats[i] == GameManager.rm.playerBodyMaterial)
					{
						mats[i] = bodyColorMat;
					}
				}
				renderer.sharedMaterials = mats;
			}
		}

		void OnRefChanged()
		{
			PlayerObject pObj = PlayerRegistry.GetPlayer(Ref);
			if (pObj != null)
				SetColour(GameManager.rm.playerColours[pObj.ColorIndex]);
		}

		public override void Interact(NetworkObject interactor)
		{
			GameManager.Instance.CallMeeting(Runner.LocalPlayer, Object, Runner.Tick);
		}

		public override bool CanInteract(PlayerMovement player)
		{
			return !player.IsDead;
		}
	}
}