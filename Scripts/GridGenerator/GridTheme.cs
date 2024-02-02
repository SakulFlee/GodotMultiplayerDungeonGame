using Godot;

[GlobalClass]
public partial class GridTheme : Resource
{
    [Export]
    public PackedScene FloorTile;

    [Export]
    public PackedScene WallTile;

    [Export]
    public PackedScene WallEdgeTile;

    [Export]
    public PackedScene WallCornerTile;
}