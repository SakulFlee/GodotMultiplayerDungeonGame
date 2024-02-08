[GlobalClass]
public partial class TileConfig : Resource
{
    [Export(PropertyHint.Range, "1,or_greater")]
    public int Atlas = 1;

    [Export]
    public Vector2I Coordinate = Vector2I.Zero;
}