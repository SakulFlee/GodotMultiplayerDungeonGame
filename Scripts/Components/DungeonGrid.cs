using System;
using System.Collections.Generic;
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

    /// <summary>
    /// Defines how many cells will be placed down inside Godot from a single
    /// tile inside <see cref="GridGenerator"/>.
    /// </summary>
    [Export]
    public Vector2I cellTranslationRatio = new Vector2I(2, 2);

    /// <summary>
    /// How big each cell is and thus how much coordinates get shifted.
    ///
    /// For example:
    /// If this is (1, 1), then any (X, Y) coordinate will be at (X, Y).
    /// But, if this is (2, 2) instead, then (X, Y) will be at (X * 2, Y * 2).
    /// 
    /// <see cref="GridToLocal(Vector3I)"/> and 
    /// <see cref="LocalToGrid(Vector3I)"/> take this into account.
    /// Use these methods for properly translating coordinates.
    /// </summary>
    [Export]
    public Vector3I cellOffset = new Vector3I(2, 2, 3);

    [ExportGroup("Cells")]
    [Export]
    public Array<PackedScene> floorTiles = new();

    [Export]
    public Array<PackedScene> wallTiles = new();

    [ExportGroup("Debug")]
    [Export]
    public bool printGeneratorResultToConsole = true;

    [Signal]
    public delegate void DungeonGridPopulationFinishedEventHandler(int seed);

    public GridGenerator gridGenerator { get; private set; }

    private List<Node3D> instances = new();

    public override void _EnterTree()
    {
        if (floorTiles.Count() == 0) GD.PrintErr("[DungeonGrid] No Floor cells set!");
        if (wallTiles.Count() == 0) GD.PrintErr("[DungeonGrid] No Wall cells set!");
    }

    public override void _Ready()
    {
        if (seed < 0) seed = Random.Shared.Next(int.MaxValue);
        GD.Print($"[DungeonGrid] Seed: {seed}");

        // Generate a dungeon
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
        // FixCorners();

        EmitSignal(SignalName.DungeonGridPopulationFinished, seed);
    }

    public PackedScene PickTile(bool isFloor) => isFloor
        ? floorTiles[gridGenerator.R.Next(0, floorTiles.Count())]
        : wallTiles[gridGenerator.R.Next(0, wallTiles.Count())];

    /// <summary>
    /// Takes a grid-space coordinate (i.e. coordinate inside 
    /// <see cref="GridGenerator"/>) and translates it to a 
    /// local-space coordinate (i.e. local to us).
    /// 
    /// An alternative way of thinking about this is this:
    /// "grid-space" refers to a coordinate used by the
    /// <see cref="GridGenerator"/>.
    /// It is a pure coordinate, without any offsets.
    /// A single digit, be it X or Y, is always the same uniform
    /// scaling and you'd expect one cell to be at (0, 0), the next at (0, 1),
    /// and so on.
    /// 
    /// "local-space" refers to a coordinate WITH offsets.
    /// When placing down a cell, the cells can have different sizes,
    /// making (0, 0) and (0, 1) the same tile (also (1, 0) and (1, 1)).
    /// To avoid this, we convert these pure grid coordinates and apply
    /// offsets to them.
    /// 
    /// <seealso cref="LocalToGrid(Vector3I)"/>
    /// </summary>
    /// <param name="gridCoordinate">Input coordinate, must be grid-space</param>
    /// <returns>Translated coordinate in local-space</returns>
    public Vector3I GridToLocal(Vector3I gridCoordinate) =>
        new Vector3I(
            gridCoordinate.X * cellOffset.X,
            gridCoordinate.Y * cellOffset.Y,
            gridCoordinate.Z * cellOffset.Z);

    /// <summary>
    /// Takes a local-space coordinate (i.e. local to us) and translates 
    /// it to a grid-space coordinate (i.e. coordinate inside 
    /// <see cref="GridGenerator"/>)).
    /// 
    /// An alternative way of thinking about this is this:
    /// "grid-space" refers to a coordinate used by the
    /// <see cref="GridGenerator"/>.
    /// It is a pure coordinate, without any offsets.
    /// A single digit, be it X or Y, is always the same uniform
    /// scaling and you'd expect one cell to be at (0, 0), the next at (0, 1),
    /// and so on.
    /// 
    /// "local-space" refers to a coordinate WITH offsets.
    /// When placing down a cell, the cells can have different sizes,
    /// making (0, 0) and (0, 1) the same tile (also (1, 0) and (1, 1)).
    /// To avoid this, we convert these pure grid coordinates and apply
    /// offsets to them.
    /// 
    /// <seealso cref="GridToLocal(Vector3I)"/>
    /// </summary>
    /// <param name="localCoordinate">Input coordinate, must be local-space</param>
    /// <returns>Translated coordinate in grid-space</returns>
    public Vector3I LocalToGrid(Vector3I localCoordinate) =>
        new Vector3I(
            localCoordinate.X / cellOffset.X,
            localCoordinate.Y / cellOffset.Y,
            localCoordinate.Z / cellOffset.Z);

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

                PackedScene cellToPlace;

                var doorCell = gridGenerator.GetDoorCell(v) ?? false;
                if (doorCell)
                {
                    // There should be a door here. We will handle this later, for now just set a floor tile.
                    cellToPlace = PickTile(true);
                }
                else
                {
                    // Not a door.
                    // Pick a (randomized!) floor or wall tile from the pool,
                    // based on if the cell in the generator is a floor or not. 
                    var cell = gridGenerator.GetFloorCell(v) ?? false;
                    cellToPlace = PickTile(cell);
                }

                // Set the chosen tile.
                for (var a = 0; a < cellTranslationRatio.X; a++)
                    for (var b = 0; b < cellTranslationRatio.Y; b++)
                    {
                        var instance = cellToPlace.Instantiate<Node3D>();
                        instance.Position = new Vector3I(
                            x * cellTranslationRatio.X + a,
                            0,
                            y * cellTranslationRatio.Y + b
                        );
                        AddChild(instance, @internal: InternalMode.Back);
                    }
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
                    var cellToPlace = PickTile(isFloor: false);

                    // Set the chosen tile
                    for (var a = 0; a < cellTranslationRatio.X; a++)
                        for (var b = 0; b < cellTranslationRatio.Y; b++)
                        {
                            var instance = cellToPlace.Instantiate<Node3D>();
                            instance.Position = new Vector3I(
                                x * cellTranslationRatio.X + a,
                                0,
                                y * cellTranslationRatio.Y + b
                            );
                            AddChild(instance, @internal: InternalMode.Back);
                        }
                }
            }
    }
}
