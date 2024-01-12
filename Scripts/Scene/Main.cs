using Godot;

public partial class Main : Panel
{
	private Timer timer;

	public override void _EnterTree()
	{
		timer = GetNode<Timer>("%Timer");
		timer.Timeout += () =>
		{
			// switchScene("MainGame/MainGame.tscn");
			switchScene("Generator/TestScene.tscn");
		};
	}

	private void switchScene(string path)
	{
		var err = GetTree().ChangeSceneToFile(path);
		if (err != Error.Ok)
		{
			GD.PrintErr($"Failed to switch scene ({err})");
			return;
		}
	}
}
