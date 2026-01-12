using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FusionImpostor
{
	/// <summary>
	/// Task in which players must match a sequence given first presented by the game.
	/// </summary>
	public class PatternMatchTask : TaskBase
	{
		static readonly float[] pitches = new float[]
		{
			1.2f, 1.3f, 1.4f,
			0.9f, 1.0f, 1.1f,
			0.5f, 0.6f, 0.7f,
		};

		public override string Name => "Pattern Match";

		public Image[] patternStageLights;
		public Image[] patternSquares;
		public Image[] matchStageLights;
		public Button[] matchButtons;

		[ColorUsage(false, false)]
		public Color
			stageLightOff = Color.black,
			stageLightOn = Color.green,
			patternSquareOff = Color.black,
			patternSquareOn = Color.blue,
			patternSquareWrong = Color.red;

		readonly List<int> pattern = new List<int>();
		readonly List<int> match = new List<int>();

		public override void ResetTask()
		{
			StopAllCoroutines();
			foreach (Image img in patternStageLights)
				img.color = stageLightOff;
			foreach (Image img in patternSquares)
				img.color = patternSquareOff;
			foreach (Image img in matchStageLights)
				img.color = stageLightOff;
			foreach (Button btn in matchButtons)
				btn.interactable = false;
			pattern.Clear();
			match.Clear();

			if (gameObject.activeInHierarchy)
				StartCoroutine(ShowPattern());
		}

		private void OnEnable()
		{
			StartCoroutine(ShowPattern());
		}

		IEnumerator ShowPattern()
		{
			yield return new WaitForSeconds(0.25f);
			match.Clear();

			foreach (Image img in matchStageLights)
				img.color = stageLightOff;

			foreach (Button btn in matchButtons)
				btn.interactable = false;

			yield return new WaitForSeconds(0.75f);

			pattern.Add((byte)Random.Range(0, 9));
			patternStageLights[pattern.Count - 1].color = stageLightOn;

			foreach (byte b in pattern)
			{
				patternSquares[b].color = patternSquareOn;
				AudioManager.Play("simonUI", AudioManager.MixerTarget.UI, null, pitches[b]);
				yield return new WaitForSeconds(0.4f);
				patternSquares[b].color = patternSquareOff;
				yield return new WaitForSeconds(0.1f);
			}

			foreach (Button btn in matchButtons)
				btn.interactable = true;
		}

		public void PressMatch(int index)
		{
			if (matchButtons[index].interactable == false) return;

			match.Add(index);
			AudioManager.Play("simonUI", AudioManager.MixerTarget.UI, null, pitches[index]);
			matchStageLights[match.Count - 1].color = stageLightOn;

			if (index != pattern[match.Count - 1])
				StartCoroutine(WrongInput());
			else if (match.Count == pattern.Count)
			{
				if (pattern.Count == 5)
					StartCoroutine(DelayCompleted());
				else
					StartCoroutine(ShowPattern());
			}
		}

		IEnumerator WrongInput()
		{
			foreach (Button btn in matchButtons)
				btn.interactable = false;

			foreach (Image img in patternSquares)
				img.color = patternSquareWrong;

			yield return new WaitForSeconds(1);
			
			ResetTask();
		}

		IEnumerator DelayCompleted()
		{
			foreach (Image img in patternSquares)
				img.color = patternSquareOn;
			yield return new WaitForSeconds(0.5f);
			Completed();
		}
	}
}