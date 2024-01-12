using Godot;

public partial class MainGame : Node
{
	private MatchMaker matchMaker;

	public override void _EnterTree()
	{
		matchMaker = GetNode<MatchMaker>("MatchMaker");

		GetNode<DebugPanel>("%DebugPanel").matchMaker = matchMaker;
		GetNode<ConnectionPanel>("%ConnectionPanel").matchMaker = matchMaker;
	}

	public override void _Ready()
	{
		matchMaker.OnMessageRaw += OnChannelMessageReceived;
	}

	public override void _Process(double delta)
	{
	}

	private void OnChannelMessageReceived(string peerUUID, ushort channel, byte[] data)
	{

	}
}
