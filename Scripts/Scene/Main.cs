using Godot;

public partial class Main : Panel
{
	private Timer timer;

	public override void _EnterTree()
	{
		timer = GetNode<Timer>("%Timer");
		timer.Timeout += () =>
		{
			switchScene("Dungeon3D");
		};
	}

	private void switchScene(string name)
	{
		var err = GetTree().ChangeSceneToFile($"res:///Scenes/{name}.tscn");
		if (err != Error.Ok)
		{
			GD.PrintErr($"Failed to switch scene ({err})");
			return;
		}
	}
}
