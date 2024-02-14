using Godot;
using System;

public partial class Player3D : Sprite3D
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		var input = Input.GetVector("move_left", "move_right", "move_up", "move_down");

		Position += new Vector3(input.X * 0.05f, 0, input.Y * 0.05f);
	}
}
