using System;
using Godot.Collections;

[GlobalClass]
public partial class DungeonGrid : Node3D
{
    /// <summary>
    /// Set to -1 for randomize on start
    /// </summary>
    [ExportGroup("Generator")]
    [Export]
    public int seed = -1;

    [Export]
    public Vector2I dungeonSize = new Vector2I(70, 70);

    [ExportGroup("Godot")]
    [Export]
    public Vector2I cellSizeOffset = new Vector2I(2, 2);

    [ExportGroup("Cells")]
    [Export]
    public MeshLibrary meshLibrary { get; set; }

    [Export]
    public Array<int> floorTiles = new();

    [Export]
    public Array<int> wallTiles = new();

    [ExportGroup("Debug")]
    [Export]
    public bool printGeneratorResultToConsole = false;

    private GridMap gridMap = new();
    private GridGenerator gridGenerator;

    public override void _EnterTree()
    {
        if (meshLibrary == null) GD.PrintErr("[DungeonGrid] Mesh Library not set!");
        if (floorTiles.Count() == 0) GD.PrintErr("[DungeonGrid] No Floor cells set!");
        if (wallTiles.Count() == 0) GD.PrintErr("[DungeonGrid] No Wall cells set!");
    }

    public override void _Ready()
    {
        AddChild(gridMap);
        gridMap.MeshLibrary = meshLibrary;

        if (seed < 0) seed = Random.Shared.Next(int.MaxValue);
        GD.Print($"[DungeonGrid] Seed: {seed}");

        gridGenerator = new GridGenerator(((uint)dungeonSize.X, (uint)dungeonSize.Y), seed: seed);
        gridGenerator.Automate(printFinalResultToConsole: printGeneratorResultToConsole);

        PlaceGeneratorOutput();
        FixCorners();
    }

    public int PickTile(bool isFloor) => isFloor
        ? floorTiles[Random.Shared.Next(0, floorTiles.Count())]
        : wallTiles[Random.Shared.Next(0, wallTiles.Count())];

    public void PlaceGeneratorOutput()
    {
        var d = new Dictionary
        {
            { 0, 0 },
            { 1, 0 },
            { 2, 0 }
        };

        for (int x = 0; x < gridGenerator.SizeX; x++)
            for (int y = 0; y < gridGenerator.SizeY; y++)
            {
                // Any wall that is surrounded cardinally by more than three 
                // walls will be skipped.
                var wallCount = gridGenerator.CountNeighboursOfType((x, y), isFloor: false, countNull: true, interCardinalsToo: false);
                if (wallCount > 3) continue;

                // Pick a (randomized!) floor or wall tile from the pool,
                // based on if the cell in the generator is a floor or not. 
                var cell = gridGenerator.GetCell((x, y));
                int pickedTile = PickTile(cell.IsFloor);

                d[pickedTile] = d[pickedTile].AsInt32() + 1;

                for (var a = 0; a < cellSizeOffset.X; a++)
                    for (var b = 0; b < cellSizeOffset.Y; b++)
                        // Set the chosen tile!
                        gridMap.SetCellItem(
                            new Vector3I(
                                x * cellSizeOffset.X + a,
                                0,
                                y * cellSizeOffset.Y + b
                            ),
                            pickedTile
                        );
            }

        foreach ((var key, var value) in d)
        {
            GD.Print($"#{key} => {value}");
        }
    }

    public void FixCorners()
    {
        for (int x = 0; x < gridGenerator.SizeX; x++)
            for (int y = 0; y < gridGenerator.SizeY; y++)
            {
                // Skip if the current cell is a floor
                var cell = gridGenerator.GetCell((x, y));
                if (cell.IsFloor) continue;

                // Count cardinal walls and skip any cells that aren't
                // surrounded by walls. Visually We want:
                // ?  W  ?
                // W [X] W
                // ?  W  ?
                // ---
                //  ? = Unknown
                //  W = Wall
                // [X] = Cell in question
                var wallCount = gridGenerator.CountNeighboursOfType((x, y), isFloor: false, countNull: true, interCardinalsToo: false);
                if (wallCount != 4) continue;

                var cellNE = gridGenerator.GetCell((x - 1, y + 1));
                var cellNW = gridGenerator.GetCell((x - 1, y - 1));
                var cellSE = gridGenerator.GetCell((x + 1, y + 1));
                var cellSW = gridGenerator.GetCell((x + 1, y - 1));

                // Now, that we only have walls surrounded by other walls,
                // we can check the inter-cardinals. Visually:
                // [X] W [X]
                //  W  W  W
                // [X] W [X]
                // ---
                //  ? = Unknown
                //  W = Wall
                // [X] = Cells in question
                //
                // If any of those inter-cardinal cells is is a floor,
                // we can set the center cell as a wall to fill the corner.
                // Note: By-default, by the floor placing algorithm, 
                // any walls that have more than three walls surrounding them
                // will be excluded. This removes corner walls which we are
                // trying to restore here for better looks.
                if ((cellNE?.IsFloor ?? false) ||
                    (cellSE?.IsFloor ?? false) ||
                    (cellSW?.IsFloor ?? false) ||
                    (cellNW?.IsFloor ?? false))
                {
                    int pickedTile = PickTile(isFloor: false);

                    for (var a = 0; a < cellSizeOffset.X; a++)
                        for (var b = 0; b < cellSizeOffset.Y; b++)
                            // Set the chosen tile!
                            gridMap.SetCellItem(
                                new Vector3I(
                                    x * cellSizeOffset.X + a,
                                    0,
                                    y * cellSizeOffset.Y + b
                                ),
                                pickedTile
                            );
                }
            }
    }
}
