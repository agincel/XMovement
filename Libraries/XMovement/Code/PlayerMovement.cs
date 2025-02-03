﻿using Sandbox;

namespace XMovement;

public partial class PlayerMovement : Component
{
	/// <summary>
	/// The current gravity.
	/// </summary>
	[Property, Group( "Config" )] public Vector3 Gravity { get; set; } = new Vector3( 0, 0, 800 );
	
	/// <summary>
	/// The lowered gravity to apply while holding jump, for a limited time.
	/// </summary>
	[Property, Group( "Config" )] public Vector3 JumpHoldGravity { get; set; } = new Vector3( 0, 0, 500 );

	/// <summary>
	/// How long the player can hold jump for more height
	/// </summary>
	[Property, Group( "Config" )] public float JumpHoldDuration { get; set; } = 0.75f;


	/// <summary>
	/// How much friction does the player have?
	/// </summary>
	[Property, Group( "Friction" )] public float BaseFriction { get; set; } = 4.0f;

	/// <summary>
	/// The speed at which we fully come to a stop.
	/// </summary>
	[Property, Group( "Friction" )] public float StopSpeed { get; set; } = 100.0f;

	/// <summary>
	/// Can we control our movement in the air?
	/// </summary>
	[Property, Group( "Config" )] public float AirControl { get; set; } = 30f;

	[Property, Group( "Acceleration" )] public float AirAcceleration { get; set; } = 40f;

	[Property, Group( "Acceleration" )] public float BaseAcceleration { get; set; } = 10;
	[Property] public MovementFrequencyMode MovementFrequency { get; set; } = MovementFrequencyMode.PerFixedUpdate;
	public enum MovementFrequencyMode
	{
		PerFixedUpdate,
		PerUpdate
	}
	[Sync] public Vector3 WishVelocity { get; set; }
	[Sync] public Vector3 AnalogInput { get; set; }
	public bool IsHoldingJump { get; set; }

	[Sync] public Vector3 RespawnPosition { get; set; }

	protected override void OnStart()
	{
		base.OnStart();
		Tags.Add( "player" );
		SetRespawnPosition( WorldPosition );
		CreateShadowObjects();
	}

	public void PrepareMovement()
	{
		UpdateFromSimulatedShadow();
	}

	public void HandleGravity()
	{
		if ( !IsOnGround )
		{
			var g = Gravity;
			if ((Time.Now - TimeLastJumped) < JumpHoldDuration && IsHoldingJump)
			{
				g = JumpHoldGravity;
			}
			Velocity -= g * Time.Delta;
		}
	}

	/// <summary>
	/// Move a character, with this velocity
	/// </summary>
	public void Move( bool withWishVelocity = true, bool withGravity = true, float frictionOverride = 0 )
	{
		RestoreGroundPos();
		ApplyAcceleration();

		if ( withGravity )
		{
			HandleGravity();
		}

		if ( withWishVelocity )
		{
			if ( IsOnGround )
			{
				Accelerate( WishVelocity, BaseAcceleration );
			}
			else
			{
				//Accelerate( WishVelocity.ClampLength( AirControl ), AirAcceleration );
				Accelerate( WishVelocity, AirAcceleration );
			}
		}

		if ( frictionOverride > 0 )
		{
			Velocity = ApplyFriction( Velocity, frictionOverride, StopSpeed );
		}
		else if ( IsOnGround )
		{
			Velocity = ApplyFriction( Velocity, GetFriction(), StopSpeed );
		}


		if ( TryUnstuck() )
			return;

		if ( IsOnGround )
		{
			Move( true );
		}
		else
		{
			Move( false );
		}

		if ( IsOnGround ) StayOnGround();

		CategorizePosition();

		// Finish gravity
		if ( !IsOnGround && withGravity )
			Velocity -= Gravity * Time.Delta * 0.5f;

		ResetSimulatedShadow();
		SaveGroundPos();
		PreviousPosition = WorldPosition;
	}

	private void ApplyAcceleration()
	{
		if ( !IsOnGround ) Acceleration = AirAcceleration;
		else Acceleration = BaseAcceleration;
	}

	/// <summary>
	/// Get the current friction.
	/// </summary>
	/// <returns></returns>
	private float GetFriction()
	{
		if (Input.AnalogMove.LengthSquared <= 0)
		{
			return BaseFriction * 2;
		}
		return BaseFriction;
	}

	public void SetRespawnPosition(Vector3 position)
	{
		RespawnPosition = position;
	}

	public void Respawn()
	{
		Velocity = Vector3.Zero;
		WorldPosition = RespawnPosition;
	}
}
