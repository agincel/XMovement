
public sealed class Respawner : Component
{
	Transform StartTransform;
	protected override void OnStart()
	{
		base.OnStart();
		StartTransform = GameObject.Root.Transform.World;
	}
	protected override void OnUpdate()
	{
		if ( Input.Pressed( "Reload" ) )
		{
			GameObject.Root.Transform.World = StartTransform;
		}
	}
}
