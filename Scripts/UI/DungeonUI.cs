public partial class DungeonUI : Control
{
	private Label seedLabel;

	public override void _Ready()
	{
		seedLabel = GetNode<Label>("%SeedLabel");
	}

	public void onDungeonGridFinished(int seed)
	{
		seedLabel.Text = $"Seed: {seed}";
	}
}
