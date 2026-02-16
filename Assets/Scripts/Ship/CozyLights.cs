using UnityEngine;

public class CozyLights : MonoBehaviour
{
	private bool cozyLightsOn;

	public Animator cozyLightsAnimator;

	private void Update()
	{
		if (!(StartOfRound.Instance == null) && StartOfRound.Instance.shipRoomLights.areLightsOn == cozyLightsOn)
		{
			cozyLightsOn = !cozyLightsOn;
			cozyLightsAnimator.SetBool("on", cozyLightsOn);
		}
	}
}
