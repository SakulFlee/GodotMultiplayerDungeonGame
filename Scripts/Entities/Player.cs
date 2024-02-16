public partial class Player : CharacterBody3D
{
	public override void _Process(double delta)
	{
		var vector = Input.GetVector("move_left", "move_right", "move_up", "move_down");
		Velocity = new Vector3(vector.X, 0, vector.Y) * 10.0f;
		GD.Print($"Velocity: {Velocity}");
		MoveAndSlide();
	}
}
