using Godot;

public partial class Player : CharacterBody3D
{
	public override void _Process(double delta)
	{
		var vector = Input.GetVector("ui_left", "ui_right", "ui_down", "ui_up");
		Velocity = new Vector3(vector.X, 0, vector.Y) * 400.0f;
		GD.Print($"Velocity: {Velocity}");
		MoveAndSlide();
	}
}
