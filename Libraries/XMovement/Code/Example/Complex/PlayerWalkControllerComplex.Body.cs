using Sandbox;
using Sandbox.Citizen;
namespace XMovement;

public partial class PlayerWalkControllerComplex : Component
{
	[Property, Group( "Body" )] public GameObject Body { get; set; }
	[Property, Group( "Body" )] public SkinnedModelRenderer BodyModelRenderer { get; set; }
	[RequireComponent] public CitizenAnimationHelper AnimationHelper { get; set; }

	protected void SetupBody()
	{
		if ( !Body.IsValid() )
		{
			Body = Scene.CreateObject();
			Body.SetParent( GameObject );
			Body.LocalPosition = Vector3.Zero;
			Body.Name = "Body";
		}
		if ( !BodyModelRenderer.IsValid() )
		{
			BodyModelRenderer = Body.AddComponent<SkinnedModelRenderer>();
			BodyModelRenderer.Model = Model.Load( "models/citizen/citizen.vmdl" );
			AnimationHelper.Target = BodyModelRenderer;
		}
	}
	public void Animate()
	{
		var rotateDifference = 0f;

		var targetAngle = new Angles( 0, EyeAngles.yaw, 0 ).ToRotation();

		var turnSpeed = 0.02f;
		BodyModelRenderer.WorldRotation = Rotation.Slerp( BodyModelRenderer.WorldRotation, targetAngle, Controller.WishVelocity.Length * Time.Delta * turnSpeed );
		BodyModelRenderer.WorldRotation = BodyModelRenderer.WorldRotation.Clamp( targetAngle, 45.0f, out var shuffle ); // lock facing to within 45 degrees of look direction
		rotateDifference = shuffle;

		AnimationHelper.WithWishVelocity( Controller.WishVelocity );
		AnimationHelper.WithVelocity( Controller.Velocity );
		AnimationHelper.IsGrounded = Controller.IsOnGround;
		AnimationHelper.WithLook( EyeAngles.Forward * 100, 1, 1, 1.0f );
		AnimationHelper.DuckLevel = IsCrouching ? 100 : 0;
	}
}
