using Sandbox;
namespace XMovement;

public partial class PlayerWalkControllerComplex : Component
{
	/// <summary>
	/// How quickly does the player move by default?
	/// </summary>
	[Property, Group( "Config" )] public float RunSpeed { get; set; } = 320.0f;
	[Property, Group( "Config" )] public float WalkSpeed { get; set; } = 120.0f;
	[Property, Group( "Config" )] public float CrouchSpeed { get; set; } = 80.0f;

	/// <summary>
	/// How powerful is the player's jump?
	/// </summary>
	[Property, Group( "Config" )] public float JumpPower { get; set; } = 268.3281572999747f;

	[Sync] public bool IsCrouching { get; set; }

	[Sync] public bool IsSlowWalking { get; set; }

	/// <summary>
	/// Do we want to jump next movement update?
	/// </summary>
	public bool WantsJump { get; set; }
	public Vector3 WishMove { get; private set; }

	public void DoMovement()
	{
		Controller.PrepareMovement();

		BuildWishVelocity();
		BuildInput();

		if ( Controller.IsOnGround && WantsJump ) Jump();

		CheckLadder();

		if ( IsTouchingLadder )
		{
			LadderMove();
		}
		else
		{
			Controller.Move();
		}

		ResetFrameInput();

		Animate();
	}

	public void Jump()
	{
		Controller.LaunchUpwards( JumpPower );
		BroadcastPlayerJumped();
	}

	/// <summary>
	/// A network message that lets other users that we've triggered a jump.
	/// </summary>
	[Rpc.Broadcast]
	public void BroadcastPlayerJumped()
	{
		AnimationHelper?.TriggerJump();
	}

	private void BuildFrameInput()
	{
		if ( Input.Pressed( "Jump" ) ) WantsJump = true;
	}
	private void ResetFrameInput()
	{
		WantsJump = false;
	}
	private void BuildInput()
	{
		IsSlowWalking = Input.Down( "Run" );
		IsCrouching = Input.Down( "Duck" ) || !CanUncrouch();
	}

	protected float GetWishSpeed()
	{
		if ( IsCrouching ) return CrouchSpeed;
		if ( IsSlowWalking ) return WalkSpeed;
		return RunSpeed;
	}

	public void BuildWishVelocity()
	{
		WishMove = Input.AnalogMove;

		var rot = EyeAngles.WithPitch( 0f ).ToRotation();

		var wishDirection = WishMove.Normal * rot;
		wishDirection = wishDirection.WithZ( 0 );

		Controller.WishVelocity = wishDirection * GetWishSpeed();
	}

	private bool CanUncrouch()
	{
		var b = Controller.Height;
		if ( !IsCrouching ) return true;
		Controller.Height = 72;
		var tr = Controller.TraceDirection( Vector3.Zero );
		Controller.Height = b;
		return !tr.Hit;
	}
}
