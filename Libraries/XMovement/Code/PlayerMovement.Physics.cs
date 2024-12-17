using Sandbox;
using System;
namespace XMovement;

public partial class PlayerMovement : Component
{
	[Range( 0, 200 )]
	[Property] public float Radius { get; set; } = 16.0f;

	[Range( 0, 200 )]
	[Property] public float Height { get; set; } = 72.0f;

	[Range( 0, 50 )]
	[Property] public float StepHeight { get; set; } = 18.0f;

	[Range( 0, 90 )]
	[Property] public float GroundAngle { get; set; } = 46.0f;

	[Range( 0, 64 )]
	[Property] public float Acceleration { get; set; } = 10.0f;

	/// <summary>
	/// When jumping into walls, should we bounce off or just stop dead?
	/// </summary>
	[Range( 0, 1 )]
	[Property] public float Bounciness { get; set; } = 0.0f;

	/// <summary>
	/// If enabled, determine what to collide with using current project's collision rules for the <see cref="GameObject.Tags"/>
	/// of the containing <see cref="GameObject"/>.
	/// </summary>
	[Property, Group( "Collision" ), Title( "Use Project Collision Rules" )] public bool UseCollisionRules { get; set; } = false;

	[Property, Group( "Collision" ), HideIf( nameof( UseCollisionRules ), true )]
	public TagSet IgnoreLayers { get; set; } = new();

	public BBox BoundingBox => new BBox( new Vector3( -Radius, -Radius, 0 ), new Vector3( Radius, Radius, Height ) );

	[Sync]
	public Vector3 Velocity { get; set; }

	[Sync]
	public Vector3 BaseVelocity { get; set; }

	[Sync]
	public bool IsOnGround { get; set; }

	public GameObject GroundObject { get; set; }
	public GameObject PreviousGroundObject { get; set; }
	public Collider GroundCollider { get; set; }

	protected override void DrawGizmos()
	{
		Gizmo.Draw.LineBBox( BoundingBox );
	}

	/// <summary>
	/// Move up and leave the ground, great for jumping.
	/// </summary>
	public void LaunchUpwards( float amount )
	{
		ClearGround();
		Velocity += Vector3.Up * amount;
		Velocity -= Gravity * Time.Delta * 0.5f;
	}

	/// <summary>
	/// Add acceleration to the current velocity. 
	/// No need to scale by time delta - it will be done inside.
	/// </summary>
	public void Accelerate( Vector3 vector )
	{
		Velocity = Velocity.WithAcceleration( vector, Acceleration * Time.Delta );
	}

	/// <summary>
	/// Apply an amount of friction to the current velocity.
	/// No need to scale by time delta - it will be done inside.
	/// </summary>
	public Vector3 ApplyFriction( Vector3 velocity, float frictionAmount, float stopSpeed = 140.0f )
	{
		var speed = velocity.Length;

		// Bleed off some speed, but if we have less than the bleed
		//  threshold, bleed the threshold amount.
		float control = (speed < stopSpeed) ? stopSpeed : speed;

		// Add the amount to the drop amount.
		var drop = control * Time.Delta * frictionAmount;

		// scale the velocity
		float newspeed = speed - drop;
		if ( newspeed < 0 ) newspeed = 0;
		if ( newspeed == speed ) return velocity;

		newspeed /= speed;
		return velocity * newspeed;
	}

	public SceneTrace BuildTrace( Vector3 from, Vector3 to, float liftFeet = 0.0f )
	{
		var box = BoundingBox;
		if ( liftFeet > 0 )
		{
			from += Vector3.Up * liftFeet;
			box.Maxs = box.Maxs.WithZ( box.Maxs.z - liftFeet );
		}
		box.Mins *= WorldScale;
		box.Maxs *= WorldScale;

		var source = Scene.Trace.Ray( from, to );
		var trace = source.Size( box ).IgnoreGameObjectHierarchy( GameObject );

		return UseCollisionRules ? trace.WithCollisionRules( Tags ) : trace.WithoutTags( IgnoreLayers );
	}

	/// <summary>
	/// Trace the controller's current position to the specified delta
	/// </summary>
	public SceneTraceResult TraceDirection( Vector3 direction )
	{
		return BuildTrace( GameObject.WorldPosition, GameObject.WorldPosition + direction ).Run();
	}
	public void MoveBy( Vector3 delta, bool step )
	{
		if ( step && IsOnGround )
		{
			//Velocity = Velocity.WithZ( 0 );
		}


		var pos = GameObject.WorldPosition;

		var mover = new CharacterControllerHelper( BuildTrace( pos, pos ), pos, delta );
		mover.Bounce = Bounciness;
		mover.MaxStandableAngle = GroundAngle;

		if ( step && IsOnGround )
		{
			mover.TryMoveWithStep( Time.Delta, StepHeight );
		}
		else
		{
			mover.TryMove( Time.Delta );
		}

		WorldPosition = mover.Position;
	}
	void Move( bool step )
	{
		if ( step && IsOnGround )
		{
			//Velocity = Velocity.WithZ( 0 );
		}


		var pos = GameObject.WorldPosition;

		Velocity *= WorldScale;

		Velocity += BaseVelocity;

		var mover = new CharacterControllerHelper( BuildTrace( pos, pos ), pos, Velocity );
		mover.Bounce = Bounciness;
		mover.MaxStandableAngle = GroundAngle;

		if ( step && IsOnGround )
		{
			mover.TryMoveWithStep( Time.Delta, StepHeight * WorldScale.z );
		}
		else
		{
			mover.TryMove( Time.Delta );
		}

		WorldPosition = mover.Position;
		Velocity = mover.Velocity;
		Velocity -= BaseVelocity;

		Velocity /= WorldScale;
	}

	void CategorizePosition()
	{
		var Position = WorldPosition;
		var point = Position + ((Vector3.Down * 2f) * WorldScale.z);
		var vBumpOrigin = Position;
		var wasOnGround = IsOnGround;

		// We're flying upwards too fast, never land on ground
		if ( !IsOnGround && Velocity.z > 140.0f )
		{
			ClearGround();
			return;
		}

		point.z -= (IsOnGround ? StepHeight : 0.1f);

		var pm = BuildTrace( vBumpOrigin, point, 0.0f ).Run();

		//
		// we didn't hit - or the ground is too steep to be ground
		//
		if ( !pm.Hit || Vector3.GetAngle( Vector3.Up, pm.Normal ) > GroundAngle )
		{
			ClearGround();
			return;
		}

		//
		// we are on ground
		//
		PreviousGroundObject = GroundObject;

		IsOnGround = true;
		GroundObject = pm.GameObject;
		GroundCollider = pm.Shape?.Collider as Collider;

		//
		// move to this ground position, if we moved, and hit
		//
		if ( wasOnGround && !pm.StartedSolid && pm.Fraction > 0.0f && pm.Fraction < 1.0f )
		{
			WorldPosition = pm.EndPosition;
		}
	}

	/// <summary>
	/// Disconnect from ground and punch our velocity. This is useful if you want the player to jump or something.
	/// </summary>
	public void Punch( in Vector3 amount )
	{
		ClearGround();
		Velocity += amount;
	}

	public void ClearGround()
	{
		PreviousGroundObject = GroundObject;
		IsOnGround = false;
		GroundObject = default;
		GroundCollider = default;
	}

	/// <summary>
	/// Move from our current position to this target position, but using tracing an sliding.
	/// This is good for different control modes like ladders and stuff.
	/// </summary>
	public void MoveTo( Vector3 targetPosition, bool useStep )
	{
		if ( TryUnstuck() )
			return;

		var pos = WorldPosition;
		var delta = targetPosition - pos;

		var mover = new CharacterControllerHelper( BuildTrace( pos, pos ), pos, delta );
		mover.MaxStandableAngle = GroundAngle;

		if ( useStep )
		{
			mover.TryMoveWithStep( 1.0f, StepHeight );
		}
		else
		{
			mover.TryMove( 1.0f );
		}

		WorldPosition = mover.Position;
	}

	int _stuckTries;

	bool IsStuck()
	{
		var result = BuildTrace( WorldPosition, WorldPosition ).Run();
		return result.StartedSolid;
	}
	[ConVar] public static bool debug_playermovement_unstick { get; set; } = false;
	Transform _previousTransform;
	bool TryUnstuck()
	{

		var result = BuildTrace( WorldPosition, WorldPosition ).Run();

		// Not stuck, we cool
		if ( !result.StartedSolid )
		{
			_stuckTries = 0;
			_previousTransform = Transform.World;
			return false;
		}

		/*using ( Gizmo.Scope( "unstuck", Transform.World ) )
		{
			Gizmo.Draw.Color = Color.Red;
			Gizmo.Draw.LineBBox( BoundingBox );
		}*/

		int AttemptsPerTick = 150;

		var normal = Vector3.Zero;
		var pos = WorldPosition;
		var startpos = WorldPosition;
		for ( int i = 0; i < AttemptsPerTick; i++ )
		{

			// First try the up direction for moving platforms
			if ( i == 0 )
			{
				pos = WorldPosition + Vector3.Up * 0.5f;
			}
			// Try base velocity
			else if ( BaseVelocity.Length > 0 && i < 80 )
			{
				normal = BaseVelocity.Normal * Time.Delta;
				normal.z = Math.Max( 0, normal.z );
				normal *= 0.8f;
				if ( i == 1 )
				{
					pos = WorldPosition + normal;
				}
				else
				{
					var searchdistance = 1f;
					if ( i > 70 ) searchdistance = 2f;
					if ( i > 75 ) searchdistance = 3f;
					normal *= searchdistance;
					pos += normal;
					if ( debug_playermovement_unstick ) DebugOverlay.Line( WorldPosition, pos, Color.Yellow, 2 );
					/*using ( Gizmo.Scope( "unstuck2", new Transform() ) )
					{
						Gizmo.Draw.Color = Color.Yellow;
						Gizmo.Draw.Line( WorldPosition, WorldPosition + normal * 12 );
					}*/

				}
				if ( debug_playermovement_unstick ) DebugOverlay.Box( BoundingBox, Color.Green, 2, Transform.World.WithRotation( Rotation.Identity ) );

				/*using ( Gizmo.Scope( "unstuck3", pos ) )
				{
					Gizmo.Draw.Color = Color.Green;
					Gizmo.Draw.LineBBox( BoundingBox );
				}*/
			}
			else
			{
				if ( debug_playermovement_unstick ) DebugOverlay.Line( WorldPosition, pos, Color.Blue, 2 );
				pos = WorldPosition + Vector3.Random.Normal * (((float)_stuckTries) * 1.25f);
			}
			/*using ( Gizmo.Scope( "unstuck4", new Transform() ) )
			{
				Gizmo.Draw.Color = Color.Blue;
				Gizmo.Draw.Line( WorldPosition, pos );
			}*/
			result = BuildTrace( pos, pos ).Run();

			if ( !result.StartedSolid )
			{
				//Log.Info( $"unstuck after {_stuckTries} tries ({_stuckTries * AttemptsPerTick} tests)" );
				Velocity += normal / Time.Delta;
				WorldPosition = pos;
				_previousTransform = Transform.World;
				return false;
			}
		}

		_stuckTries++;

		_previousTransform = Transform.World;
		return true;
	}
}
