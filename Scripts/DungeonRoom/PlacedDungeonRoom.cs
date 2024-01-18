using Godot;
using SIPSorcery.Media;

public class PlacedDungeonRoom
{
    public Vector2I Location { get; private set; }
    public char[,] Grid { get; private set; }
    public int Width { get; private set; }
    public int Height { get; private set; }

    public PlacedDungeonRoom(DungeonRoomGenerator generator, Vector2I location)
    {
        if (!generator.IsGenerated()) throw new System.Exception("Trying to instantiate PlacedDungeonRoom before room generation");

        Location = location;
        Grid = generator.Grid;
        Width = generator.GetWidth();
        Height = generator.GetHeight();
    }

    public void Print()
    {
        var result = "";
        for (var x = 0; x < Grid.GetUpperBound(0); x++)
        {
            for (var y = 0; y < Grid.GetUpperBound(1); y++)
            {
                result += Grid[x, y];
            }
            result += "\n";
        }
        GD.Print(result);
    }
}
