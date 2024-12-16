﻿using Sandbox;
namespace XMovement;

public partial class PlayerWalkControllerComplex : Component
{
	[Property, Group( "Camera" ), Change( "SetupCamera" )]
	public CameraModes CameraMode { get; set; } = CameraModes.ThirdPerson;
	public enum CameraModes
	{
		FirstPerson,
		ThirdPerson,
		Manual,
	}

	[Property, Group( "Camera" ), ShowIf( "CameraMode", CameraModes.Manual )]
	public CameraComponent Camera { get; set; }


	[Property, Group( "Camera" ), ShowIf( "CameraMode", CameraModes.FirstPerson ), Change( "SetupCamera" )]
	public bool PlayerShadowsOnly { get; set; } = true;


	[Property, Group( "Camera" ), ShowIf( "CameraMode", CameraModes.FirstPerson ), Change( "SetupCamera" )]
	public Vector3 FirstPersonOffset { get; set; } = new Vector3( 0, 0, 0 );


	[Property, Group( "Camera" ), ShowIf( "CameraMode", CameraModes.ThirdPerson ), Change( "SetupCamera" )]
	public Vector3 ThirdPersonOffset { get; set; } = new Vector3( -300, 0, 0 );

	public void SetupCamera()
	{
		if ( CameraMode != CameraModes.Manual && !Camera.IsValid() )
		{
			var cameraobj = Scene.CreateObject();
			cameraobj.SetParent( Head );
			cameraobj.Name = "Camera";
			Camera = cameraobj.AddComponent<CameraComponent>();
			Camera.Enabled = false;
		}
		if ( CameraMode == CameraModes.FirstPerson )
		{
			Camera.LocalPosition = FirstPersonOffset;
			BodyModelRenderer.RenderType = PlayerShadowsOnly ? Sandbox.ModelRenderer.ShadowRenderType.ShadowsOnly : Sandbox.ModelRenderer.ShadowRenderType.On;
		}
		if ( CameraMode == CameraModes.ThirdPerson )
		{
			Camera.LocalPosition = ThirdPersonOffset;
			if ( BodyModelRenderer.RenderType == Sandbox.ModelRenderer.ShadowRenderType.ShadowsOnly && PlayerShadowsOnly )
				BodyModelRenderer.RenderType = Sandbox.ModelRenderer.ShadowRenderType.On;
		}
		if ( !IsProxy && Game.IsPlaying )
		{
			Camera.Enabled = true;
		}
	}
}
