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

    }

    // private void MakeBackgroundLayerFromGenerator(GridGenerator generator)
    // {
    //     var possibleTilesFloor = CompileProbabilityList(FloorTiles);
    //     var possibleTilesWallArea = CompileProbabilityList(WallAreaFloorTiles);

    //     for (uint x = 0; x < generator.SizeX; x++)
    //         for (uint y = 0; y < generator.SizeY; y++)
    //         {
    //             TileConfig pickedCell;
    //             var cell = generator.GetCell((x, y));
    //             if (cell.IsFloor)
    //                 pickedCell = possibleTilesFloor[Random.Shared.Next(0, possibleTilesFloor.Count() - 1)];
    //             else
    //                 pickedCell = possibleTilesWallArea[Random.Shared.Next(0, possibleTilesFloor.Count() - 1)];

    //             placeCells(x, y, pickedCell, layerBackground);
    //         }
    // }

    // /// <summary>
    // /// Refer to `WallTileOrientation.drawio.svg` for an overview of what 
    // /// this function is doing...
    // /// </summary>
    // /// <param name="generator"></param>
    // private void MakeWallLayerFromGenerator(GridGenerator generator)
    // {
    //     var possibleTilesFloor = CompileProbabilityList(FloorTiles);
    //     var possibleTilesWallArea = CompileProbabilityList(WallAreaFloorTiles);

    //     var possibleWallBackCenter = CompileProbabilityList(WallBackCenter);
    //     var possibleWallBackRight = CompileProbabilityList(WallBackRight);
    //     var possibleWallEdgeRight = CompileProbabilityList(WallEdgeRight);
    //     var possibleWallFrontRight = CompileProbabilityList(WallFrontRight);
    //     var possibleWallFrontCenter = CompileProbabilityList(WallFrontCenter);
    //     var possibleWallFrontLeft = CompileProbabilityList(WallFrontLeft);
    //     var possibleWallEdgeLeft = CompileProbabilityList(WallEdgeLeft);
    //     var possibleWallBackLeft = CompileProbabilityList(WallBackLeft);

    //     for (uint x = 0; x < generator.SizeX; x++)
    //         for (uint y = 0; y < generator.SizeY; y++)
    //         {
    //             var cell = generator.GetCell((x, y));

    //             // Skip non-walls
    //             if (cell.IsFloor) continue;

    //             var cellN = generator.GetCell((x - 1, y));
    //             var cellS = generator.GetCell((x + 1, y));
    //             var cellW = generator.GetCell((x, y - 1));
    //             var cellE = generator.GetCell((x, y + 1));

    //             TileConfig wallCellTL = null;
    //             TileConfig wallCellTC = null;
    //             TileConfig wallCellTR = null;
    //             TileConfig wallCellML = null;
    //             TileConfig wallCellMC = null;
    //             TileConfig wallCellMR = null;
    //             TileConfig wallCellBL = null;
    //             TileConfig wallCellBC = null;
    //             TileConfig wallCellBR = null;

    //             TileConfig floorCellTL = null;
    //             TileConfig floorCellTC = null;
    //             TileConfig floorCellTR = null;
    //             TileConfig floorCellML = null;
    //             TileConfig floorCellMC = null;
    //             TileConfig floorCellMR = null;
    //             TileConfig floorCellBL = null;
    //             TileConfig floorCellBC = null;
    //             TileConfig floorCellBR = null;

    //             // --- Vertical Walls ---
    //             // #  F  #
    //             // W [W] W
    //             // #  F  #
    //             if (
    //                 (cellN?.IsFloor ?? false) &&
    //                 (cellS?.IsFloor ?? false) &&
    //                 (!cellE?.IsFloor ?? false) &&
    //                 (!cellW?.IsFloor ?? false))
    //             {
    //                 // Walls
    //                 wallCellTL = possibleWallBackCenter[Random.Shared.Next(0, possibleWallBackCenter.Count() - 1)];
    //                 wallCellTC = possibleWallBackCenter[Random.Shared.Next(0, possibleWallBackCenter.Count() - 1)];
    //                 wallCellTR = possibleWallBackCenter[Random.Shared.Next(0, possibleWallBackCenter.Count() - 1)];

    //                 wallCellBL = possibleWallFrontCenter[Random.Shared.Next(0, possibleWallFrontCenter.Count() - 1)];
    //                 wallCellBC = possibleWallFrontCenter[Random.Shared.Next(0, possibleWallFrontCenter.Count() - 1)];
    //                 wallCellBR = possibleWallFrontCenter[Random.Shared.Next(0, possibleWallFrontCenter.Count() - 1)];

    //                 // // Floors
    //                 floorCellTL = possibleTilesFloor[Random.Shared.Next(0, possibleTilesFloor.Count() - 1)];
    //                 floorCellTC = possibleTilesFloor[Random.Shared.Next(0, possibleTilesFloor.Count() - 1)];
    //                 floorCellTR = possibleTilesFloor[Random.Shared.Next(0, possibleTilesFloor.Count() - 1)];
    //             }
    //             // #  W  #
    //             // W [W] W
    //             // #  F  #
    //             else if (
    //                 (!cellN?.IsFloor ?? false) &&
    //                 (cellS?.IsFloor ?? false) &&
    //                 (!cellE?.IsFloor ?? false) &&
    //                 (!cellW?.IsFloor ?? false))
    //             {
    //                 wallCellBL = possibleWallFrontCenter[Random.Shared.Next(0, possibleWallFrontCenter.Count() - 1)];
    //                 wallCellBC = possibleWallFrontCenter[Random.Shared.Next(0, possibleWallFrontCenter.Count() - 1)];
    //                 wallCellBR = possibleWallFrontCenter[Random.Shared.Next(0, possibleWallFrontCenter.Count() - 1)];
    //             }
    //             // #  F  #
    //             // W [W] W
    //             // #  W  #
    //             else if (
    //                 (cellN?.IsFloor ?? false) &&
    //                 (!cellS?.IsFloor ?? false) &&
    //                 (!cellE?.IsFloor ?? false) &&
    //                 (!cellW?.IsFloor ?? false))
    //             {
    //                 wallCellTL = possibleWallBackCenter[Random.Shared.Next(0, possibleWallBackCenter.Count() - 1)];
    //                 wallCellTC = possibleWallBackCenter[Random.Shared.Next(0, possibleWallBackCenter.Count() - 1)];
    //                 wallCellTR = possibleWallBackCenter[Random.Shared.Next(0, possibleWallBackCenter.Count() - 1)];

    //                 floorCellTL = possibleTilesFloor[Random.Shared.Next(0, possibleTilesFloor.Count() - 1)];
    //                 floorCellTC = possibleTilesFloor[Random.Shared.Next(0, possibleTilesFloor.Count() - 1)];
    //                 floorCellTR = possibleTilesFloor[Random.Shared.Next(0, possibleTilesFloor.Count() - 1)];
    //             }
    //             // --- Horizontal Walls ---
    //             // #  W  #
    //             // F [W] F
    //             // #  W  #
    //             else if (
    //                 (!cellN?.IsFloor ?? false) &&
    //                 (!cellS?.IsFloor ?? false) &&
    //                 (cellE?.IsFloor ?? false) &&
    //                 (cellW?.IsFloor ?? false))
    //             {
    //                 // Walls
    //                 wallCellTL = possibleWallEdgeRight[Random.Shared.Next(0, possibleWallEdgeRight.Count() - 1)];
    //                 wallCellML = possibleWallEdgeRight[Random.Shared.Next(0, possibleWallEdgeRight.Count() - 1)];
    //                 wallCellBL = possibleWallEdgeRight[Random.Shared.Next(0, possibleWallEdgeRight.Count() - 1)];

    //                 wallCellTR = possibleWallEdgeLeft[Random.Shared.Next(0, possibleWallEdgeLeft.Count() - 1)];
    //                 wallCellMR = possibleWallEdgeLeft[Random.Shared.Next(0, possibleWallEdgeLeft.Count() - 1)];
    //                 wallCellBR = possibleWallEdgeLeft[Random.Shared.Next(0, possibleWallEdgeLeft.Count() - 1)];
    //             }
    //             // #  W  #
    //             // W [W] F
    //             // #  W  #
    //             else if (
    //                 (!cellN?.IsFloor ?? false) &&
    //                 (!cellS?.IsFloor ?? false) &&
    //                 (cellE?.IsFloor ?? false) &&
    //                 (!cellW?.IsFloor ?? false))
    //             {
    //                 // Walls
    //                 wallCellTR = possibleWallEdgeLeft[Random.Shared.Next(0, possibleWallEdgeLeft.Count() - 1)];
    //                 wallCellMR = possibleWallEdgeLeft[Random.Shared.Next(0, possibleWallEdgeLeft.Count() - 1)];
    //                 wallCellBR = possibleWallEdgeLeft[Random.Shared.Next(0, possibleWallEdgeLeft.Count() - 1)];
    //             }
    //             // #  W  #
    //             // F [W] W
    //             // #  W  #
    //             else if (
    //                 (!cellN?.IsFloor ?? false) &&
    //                 (!cellS?.IsFloor ?? false) &&
    //                 (!cellE?.IsFloor ?? false) &&
    //                 (cellW?.IsFloor ?? false))
    //             {
    //                 // Walls
    //                 wallCellTL = possibleWallEdgeRight[Random.Shared.Next(0, possibleWallEdgeRight.Count() - 1)];
    //                 wallCellML = possibleWallEdgeRight[Random.Shared.Next(0, possibleWallEdgeRight.Count() - 1)];
    //                 wallCellBL = possibleWallEdgeRight[Random.Shared.Next(0, possibleWallEdgeRight.Count() - 1)];
    //             }
    //             // --- Corners ---
    //             // #  F  #
    //             // F [W] W
    //             // #  W  #
    //             else if (
    //                 (cellN?.IsFloor ?? false) &&
    //                 (!cellS?.IsFloor ?? false) &&
    //                 (!cellE?.IsFloor ?? false) &&
    //                 (cellW?.IsFloor ?? false))
    //             {
    //                 // Walls
    //                 wallCellTC = possibleWallBackCenter[Random.Shared.Next(0, possibleWallBackCenter.Count() - 1)];
    //                 wallCellTR = possibleWallBackCenter[Random.Shared.Next(0, possibleWallBackCenter.Count() - 1)];

    //                 wallCellTL = possibleWallEdgeRight[Random.Shared.Next(0, possibleWallEdgeRight.Count() - 1)];
    //                 wallCellML = possibleWallEdgeRight[Random.Shared.Next(0, possibleWallEdgeRight.Count() - 1)];
    //                 wallCellBL = possibleWallEdgeRight[Random.Shared.Next(0, possibleWallEdgeRight.Count() - 1)];
    //             }

    //             // // Front Left
    //             // else if (cellN != null && !cellN.IsFloor &&
    //             //     (cellS == null || cellS.IsFloor) &&
    //             //     cellE != null && !cellE.IsFloor &&
    //             //     (cellW == null || cellW.IsFloor))
    //             // {
    //             //     tileMap.SetCell(layerWalls, position, WallFrontLeft.First().Atlas, WallFrontLeft.First().Coordinate);
    //             // }
    //             // // Front Right
    //             // else if (cellN != null && !cellN.IsFloor &&
    //             //     (cellS == null || cellS.IsFloor) &&
    //             //     (cellE == null || cellE.IsFloor) &&
    //             //     cellW != null && !cellW.IsFloor)
    //             // {
    //             //     tileMap.SetCell(layerWalls, position, WallFrontRight.First().Atlas, WallFrontRight.First().Coordinate);
    //             // }
    //             // // Back Center
    //             // else if (cellN != null && cellN.IsFloor &&
    //             //     (cellS == null || !cellS.IsFloor) &&
    //             //     cellE != null && !cellE.IsFloor &&
    //             //     cellW != null && !cellW.IsFloor)
    //             // {
    //             //     tileMap.SetCell(layerWalls, position, WallBackCenter.First().Atlas, WallBackCenter.First().Coordinate);
    //             // }
    //             // // Back Left
    //             // else if ((cellN == null || cellN.IsFloor) &&
    //             //     cellS != null && !cellS.IsFloor &&
    //             //     cellE != null && !cellE.IsFloor &&
    //             //     (cellW == null || cellW.IsFloor))
    //             // {
    //             //     tileMap.SetCell(layerWalls, position, WallFrontLeft.First().Atlas, WallFrontLeft.First().Coordinate);
    //             // }
    //             // // Back Right
    //             // else if ((cellN == null || cellN.IsFloor) &&
    //             //     cellS != null && !cellS.IsFloor &&
    //             //     (cellE == null || cellE.IsFloor) &&
    //             //     cellW != null && !cellW.IsFloor)
    //             // {
    //             //     tileMap.SetCell(layerWalls, position, WallFrontRight.First().Atlas, WallFrontRight.First().Coordinate);
    //             // }
    //             // // Edge Left
    //             // else if (cellN != null && !cellN.IsFloor &&
    //             //     cellS != null && !cellS.IsFloor &&
    //             //     (cellE == null || cellE.IsFloor) &&
    //             //     cellW != null && !cellW.IsFloor)
    //             // {
    //             //     tileMap.SetCell(layerWalls, position, WallEdgeLeft.First().Atlas, WallEdgeLeft.First().Coordinate);
    //             // }
    //             // // Edge Right
    //             // else if (cellN != null && !cellN.IsFloor &&
    //             //     cellS != null && !cellS.IsFloor &&
    //             //     cellE != null && cellE.IsFloor &&
    //             //     (cellW == null || cellW.IsFloor))
    //             // {
    //             //     tileMap.SetCell(layerWalls, position, WallEdgeLeft.First().Atlas, WallEdgeLeft.First().Coordinate);
    //             // }
    //             // else
    //             // {
    //             //     // GD.Print($"Unknown mapping at {x}:{y}! [{cell}]");
    //             // }


    //             placeCells(x, y,
    //                 floorCellTL, floorCellTC, floorCellTR,
    //                 floorCellML, floorCellMC, floorCellMR,
    //                 floorCellBL, floorCellBC, floorCellBR,
    //                 layerBackground
    //             );
    //             placeCells(x, y,
    //                 wallCellTL, wallCellTC, wallCellTR,
    //                 wallCellML, wallCellMC, wallCellMR,
    //                 wallCellBL, wallCellBC, wallCellBR,
    //                 layerWalls
    //             );
    //         }
    // }
}
