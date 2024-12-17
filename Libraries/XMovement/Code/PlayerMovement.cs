﻿using Sandbox;

namespace XMovement;

public partial class PlayerMovement : Component
{
	/// <summary>
	/// The current gravity.
	/// </summary>
	[Property, Group( "Config" )] public Vector3 Gravity { get; set; } = new Vector3( 0, 0, 800 );

	/// <summary>
	/// How much friction does the player have?
	/// </summary>
	[Property, Group( "Friction" )] public float BaseFriction { get; set; } = 4.0f;

	/// <summary>
	/// Can we control our movement in the air?
	/// </summary>
	[Property, Group( "Config" )] public float AirControl { get; set; } = 30f;

	[Property, Group( "Acceleration" )] public float AirAcceleration { get; set; } = 50f;

	[Property, Group( "Acceleration" )] public float BaseAcceleration { get; set; } = 10;
	[Property] public MovementFrequencyMode MovementFrequency { get; set; } = MovementFrequencyMode.PerFixedUpdate;
	public enum MovementFrequencyMode
	{
		PerFixedUpdate,
		PerUpdate
	}
	[Sync] public Vector3 WishVelocity { get; set; }
	protected override void OnStart()
	{
		base.OnStart();
		Tags.Add( "player" );
		CreateShadowObjects();
	}

	public void PrepareMovement()
	{
		ResetSimulatedShadow();
	}

	/// <summary>
	/// Move a character, with this velocity
	/// </summary>
	public void Move( bool withWishVelocity = true, bool withGravity = true, float frictionOverride = 0 )
	{
		ApplyAcceleration();
		UpdateSimulatedShadow();

		if ( withWishVelocity )
		{
			if ( IsOnGround )
			{
				Accelerate( WishVelocity );
			}
			else
			{
				Accelerate( WishVelocity.ClampLength( AirControl ) );
			}
		}

		if ( frictionOverride > 0 )
		{
			Velocity = ApplyFriction( Velocity, frictionOverride, 100 );
		}
		else
		{
			Velocity = ApplyFriction( Velocity, GetFriction(), 100 );
		}

		if ( !IsOnGround && withGravity )
			Velocity -= Gravity * Time.Delta * 1f;

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

		CategorizePosition();
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
		if ( !IsOnGround ) return 0f;
		return BaseFriction;
	}
}
