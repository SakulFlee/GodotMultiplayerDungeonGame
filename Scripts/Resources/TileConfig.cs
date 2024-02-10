[GlobalClass]
public partial class TileConfig : Resource
{
    [Export(PropertyHint.Range, "0,or_greater")]
    public int Atlas = 0;

    [Export]
    public Vector2I Coordinate = Vector2I.Zero;

    public override string ToString()
    {
        return $"TileConfig {{Atlas={Atlas}, Coordinate={Coordinate}}}";
    }
}
