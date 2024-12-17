using Sandbox;
namespace XMovement;

public partial class PlayerWalkControllerComplex : Component
{
	[Property, FeatureEnabled( "Noclip" )] public bool EnableNoclipping { get; set; } = false;
	/// <summary>
	/// The Input Action that noclipping is triggered by.
	/// </summary>
	[Property, InputAction, Feature( "Noclip" )] public string NoclipAction { get; set; } = "Noclip";
	[Sync] public bool IsNoclipping { get; set; }

	/// <summary>
	/// TODO
	/// </summary>
	public virtual void DoNoclipMove()
	{
		Controller.Move( withWishVelocity: false, withGravity: false, frictionOverride: 1 );
	}
}
