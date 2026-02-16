using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

public class BreakerBox : NetworkBehaviour, IShockableWithGun
{
	public int leversSwitchedOff = 2;

	public bool isPowerOn;

	public RoundManager roundManager;

	public Animator[] breakerSwitches;

	public AudioSource thisAudioSource;

	public AudioSource breakerBoxHum;

	public AudioClip switchPowerSFX;

	private void Start()
	{
		roundManager = Object.FindObjectOfType<RoundManager>();
	}

	public void SetSwitchesOff()
	{
		roundManager = Object.FindObjectOfType<RoundManager>();
		if (roundManager == null)
		{
			Debug.LogError("Could not find round manager from breaker box script!");
			return;
		}
		leversSwitchedOff = 0;
		int num = roundManager.BreakerBoxRandom.Next(2, breakerSwitches.Length - 1);
		Debug.Log($"loopLimit: {num}");
		for (int i = 0; i < num; i++)
		{
			int num2 = roundManager.BreakerBoxRandom.Next(0, breakerSwitches.Length);
			Debug.Log($"switch {i}: {num2}");
			AnimatedObjectTrigger component = breakerSwitches[num2].gameObject.GetComponent<AnimatedObjectTrigger>();
			if (!component.boolValue)
			{
				Debug.Log("switch was already turned off");
				continue;
			}
			breakerSwitches[num2].SetBool("turnedLeft", value: false);
			component.boolValue = false;
			component.setInitialState = false;
			leversSwitchedOff++;
		}
		Debug.Log("Set lever switches");
	}

	public void SwitchBreaker(bool on)
	{
		Debug.Log("Switch breaker!");
		if (roundManager == null)
		{
			return;
		}
		if (on)
		{
			leversSwitchedOff--;
		}
		else
		{
			leversSwitchedOff++;
		}
		if (base.IsServer)
		{
			Debug.Log("Breaker switched on server.");
			if (leversSwitchedOff <= 0 && !isPowerOn)
			{
				isPowerOn = true;
				roundManager.SwitchPower(on: true);
			}
			else if (leversSwitchedOff > 0 && isPowerOn)
			{
				isPowerOn = false;
				roundManager.SwitchPower(on: false);
			}
		}
		if (leversSwitchedOff <= 0)
		{
			breakerBoxHum.Play();
		}
		else if (leversSwitchedOff == 1)
		{
			breakerBoxHum.Stop();
		}
	}

	void IShockableWithGun.ShockWithGun(PlayerControllerB shockedByPlayer)
	{
		SetSwitchesOff();
		RoundManager.Instance.FlickerLights();
	}

	void IShockableWithGun.StopShockingWithGun()
	{
		RoundManager.Instance.FlickerLights();
	}

	bool IShockableWithGun.CanBeShocked()
	{
		return true;
	}

	float IShockableWithGun.GetDifficultyMultiplier()
	{
		return 0.3f;
	}

	Vector3 IShockableWithGun.GetShockablePosition()
	{
		return base.transform.position;
	}

	Transform IShockableWithGun.GetShockableTransform()
	{
		return base.transform;
	}

	NetworkObject IShockableWithGun.GetNetworkObject()
	{
		return base.NetworkObject;
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	protected internal override string __getTypeName()
	{
		return "BreakerBox";
	}
}
