using System;

[GlobalClass]
public partial class DungeonGrid : Node3D
{
    [ExportGroup("Generator")]
    [Export]
    public int seed = 0;

    [Export]
    public bool randomizeSeedOnStart = true;

    [Export]
    public Vector2I dungeonSize = new Vector2I(70, 70);

    [Export]
    public bool printGeneratorResultToConsole = false;

    [ExportGroup("Cells")]
    [Export]
    public MeshLibrary meshLibrary { get; set; }

    [Export]
    public MeshIdAssignment meshIdAssignment;

    private GridMap gridMap = new();
    private GridGenerator gridGenerator;

    public override void _EnterTree()
    {
        if (meshLibrary == null) GD.PrintErr("Mesh Library not set!");
        if (meshIdAssignment == null) GD.PrintErr("Mesh Id Assignment not set!");
    }

    public override void _Ready()
    {
        AddChild(gridMap);
        gridMap.MeshLibrary = meshLibrary;

        if (randomizeSeedOnStart) seed = Random.Shared.Next(int.MaxValue);
        GD.Print($"Seed: {seed}");

        gridGenerator = new GridGenerator(((uint)dungeonSize.X, (uint)dungeonSize.Y), seed: seed);
        gridGenerator.Automate(printFinalResultToConsole: printGeneratorResultToConsole);

        PlaceFloor();
        PlaceWalls();
        // TODO: Fix Walls!
        // If there are two walls facing the same direction AND another wall
        // 90° from that facing in a 90° different direction from the other two 
        // walls, this should be a "T" junction.
        //
        // Visual example:
        // If we have a placement like this:
        //    W
        // W [W] W
        //    F
        // W = Walls
        // F = Floors
        // [W] = Wall we want to place (and later the "T" junction)
        // 
        // We can categorize the walls into the direction they are facing:
        //      East/West
        // South [South] South
        // 
        // If this is true and verified, we can replace the center [South]
        // tile to a "T junction".
    }

    public void PlaceFloor()
    {
        for (int x = 0; x < gridGenerator.SizeX; x++)
        {
            for (int y = 0; y < gridGenerator.SizeY; y++)
            {
                var cell = gridGenerator.GetCell((x, y));
                if (!cell.IsFloor) continue;

                var pickedCell = meshIdAssignment.Pick(meshIdAssignment.floor);
                gridMap.SetCellItem(
                    new Vector3I(x, 0, y),
                    pickedCell,
                    gridMap.GetOrthogonalIndexFromBasis(
                        Basis.Identity
                    )
                );
            }
        }
    }

    public void PlaceWalls()
    {
        for (int x = 0; x < gridGenerator.SizeX; x++)
        {
            for (int y = 0; y < gridGenerator.SizeY; y++)
            {
                var cell = gridGenerator.GetCell((x, y));
                if (cell.IsFloor) continue;

                // Note X & Y between GridGenerator and Godot are FLIPPED!
                // Normally, North would be (x - 1, y), but for Godot it's
                // (x, y - 1)!
                var cellN = gridGenerator.GetCell((x, y - 1));
                var cellS = gridGenerator.GetCell((x, y + 1));
                var cellW = gridGenerator.GetCell((x - 1, y));
                var cellE = gridGenerator.GetCell((x + 1, y));

                var wallNeighbourCount =
                      (!cellN?.IsFloor ?? true ? 1 : 0)
                    + (!cellS?.IsFloor ?? true ? 1 : 0)
                    + (!cellW?.IsFloor ?? true ? 1 : 0)
                    + (!cellE?.IsFloor ?? true ? 1 : 0);

                // --- Walls ---
                if (wallNeighbourCount == 3)
                {
                    //    F
                    // W [W] W
                    //    W
                    if ((cellN?.IsFloor ?? false) &&
                       (!cellS?.IsFloor ?? true) &&
                       (!cellW?.IsFloor ?? true) &&
                       (!cellE?.IsFloor ?? true))
                    {
                        var pickedCell = meshIdAssignment.Pick(meshIdAssignment.wall);
                        gridMap.SetCellItem(
                            new Vector3I(x, 1, y),
                            pickedCell,
                            gridMap.GetOrthogonalIndexFromBasis(
                                BasisHelper.RotateTwiceAroundY
                            )
                        );
                    }
                    //    W
                    // W [W] F
                    //    W
                    else if ((!cellN?.IsFloor ?? true) &&
                        (!cellS?.IsFloor ?? true) &&
                        (!cellW?.IsFloor ?? true) &&
                        (cellE?.IsFloor ?? false))
                    {
                        var pickedCell = meshIdAssignment.Pick(meshIdAssignment.wall);
                        gridMap.SetCellItem(
                            new Vector3I(x, 1, y),
                            pickedCell,
                            gridMap.GetOrthogonalIndexFromBasis(
                                BasisHelper.RotateThriceAroundY
                            )
                        );
                    }
                    //    W
                    // W [W] W
                    //    F
                    else if ((!cellN?.IsFloor ?? true) &&
                         (cellS?.IsFloor ?? false) &&
                         (!cellW?.IsFloor ?? true) &&
                         (!cellE?.IsFloor ?? true))
                    {
                        var pickedCell = meshIdAssignment.Pick(meshIdAssignment.wall);
                        gridMap.SetCellItem(
                            new Vector3I(x, 1, y),
                            pickedCell,
                            gridMap.GetOrthogonalIndexFromBasis(
                                BasisHelper.DefaultState
                            )
                        );
                    }
                    //    W
                    // F [W] W
                    //    W
                    else if ((!cellN?.IsFloor ?? true) &&
                        (!cellS?.IsFloor ?? true) &&
                        (cellW?.IsFloor ?? false) &&
                        (!cellE?.IsFloor ?? true))
                    {
                        var pickedCell = meshIdAssignment.Pick(meshIdAssignment.wall);
                        gridMap.SetCellItem(
                            new Vector3I(x, 1, y),
                            pickedCell,
                            gridMap.GetOrthogonalIndexFromBasis(
                                BasisHelper.RotateFourTimesAroundY
                            )
                        );
                    }
                }
                // --- Bridge Walls
                else if (wallNeighbourCount == 2)
                {
                    //    F
                    // W [W] W
                    //    F
                    if ((!cellN?.IsFloor ?? true) &&
                        (!cellS?.IsFloor ?? true) &&
                        (cellW?.IsFloor ?? false) &&
                        (cellE?.IsFloor ?? false))
                    {
                        var pickedCell = meshIdAssignment.Pick(meshIdAssignment.wallBridge);
                        gridMap.SetCellItem(
                            new Vector3I(x, 1, y),
                            pickedCell,
                            gridMap.GetOrthogonalIndexFromBasis(
                                BasisHelper.RotateThriceAroundY
                            )
                        );
                    }
                    //    W
                    // F [W] F
                    //    W
                    else if ((cellN?.IsFloor ?? false) &&
                        (cellS?.IsFloor ?? false) &&
                        (!cellW?.IsFloor ?? true) &&
                        (!cellE?.IsFloor ?? true))
                    {
                        var pickedCell = meshIdAssignment.Pick(meshIdAssignment.wallBridge);
                        gridMap.SetCellItem(
                            new Vector3I(x, 1, y),
                            pickedCell,
                            gridMap.GetOrthogonalIndexFromBasis(
                                BasisHelper.RotateOnceAroundY
                            )
                        );
                    }
                    // --- Corners ---
                    //    W
                    // F [W] W
                    //    F
                    else if ((!cellN?.IsFloor ?? true) &&
                        (cellS?.IsFloor ?? false) &&
                        (cellW?.IsFloor ?? false) &&
                        (!cellE?.IsFloor ?? true))
                    {
                        var pickedCell = meshIdAssignment.Pick(meshIdAssignment.wallCornerInwards);
                        gridMap.SetCellItem(
                            new Vector3I(x, 1, y),
                            pickedCell,
                            gridMap.GetOrthogonalIndexFromBasis(
                                BasisHelper.RotateFourTimesAroundY
                            )
                        );
                    }
                    //    F
                    // F [W] W
                    //    W
                    else if ((cellN?.IsFloor ?? false) &&
                        (!cellS?.IsFloor ?? true) &&
                        (cellW?.IsFloor ?? false) &&
                        (!cellE?.IsFloor ?? true))
                    {
                        var pickedCell = meshIdAssignment.Pick(meshIdAssignment.wallCornerInwards);
                        gridMap.SetCellItem(
                            new Vector3I(x, 1, y),
                            pickedCell,
                            gridMap.GetOrthogonalIndexFromBasis(
                                BasisHelper.RotateTwiceAroundY
                            )
                        );
                    }
                    //    F
                    // W [W] F
                    //    W
                    else if ((cellN?.IsFloor ?? false) &&
                        (!cellS?.IsFloor ?? true) &&
                        (!cellW?.IsFloor ?? true) &&
                        (cellE?.IsFloor ?? false))
                    {
                        var pickedCell = meshIdAssignment.Pick(meshIdAssignment.wallCornerInwards);
                        gridMap.SetCellItem(
                            new Vector3I(x, 1, y),
                            pickedCell,
                            gridMap.GetOrthogonalIndexFromBasis(
                                BasisHelper.RotateThriceAroundY
                            )
                        );
                    }
                    //    W
                    // W [W] F
                    //    F
                    else if ((!cellN?.IsFloor ?? true) &&
                        (cellS?.IsFloor ?? false) &&
                        (!cellW?.IsFloor ?? true) &&
                        (cellE?.IsFloor ?? false))
                    {
                        var pickedCell = meshIdAssignment.Pick(meshIdAssignment.wallCornerInwards);
                        gridMap.SetCellItem(
                            new Vector3I(x, 1, y),
                            pickedCell,
                            gridMap.GetOrthogonalIndexFromBasis(
                                BasisHelper.RotateOnceAroundY
                            )
                        );
                    }
                }
                else if (wallNeighbourCount == 1)
                {
                    // --- Inner Double Edge ---
                    //    F
                    // F [W] F
                    //    W
                    if ((cellN?.IsFloor ?? false) &&
                        (!cellS?.IsFloor ?? true) &&
                        (cellW?.IsFloor ?? false) &&
                        (cellE?.IsFloor ?? false))
                    {
                        var pickedCell = meshIdAssignment.Pick(meshIdAssignment.wallCornerInwardsDouble);
                        gridMap.SetCellItem(
                            new Vector3I(x, 1, y),
                            pickedCell,
                            gridMap.GetOrthogonalIndexFromBasis(
                                BasisHelper.RotateThriceAroundY
                            )
                        );
                    }
                    //    F
                    // W [W] F
                    //    F
                    else if ((cellN?.IsFloor ?? false) &&
                        (cellS?.IsFloor ?? false) &&
                        (!cellW?.IsFloor ?? true) &&
                        (cellE?.IsFloor ?? false))
                    {
                        var pickedCell = meshIdAssignment.Pick(meshIdAssignment.wallCornerInwardsDouble);
                        gridMap.SetCellItem(
                            new Vector3I(x, 1, y),
                            pickedCell,
                            gridMap.GetOrthogonalIndexFromBasis(
                                BasisHelper.RotateOnceAroundY
                            )
                        );
                    }
                    //    W
                    // F [W] F
                    //    F
                    else if ((!cellN?.IsFloor ?? true) &&
                        (cellS?.IsFloor ?? false) &&
                        (cellW?.IsFloor ?? false) &&
                        (cellE?.IsFloor ?? false))
                    {
                        var pickedCell = meshIdAssignment.Pick(meshIdAssignment.wallCornerInwardsDouble);
                        gridMap.SetCellItem(
                            new Vector3I(x, 1, y),
                            pickedCell,
                            gridMap.GetOrthogonalIndexFromBasis(
                                BasisHelper.RotateFourTimesAroundY
                            )
                        );
                    }
                    //    F
                    // F [W] W
                    //    F
                    else if ((cellN?.IsFloor ?? false) &&
                        (cellS?.IsFloor ?? false) &&
                        (cellW?.IsFloor ?? false) &&
                        (!cellE?.IsFloor ?? true))
                    {
                        var pickedCell = meshIdAssignment.Pick(meshIdAssignment.wallCornerInwardsDouble);
                        gridMap.SetCellItem(
                            new Vector3I(x, 1, y),
                            pickedCell,
                            gridMap.GetOrthogonalIndexFromBasis(
                                BasisHelper.RotateTwiceAroundY
                            )
                        );
                    }
                }
            }
        }
    }
}
