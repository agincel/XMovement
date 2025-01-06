using Sandbox;
using Sandbox.VR;
namespace XMovement;

public partial class PlayerWalkControllerComplex : Component
{
	[Property, FeatureEnabled( "VR" )] public bool EnableVR { get; set; } = false;
	[RequireComponent] public VRAnchor Anchor { get; set; }
	[Sync] public bool IsInVR { get; set; } = false;

	private void SetupVR()
	{
		if ( !EnableVR ) return;
		IsInVR = Game.IsRunningInVR;
	}

	private void UpdateVR()
	{
		if ( !IsInVR ) return;
	}
	public virtual void VRPositionHead()
	{
		if ( !IsInVR ) return;
		Camera.WorldRotation = Input.VR.Head.Rotation;
		Camera.WorldPosition = Input.VR.Head.Position;
	}
}
