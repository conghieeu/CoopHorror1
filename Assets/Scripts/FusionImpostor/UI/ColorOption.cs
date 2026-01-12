using UnityEngine;
using UnityEngine.UI;

namespace FusionImpostor
{
	/// <summary>
	/// Represents one of the color options players can select
	/// </summary>
	public class ColorOption : MonoBehaviour
	{
		public Toggle toggle;
		public byte ColorIndex { get; private set; }

		public void Init(ToggleGroup group, byte color)
		{
			ColorIndex = color;
			toggle.image.color = GameManager.rm.playerColours[ColorIndex];
			toggle.onValueChanged.AddListener(selected =>
			{
				if (selected)
				{
					PlayerObject.Local.Rpc_SetColor(ColorIndex);
				}
			});
			toggle.group = group;
		}
	}
}