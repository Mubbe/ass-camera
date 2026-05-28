using Godot;
using System;

public partial class Player : CharacterBody3D
{
	[Export] public float Speed = 6.0f;
    //[Export] public Camera3D Camera;
	 private Camera3D Camera;

	public override void _Ready()
	{
    	Camera = GetTree().Root.GetNode("Main/CameraRig/SpringArm3D/Camera3D") as Camera3D;
	}

    public override void _PhysicsProcess(double delta)
    {
		Vector2 input = Input.GetVector(
			"move_left",
			"move_right",
			"move_back",
			"move_forward"
		);

		if (Camera == null)
			return;

		// Get camera forward flattened
		Vector3 forward = -Camera.GlobalTransform.Basis.Z;
		forward.Y = 0;
		forward = forward.Normalized();

		// Get camera right flattened
		Vector3 right = Camera.GlobalTransform.Basis.X;
		right.Y = 0;
		right = right.Normalized();

		// Build move direction
		Vector3 moveDirection =
			(forward * input.Y) +
			(right * input.X);

		if (moveDirection.Length() > 1)
			moveDirection = moveDirection.Normalized();

		// Apply movement
		Velocity = new Vector3(
			moveDirection.X * Speed,
			Velocity.Y,
			moveDirection.Z * Speed
			);

		MoveAndSlide();
    }
}
