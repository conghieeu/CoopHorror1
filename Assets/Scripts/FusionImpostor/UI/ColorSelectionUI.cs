using Helpers.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FusionImpostor
{
	/// <summary>
	/// Class for updating the player color selector in the pregame state.
	/// </summary>
	public class ColorSelectionUI : MonoBehaviour
	{
		public Transform selectionsHolder;
		public ToggleGroup toggleGroup;
		public ColorOption optionPrefab;

		public Material colorableMaterial;
		public Renderer[] dummyRenderers;

		readonly Dictionary<byte, ColorOption> options = new Dictionary<byte, ColorOption>();
		bool hasInit = false;

		public void Init()
		{
			if (hasInit) return;

			for (byte c = 1; c < GameManager.rm.playerColours.Length; c++)
			{
				ColorOption colorOption = Instantiate(optionPrefab, selectionsHolder);
				colorOption.Init(toggleGroup, c);
				options.Add(c, colorOption);
			}

			dummyRenderers.ForEach(ren =>
			{
				Material[] mats = ren.sharedMaterials;
				for (int i = 0; i < mats.Length; i++)
				{
					if (mats[i] == colorableMaterial)
					{
						mats[i] = PlayerObject.Local.GetComponent<PlayerData>().BodyMaterial;
					}
				}
				ren.sharedMaterials = mats;
			});

			hasInit = true;
		}

		public void Open()
		{
			gameObject.SetActive(true);
			Init();
			options[PlayerObject.Local.ColorIndex].toggle.SetIsOnWithoutNotify(true);

			Refresh();
		}

		public void Refresh()
		{
			foreach (ColorOption opt in options.Values)
			{
				opt.toggle.interactable = false;
			}

			foreach (byte c in PlayerRegistry.GetAvailableColors())
			{
				options[c].toggle.interactable = true;
			}
		}

		public void Close()
		{
			if (gameObject.activeInHierarchy == false) return;
			gameObject.SetActive(false);
			PlayerMovement.Local.RPC_EndInteraction();
		}
	}
}