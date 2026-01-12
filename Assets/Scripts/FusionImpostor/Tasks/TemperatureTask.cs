using System.Collections;
using TMPro;
using UnityEngine;

namespace FusionImpostor
{
	/// <summary>
	/// task hoàn thành khi nhiệt độ hiện tại bằng với nhiệt độ mục tiêu
	/// </summary>
	public class TemperatureTask : TaskBase
	{
		public override string Name => "Thermostat";
		public TMP_Text logText;
		public TMP_Text readingText;

		int currentTemperature; // dùng để lưu nhiệt độ hiện tại
		int targetTemperature; // dùng để lưu nhiệt độ mục tiêu
		int delta = 0; // dùng để lưu sự thay đổi nhiệt độ

		public override void ResetTask()
		{
			delta = 0;
			targetTemperature = Random.Range(-99, 99);
			readingText.text = $"{targetTemperature}";
			currentTemperature = Random.Range(-99, 99);
			logText.text = $"{currentTemperature}";
		}

		private IEnumerator UpdateTemperature()
		{
			float t = Time.time;
			while (delta != 0)
			{
				float heldFor = (Time.time - t);

				currentTemperature += heldFor < 3 ? delta : delta * 10;
				logText.text = $"{currentTemperature}";

				yield return new WaitForSeconds(heldFor < 1 ? 0.2f : heldFor < 3 ? 0.08f : 0.5f);
			}

			if (currentTemperature == targetTemperature)
			{
				Completed();
			}
		}

		public void SetDelta(int delta)
		{
			this.delta = delta;
			if (delta != 0)
				StartCoroutine(UpdateTemperature());
		}
	}
}