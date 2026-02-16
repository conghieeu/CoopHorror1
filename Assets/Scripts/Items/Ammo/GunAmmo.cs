public class GunAmmo : GrabbableObject
{
	public int ammoType;

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	protected internal override string __getTypeName()
	{
		return "GunAmmo";
	}
}
