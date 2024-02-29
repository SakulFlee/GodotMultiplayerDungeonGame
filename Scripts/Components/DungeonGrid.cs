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
    public int roomSizeMinimum { get; set; } = 5;

    [Export]
    public int roomSizeMaximum { get; set; } = 30;

    [Export]
    public int minimumNeighbourWallsForFloor { get; set; } = 4;

    [Export]
    public int areaMinimumCells { get; set; } = 9;

    [Export]
    public Vector2I dungeonSize = new Vector2I(70, 70);

    [Export]
    public double circularRoomChance { get; set; } = 0.33;

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

    [Signal]
    public delegate void DungeonGridFinishedEventHandler(int seed);

    public GridGenerator gridGenerator { get; private set; }

    private GridMap gridMap = new();

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

        // Keep generating until a valid dungeon appears.
        gridGenerator = new GridGenerator(seed: seed)
        {
            roomSizeMinimum = roomSizeMinimum,
            roomSizeMaximum = roomSizeMaximum,
            minimumNeighbourWallsForFloor = minimumNeighbourWallsForFloor,
            areaMinimumCells = areaMinimumCells,
            circularRoomChance = circularRoomChance,
        };
        gridGenerator.Automate(dungeonSize);

        if (printGeneratorResultToConsole)
            gridGenerator.PrintToConsole();

        PlaceGeneratorOutput();
        FixCorners();

        EmitSignal(SignalName.DungeonGridFinished, seed);
    }

    public int PickTile(bool isFloor) => isFloor
        ? floorTiles[gridGenerator.R.Next(0, floorTiles.Count())]
        : wallTiles[gridGenerator.R.Next(0, wallTiles.Count())];

    public void PlaceGeneratorOutput()
    {
        for (var x = 0; x < gridGenerator.gridSize.X; x++)
            for (var y = 0; y < gridGenerator.gridSize.Y; y++)
            {
                var v = new Vector2I(x, y);

                // Any wall that is surrounded cardinally by more than three 
                // walls will be skipped.
                var wallCount = gridGenerator.CountWallNeighbours(
                    v,
                    countNull: true,
                    interCardinalsToo: false);
                if (wallCount > 3) continue;

                int tileToPlace;

                var doorCell = gridGenerator.GetDoorCell(v) ?? false;
                if (doorCell)
                {
                    // There should be a door here. We will handle this later, for now just set a floor tile.
                    tileToPlace = PickTile(true);
                }
                else
                {
                    // Not a door.
                    // Pick a (randomized!) floor or wall tile from the pool,
                    // based on if the cell in the generator is a floor or not. 
                    var cell = gridGenerator.GetFloorCell(v) ?? false;
                    tileToPlace = PickTile(cell);
                }

                for (var a = 0; a < cellSizeOffset.X; a++)
                    for (var b = 0; b < cellSizeOffset.Y; b++)
                        // Set the chosen tile!
                        gridMap.SetCellItem(
                            new Vector3I(
                                x * cellSizeOffset.X + a,
                                0,
                                y * cellSizeOffset.Y + b
                            ),
                            tileToPlace
                        );
            }
    }

    public void FixCorners()
    {
        for (var x = 0; x < gridGenerator.gridSize.X; x++)
            for (var y = 0; y < gridGenerator.gridSize.Y; y++)
            {
                var v = new Vector2I(x, y);

                // Skip if the current cell is a floor
                var cell = gridGenerator.GetFloorCell(v) ?? false; // TODO: true?
                if (cell) continue;

                // Count cardinal walls and skip any cells that aren't
                // surrounded by walls. Visually We want:
                // ?  W  ?
                // W [X] W
                // ?  W  ?
                // ---
                //  ? = Unknown
                //  W = Wall
                // [X] = Cell in question
                var wallCount = gridGenerator.CountWallNeighbours(
                    v,
                    countNull: true,
                    interCardinalsToo: false);
                if (wallCount != 4) continue;

                var cellNE = gridGenerator.GetFloorCell(v + new Vector2I(-1, 1));
                var cellNW = gridGenerator.GetFloorCell(v + new Vector2I(-1, -1));
                var cellSE = gridGenerator.GetFloorCell(v + new Vector2I(1, 1));
                var cellSW = gridGenerator.GetFloorCell(v + new Vector2I(1, -1));

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
                if ((cellNE ?? false) ||
                    (cellSE ?? false) ||
                    (cellSW ?? false) ||
                    (cellNW ?? false))
                {
                    int tileToPlace = PickTile(isFloor: false);

                    for (var a = 0; a < cellSizeOffset.X; a++)
                        for (var b = 0; b < cellSizeOffset.Y; b++)
                            // Set the chosen tile!
                            gridMap.SetCellItem(
                                new Vector3I(
                                    x * cellSizeOffset.X + a,
                                    0,
                                    y * cellSizeOffset.Y + b
                                ),
                                tileToPlace
                            );
                }
            }
    }

    public Vector3 MapToLocal(Vector3I map)
        => gridMap.MapToLocal(map);

    public Vector3I LocalToMap(Vector3 local)
        => gridMap.LocalToMap(local);
}
