using UnityEngine;

public class RemoteProp : GrabbableObject
{
	public AudioSource remoteAudio;

	public override void ItemActivate(bool used, bool buttonDown = true)
	{
		remoteAudio.PlayOneShot(remoteAudio.clip);
		WalkieTalkie.TransmitOneShotAudio(remoteAudio, remoteAudio.clip, 0.7f);
		RoundManager.Instance.PlayAudibleNoise(base.transform.position, 8f, 0.4f, 0, isInElevator && StartOfRound.Instance.hangarDoorsClosed);
		Object.FindObjectOfType<ShipLights>().ToggleShipLightsOnLocalClientOnly();
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	protected internal override string __getTypeName()
	{
		return "RemoteProp";
	}
}
