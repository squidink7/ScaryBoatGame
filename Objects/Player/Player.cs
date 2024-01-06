using Godot;
using System;

public partial class Player : RigidBody3D
{
	[Export] int Speed = 20;
	[Export] float SprintSpeedMultiplier = 1.15f;
	[Export] float AirSpeedMultiplier = 0.2f;
	[Export] int JumpHeight = 40;
	[Export] float LookSensitivity = 0.2f;

	float JumpCooldown;
	bool ReadyToJump;

	[ExportGroup("Components")]
	[Export] Camera3D Camera;
	[Export] ShapeCast3D GroundRayCast;

	public override void _Ready()
	{
		DisplayServer.MouseSetMode(DisplayServer.MouseMode.Captured);
	}

	public override void _PhysicsProcess(double delta)
	{	
		ProcessMove(delta);
		ProcessJump();
		ProcessMovementEffects();
	}

	public override void _Input(InputEvent ev)
	{
		if (ev is InputEventMouseMotion mouse)
		{
			// Translate window-relative mouse movements to player camera rotation
			var rotation = Camera.Rotation;
			rotation.X -= Mathf.DegToRad(mouse.Relative.Y) * LookSensitivity;
			rotation.X = Mathf.Clamp(rotation.X, -Mathf.Pi/2, Mathf.Pi/2);
			rotation.Y -= Mathf.DegToRad(mouse.Relative.X) * LookSensitivity;
			Camera.Rotation = rotation;
		}
		else if (ev.IsActionPressed("quit"))
		{
			GetTree().Quit();
		}
	}

	bool IsOnGround() => GroundRayCast.IsColliding();
	
	void ProcessMove(double delta)
	{
		var input = Input.GetVector("move_left", "move_right", "move_forward", "move_backward").Normalized();
		var inputVelocity = new Vector3(input.X, 0, input.Y).Rotated(Vector3.Up, Camera.Rotation.Y);

		float speed = Speed;

		if (Input.IsActionPressed("move_sprint"))
		{
			speed *= SprintSpeedMultiplier;
		}

		inputVelocity *= speed - LinearVelocity.Length();

		if (IsOnGround())
		{
			ApplyCentralForce(inputVelocity * speed * (float)delta*60);
			LinearDamp = 5;
			PhysicsMaterialOverride.Friction = 1;
		}
		else
		{
			speed *= AirSpeedMultiplier;

			var airSpeed = LinearVelocity.Length();

			if (airSpeed >= 1)
			{
				// slow air movement based on how fast player is going
				ApplyCentralForce(inputVelocity * (speed / airSpeed) * (float)delta*60);
			}
			else
			{
				// player isn't moving very fast, don't bother
				ApplyCentralForce(inputVelocity * speed * (float)delta*60);
			}
			LinearDamp = 0;
			PhysicsMaterialOverride.Friction = 0;
		}
	}

	void ProcessJump()
	{
		if (Input.IsActionPressed("move_jump") && IsOnGround())
		{
			// SetAxisVelocity(Vector3.Up * JumpHeight);
			LinearVelocity *= new Vector3(1, 0, 1);
			ApplyCentralImpulse(Vector3.Up * JumpHeight);
		}
	}

	void ProcessMovementEffects()
	{
		// Head bob
		var speed = LinearVelocity.LengthSquared();
		if (speed > 10) speed = 10;

		var bobFrequency = 0.015;
		if (Input.IsActionPressed("move_sprint"))
		{
			bobFrequency = 0.02;
		}

		Camera.Position += Vector3.Up * (float)Math.Sin(Time.GetTicksMsec() * bobFrequency /* speed */) * speed / 1000 /* intensity */;
	}
}
