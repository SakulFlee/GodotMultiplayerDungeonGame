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

        CovertAndPlaceGeneratorOutputToGodot();
    }

    public int PickTile(bool floorCell) => floorCell
        ? floorTiles[Random.Shared.Next(floorTiles.Count() - 1)]
        : wallTiles[Random.Shared.Next(wallTiles.Count() - 1)];

    public void CovertAndPlaceGeneratorOutputToGodot()
    {
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
