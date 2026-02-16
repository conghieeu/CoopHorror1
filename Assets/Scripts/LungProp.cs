using System.Collections;
using UnityEngine;

public class LungProp : GrabbableObject
{
	public bool isLungPowered = true;

	public bool isLungDocked = true;

	public bool isLungDockedInElevator;

	public RoundManager roundManager;

	public GameObject sparkParticle;

	private Coroutine disconnectAnimation;

	public AudioClip connectSFX;

	public AudioClip disconnectSFX;

	public AudioClip removeFromMachineSFX;

	public float lungDeviceLightIntensity;

	public MeshRenderer lungDeviceMesh;

	private Color emissiveColor;

	public override void EquipItem()
	{
		Debug.Log($"Lung apparatice was grabbed. Is owner: {base.IsOwner}");
		if (isLungDocked)
		{
			isLungDocked = false;
			if (disconnectAnimation != null)
			{
				StopCoroutine(disconnectAnimation);
			}
			disconnectAnimation = StartCoroutine(DisconnectFromMachinery());
		}
		if (isLungDockedInElevator)
		{
			isLungDockedInElevator = false;
			base.gameObject.GetComponent<AudioSource>().PlayOneShot(disconnectSFX);
			_ = isLungPowered;
		}
		base.EquipItem();
	}

	private IEnumerator DisconnectFromMachinery()
	{
		GameObject newSparkParticle = Object.Instantiate(sparkParticle, base.transform.position, Quaternion.identity, null);
		AudioSource thisAudio = base.gameObject.GetComponent<AudioSource>();
		thisAudio.Stop();
		thisAudio.PlayOneShot(disconnectSFX, 0.7f);
		yield return new WaitForSeconds(0.1f);
		newSparkParticle.SetActive(value: true);
		thisAudio.PlayOneShot(removeFromMachineSFX);
		if (base.IsServer && Random.Range(0, 100) < 70 && RoundManager.Instance.minEnemiesToSpawn < 2)
		{
			RoundManager.Instance.minEnemiesToSpawn = 2;
		}
		yield return new WaitForSeconds(1f);
		roundManager.FlickerLights();
		yield return new WaitForSeconds(2.5f);
		roundManager.SwitchPower(on: false);
		roundManager.powerOffPermanently = true;
		yield return new WaitForSeconds(0.75f);
		HUDManager.Instance.RadiationWarningHUD();
	}

	public override void Start()
	{
		base.Start();
		roundManager = Object.FindObjectOfType<RoundManager>();
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	protected internal override string __getTypeName()
	{
		return "LungProp";
	}
}
