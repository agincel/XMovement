public sealed class bridgecreatorlol : Component
{
	GameObject PreviouslyCreatedGameObject;
	GameObject FinalPlank;
	[Property] public int PlankCount { get; set; } = 32;
	[Property] public int Spacing { get; set; } = 8;
	[Property] public int Slack { get; set; } = 8;
	bool updated = false;
	protected override void OnStart()
	{
		if ( !Networking.IsHost ) return;
		GameObject.NetworkSpawn();
		foreach ( var child in GameObject.Children )
		{
			child.Destroy();
		}
		updated = true;
		base.OnStart();
		PreviouslyCreatedGameObject = null;


		for ( int i = 1; i <= PlankCount; i++ )
		{
			PreviouslyCreatedGameObject = CreateCopy( i );
		}


		GameObject.Components.Get<Rigidbody>().PhysicsBody.MotionEnabled = false;
		GameObject.Components.Get<Rigidbody>().PhysicsBody.GravityEnabled = false;
		GameObject.Components.Get<Rigidbody>().PhysicsBody.Sleeping = true;
		GameObject.Components.Get<Rigidbody>().MotionEnabled = false;
		GameObject.Components.Get<ModelCollider>().Static = true;

		PreviouslyCreatedGameObject.Components.Get<Rigidbody>().PhysicsBody.MotionEnabled = false;
		PreviouslyCreatedGameObject.Components.Get<Rigidbody>().PhysicsBody.GravityEnabled = false;
		PreviouslyCreatedGameObject.Components.Get<Rigidbody>().PhysicsBody.Sleeping = true;
		PreviouslyCreatedGameObject.Components.Get<Rigidbody>().MotionEnabled = false;
		PreviouslyCreatedGameObject.Components.Get<ModelCollider>().Static = true;
	}
	public GameObject Duplicate()
	{
		var copy = Game.ActiveScene.CreateObject();
		foreach ( Component component in this.Components.GetAll() )
		{
			if ( component is bridgecreatorlol ) continue;
			var c = copy.Components.Create( TypeLibrary.GetType( component.GetType() ) );
			c.DeserializeImmediately( component.Serialize().AsObject() );
		}
		foreach ( var tag in this.Tags.TryGetAll() )
		{
			copy.Tags.Add( tag );
		}
		copy.WorldRotation = WorldRotation;
		copy.WorldPosition = WorldPosition;
		copy.WorldScale = WorldScale;
		return copy;
	}
	GameObject CreateCopy( int i )
	{

		var copy = Duplicate();

		copy.Parent = GameObject;
		copy.WorldPosition = WorldPosition + WorldRotation.Right * (Slack * i);


		var joint2 = copy.Components.Create<HingeJoint>();
		joint2.EnableCollision = false;
		joint2.MaxAngle = 0;
		joint2.MinAngle = 0;
		joint2.Friction = 1000000;
		joint2.BreakForce = 100000000000;
		joint2.BreakTorque = 100000000000;
		joint2.EnableCollision = false;
		joint2.Body = PreviouslyCreatedGameObject;

		if ( i == 1 || i == PlankCount )
		{
			var joint3 = copy.Components.Create<HingeJoint>();
			joint3.MaxAngle = 0;
			joint3.MinAngle = 0;
			joint3.Friction = 1000000;
			joint3.BreakForce = 100000000000;
			joint3.BreakTorque = 100000000000;
			joint3.EnableCollision = false;
		}

		copy.WorldPosition = WorldPosition + WorldRotation.Right * (Spacing * i);
		copy.WorldRotation = WorldRotation;

		copy.NetworkSpawn();
		return copy;
	}
}
