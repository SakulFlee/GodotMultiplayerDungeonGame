using Godot;

public partial class Player : Node3D
{
	private MatchMaker matchMaker;

	public override void _EnterTree()
	{
		matchMaker = GetNode<MatchMaker>("%MatchMaker");
	}

	public override void _Process(double delta)
	{

	}

	public override void _Input(InputEvent @event)
	{
		GD.Print($"{@event} {@event.ResourceName} {@event.GetClass()}");
	}
}
