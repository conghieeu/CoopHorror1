using TMPro;
using UnityEngine;

namespace FusionImpostor
{
	/// <summary>
	/// Handles displaying the version of the game.
	/// </summary>
	public class VersionText : MonoBehaviour
	{
		public TMP_Text text;

		void Awake()
		{
			text.text = $"v{Application.version}";
		}
	}
}