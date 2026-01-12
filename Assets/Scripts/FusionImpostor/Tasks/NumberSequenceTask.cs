using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace FusionImpostor
{
	/// <summary>
	/// Task where the player must press a series of buttons in a 1 to 10 sequence.
	/// </summary>
	public class NumberSequenceTask : TaskBase
	{
		public override string Name => "Number Sequence";
		public Button[] buttons;
		int lastPushed = 0;

		public void PushButton(int number)
		{
			buttons[number - 1].interactable = false;

			if (number == lastPushed + 1)
			{
				lastPushed = number;
				if (number == buttons.Length)
				{
					Completed();
				}
			}
			else
			{
				ResetTask();
			}
		}

		public override void ResetTask()
		{
			lastPushed = 0;
			foreach(Button button in buttons.ToList().OrderBy(b => Random.value))
			{
				button.interactable = true;
				button.transform.SetAsFirstSibling();
			}
		}
	}
}