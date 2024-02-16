public partial class Main : Panel
{
	private Timer timer;

	public override void _EnterTree()
	{
		timer = GetNode<Timer>("%Timer");
		timer.Timeout += () =>
		{
			switchGameMode("Dungeon");
		};
	}

	private void switchGameMode(string name)
	{
		var err = GetTree().ChangeSceneToFile($"res:///Scenes/GameModes/{name}.tscn");
		if (err != Error.Ok)
		{
			GD.PrintErr($"Failed to switch scene ({err})");
			return;
		}
	}
}
