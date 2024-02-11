using System;
using System.Collections.Generic;
using Godot.Collections;

[GlobalClass]
public partial class DungeonGrid : Node2D
{
    [ExportGroup("Tiles")]
    [Export]
    public TileSet tileSet { get; set; }

    [Export]
    public Array<TileConfig> FloorTiles;

    [Export]
    public Array<TileConfig> WallAreaFloorTiles;

    [Export]
    public Array<TileConfig> WallBackCenter;

    [Export]
    public Array<TileConfig> WallBackRight;

    [Export]
    public Array<TileConfig> WallEdgeRight;

    [Export]
    public Array<TileConfig> WallFrontRight;

    [Export]
    public Array<TileConfig> WallFrontCenter;

    [Export]
    public Array<TileConfig> WallFrontLeft;

    [Export]
    public Array<TileConfig> WallEdgeLeft;

    [Export]
    public Array<TileConfig> WallBackLeft;

    [Export]
    public Array<TileConfig> WallInverseCornerLeft;

    [Export]
    public Array<TileConfig> WallInverseCornerRight;

    private TileMap tileMap = new();

    private int layerCounter = 0;
    private int layerBackground = -1;
    private int layerWalls = -1;
    private int layerDecorations = -1;

    public override void _EnterTree()
    {
    }

    public override void _Ready()
    {
        AddChild(tileMap);

        tileMap.TileSet = tileSet;

        layerBackground = AddLayer("Background");
        layerWalls = AddLayer("Walls");
        layerDecorations = AddLayer("Decorations");
    }

    private int AddLayer(string name)
    {
        var layerId = layerCounter++;
        tileMap.AddLayer(layerId);
        tileMap.SetLayerName(layerId, name);
        return layerId;
    }

    public void FromGridGenerator(GridGenerator generator)
    {
        MakeBackgroundLayerFromGenerator(generator);
        MakeWallLayerFromGenerator(generator);
    }

    /// <summary>
    /// ⚠️ Workaround ⚠️
    /// </summary>
    /// <param name="tileConfig"></param>
    /// <returns></returns> <summary>
    /// 
    /// </summary>
    /// <param name="tileConfig"></param>
    /// <returns></returns>
    private float FindProbabilityOfTile(TileConfig tileConfig)
    {
        // Constants
        var zeroLayer = 0;
        var zeroCoordinate = new Vector2I(0, 0);

        float probeTile()
        {
            // Set the cell we want to know the probability of
            tileMap.SetCell(zeroLayer, zeroCoordinate, tileConfig.Atlas, tileConfig.Coordinate);

            // Retrieve probability of the cell
            var probabilityCellData = tileMap.GetCellTileData(zeroLayer, zeroCoordinate);
            var result = (float)probabilityCellData.Get("probability");

            // Unset cell at zero position
            tileMap.SetCell(zeroLayer, zeroCoordinate, -1);

            return result;
        }

        float probability;
        var modeProbeLayer = tileMap.GetLayersCount() == 0;
        if (modeProbeLayer)
        {
            // Add temporary layer for probing
            tileMap.AddLayer(zeroLayer);
            tileMap.SetLayerName(zeroLayer, "__PROBABILITY_PROBE");

            probability = probeTile();

            // Remove temporary layer
            tileMap.RemoveLayer(zeroLayer);
        }
        else
        {
            // Retrieve existing tile information to restore later
            var existingTileAtlas = tileMap.GetCellSourceId(zeroLayer, zeroCoordinate);
            var existingTileCoordinate = tileMap.GetCellAtlasCoords(zeroLayer, zeroCoordinate);

            probability = probeTile();

            // Restore previous cell
            tileMap.SetCell(zeroLayer, zeroCoordinate, existingTileAtlas, existingTileCoordinate);
        }

        // Return findings!
        return probability;
    }

    private List<TileConfig> CompileProbabilityList(Array<TileConfig> tileConfigs)
    {
        // List of possible tiles to be chosen from.
        // There will be **intentionally** duplicates in here.
        // Example: A probability of 0.95 will result in 95 duplicated objects
        // being added (this number will be adjusted based on 
        // the maximum probability).
        var possibleTiles = new List<TileConfig>();
        var probabilityMap = new System.Collections.Generic.Dictionary<TileConfig, float>();

        // Calculate the relative probability.
        // In an ideal case this will be 1.0 (100%).
        // However, if we have a tile that has 1.0 (100%) set and another 
        // one that has 0.5 (50%) set, the total maximum relative 
        // probability will be 1.5 (150%), instead of 1.0 (100%).
        var relativeMaximumProbability = 0f;
        foreach (var tileConfig in tileConfigs)
        {
            var probability = FindProbabilityOfTile(tileConfig);
            relativeMaximumProbability += probability;
            probabilityMap.Add(tileConfig, probability);
        }

        // For each tile that can be placed here, calculate the adjusted
        // probability based on taking the set probability and dividing it
        // by the relative maximum probability.
        // The resulting value will be a float from 0.0f to 1.0f, adjusted
        // to any total percentages beyond 100% (1.0f).
        //
        // Take that adjusted probability, multiply it by 100 and cast
        // to an int. This will result in a number between 0i and 100i. 
        // Add the tile configuration that many times to the list.
        foreach (var tileConfig in tileConfigs)
        {
            var probability = probabilityMap[tileConfig];
            var adjustedProbability = probability / relativeMaximumProbability;
            var objectCountToAdd = (int)(adjustedProbability * 100);

            for (var i = 0; i < objectCountToAdd; i++)
                possibleTiles.Add(tileConfig);
        }

        return possibleTiles;
    }

    // TODO: Randomize cells if multiple are possible to avoid "3x3 spots"
    private void placeCells(uint x, uint y, TileConfig cell, int layer, bool isGridCoordinate = true)
    {
        for (var ix = 0; ix < 3; ix++)
            for (var iy = 0; iy < 3; iy++)
            {
                var actualX = (isGridCoordinate ? x * 3 : x) + ix;
                var actualY = (isGridCoordinate ? y * 3 : y) + iy;

                tileMap.SetCell(
                    layer,
                    // Positions are intentionally flipped!
                    new Vector2I((int)actualY, (int)actualX),
                    cell.Atlas,
                    cell.Coordinate
                );
            }
    }

    private void placeCells(
        uint x, uint y,
        TileConfig cellTL, TileConfig cellTC, TileConfig cellTR,
        TileConfig cellML, TileConfig cellMC, TileConfig cellMR,
        TileConfig cellBL, TileConfig cellBC, TileConfig cellBR,
        int layer, bool isGridCoordinate = true)
    {
        if (cellTL != null)
            tileMap.SetCell(layer,
            // Positions are flipped intentionally
            new Vector2I(
                (int)(isGridCoordinate ? y * 3 : y),
                (int)(isGridCoordinate ? x * 3 : x)
            ),
            cellTL.Atlas, cellTL.Coordinate);

        if (cellTC != null)
            tileMap.SetCell(layer,
            // Positions are flipped intentionally
            new Vector2I(
                (int)(isGridCoordinate ? y * 3 : y) + 1,
                (int)(isGridCoordinate ? x * 3 : x)
            ),
            cellTC.Atlas, cellTC.Coordinate);

        if (cellTR != null)
            tileMap.SetCell(layer,
            // Positions are flipped intentionally
            new Vector2I(
                (int)(isGridCoordinate ? y * 3 : y) + 2,
                (int)(isGridCoordinate ? x * 3 : x)
            ),
            cellTR.Atlas, cellTR.Coordinate);

        if (cellML != null)
            tileMap.SetCell(layer,
            // Positions are flipped intentionally
            new Vector2I(
                (int)(isGridCoordinate ? y * 3 : y),
                (int)(isGridCoordinate ? x * 3 : x) + 1
            ),
            cellML.Atlas, cellML.Coordinate);

        if (cellMC != null)
            tileMap.SetCell(layer,
            // Positions are flipped intentionally
            new Vector2I(
                (int)(isGridCoordinate ? y * 3 : y) + 1,
                (int)(isGridCoordinate ? x * 3 : x) + 1
            ),
            cellMC.Atlas, cellMC.Coordinate);

        if (cellMR != null)
            tileMap.SetCell(layer,
            // Positions are flipped intentionally
            new Vector2I(
                (int)(isGridCoordinate ? y * 3 : y) + 2,
                (int)(isGridCoordinate ? x * 3 : x) + 1
            ),
            cellMR.Atlas, cellMR.Coordinate);

        if (cellBL != null)
            tileMap.SetCell(layer,
            // Positions are flipped intentionally
            new Vector2I(
                (int)(isGridCoordinate ? y * 3 : y),
                (int)(isGridCoordinate ? x * 3 : x) + 2
            ),
            cellBL.Atlas, cellBL.Coordinate);

        if (cellBC != null)
            tileMap.SetCell(layer,
            // Positions are flipped intentionally
            new Vector2I(
                (int)(isGridCoordinate ? y * 3 : y) + 1,
                (int)(isGridCoordinate ? x * 3 : x) + 2
            ),
            cellBC.Atlas, cellBC.Coordinate);

        if (cellBR != null)
            tileMap.SetCell(layer,
            // Positions are flipped intentionally
            new Vector2I(
                (int)(isGridCoordinate ? y * 3 : y) + 2,
                (int)(isGridCoordinate ? x * 3 : x) + 2
            ),
            cellBR.Atlas, cellBR.Coordinate);
    }

    private void MakeBackgroundLayerFromGenerator(GridGenerator generator)
    {
        var possibleTilesFloor = CompileProbabilityList(FloorTiles);
        var possibleTilesWallArea = CompileProbabilityList(WallAreaFloorTiles);

        for (uint x = 0; x < generator.SizeX; x++)
            for (uint y = 0; y < generator.SizeY; y++)
            {
                TileConfig pickedCell;
                var cell = generator.GetCell((x, y));
                if (cell.IsFloor)
                    pickedCell = possibleTilesFloor[Random.Shared.Next(0, possibleTilesFloor.Count() - 1)];
                else
                    pickedCell = possibleTilesWallArea[Random.Shared.Next(0, possibleTilesFloor.Count() - 1)];

                placeCells(x, y, pickedCell, layerBackground);
            }
    }

    /// <summary>
    /// Refer to `WallTileOrientation.drawio.svg` for an overview of what 
    /// this function is doing...
    /// </summary>
    /// <param name="generator"></param>
    private void MakeWallLayerFromGenerator(GridGenerator generator)
    {
        var possibleTilesFloor = CompileProbabilityList(FloorTiles);
        var possibleTilesWallArea = CompileProbabilityList(WallAreaFloorTiles);

        var possibleWallBackCenter = CompileProbabilityList(WallBackCenter);
        var possibleWallBackRight = CompileProbabilityList(WallBackRight);
        var possibleWallEdgeRight = CompileProbabilityList(WallEdgeRight);
        var possibleWallFrontRight = CompileProbabilityList(WallFrontRight);
        var possibleWallFrontCenter = CompileProbabilityList(WallFrontCenter);
        var possibleWallFrontLeft = CompileProbabilityList(WallFrontLeft);
        var possibleWallEdgeLeft = CompileProbabilityList(WallEdgeLeft);
        var possibleWallBackLeft = CompileProbabilityList(WallBackLeft);

        for (uint x = 0; x < generator.SizeX; x++)
            for (uint y = 0; y < generator.SizeY; y++)
            {
                var cell = generator.GetCell((x, y));

                // Skip non-walls
                if (cell.IsFloor) continue;

                var cellN = generator.GetCell((x - 1, y));
                var cellS = generator.GetCell((x + 1, y));
                var cellW = generator.GetCell((x, y - 1));
                var cellE = generator.GetCell((x, y + 1));

                TileConfig wallCellTL = null;
                TileConfig wallCellTC = null;
                TileConfig wallCellTR = null;
                TileConfig wallCellML = null;
                TileConfig wallCellMC = null;
                TileConfig wallCellMR = null;
                TileConfig wallCellBL = null;
                TileConfig wallCellBC = null;
                TileConfig wallCellBR = null;

                TileConfig floorCellTL = null;
                TileConfig floorCellTC = null;
                TileConfig floorCellTR = null;
                TileConfig floorCellML = null;
                TileConfig floorCellMC = null;
                TileConfig floorCellMR = null;
                TileConfig floorCellBL = null;
                TileConfig floorCellBC = null;
                TileConfig floorCellBR = null;

                // --- Vertical Walls ---
                // #  F  #
                // W [W] W
                // #  F  #
                if (
                    (cellN?.IsFloor ?? false) &&
                    (cellS?.IsFloor ?? false) &&
                    (!cellE?.IsFloor ?? false) &&
                    (!cellW?.IsFloor ?? false))
                {
                    // Walls
                    wallCellTL = possibleWallBackCenter[Random.Shared.Next(0, possibleWallBackCenter.Count() - 1)];
                    wallCellTC = possibleWallBackCenter[Random.Shared.Next(0, possibleWallBackCenter.Count() - 1)];
                    wallCellTR = possibleWallBackCenter[Random.Shared.Next(0, possibleWallBackCenter.Count() - 1)];

                    wallCellBL = possibleWallFrontCenter[Random.Shared.Next(0, possibleWallFrontCenter.Count() - 1)];
                    wallCellBC = possibleWallFrontCenter[Random.Shared.Next(0, possibleWallFrontCenter.Count() - 1)];
                    wallCellBR = possibleWallFrontCenter[Random.Shared.Next(0, possibleWallFrontCenter.Count() - 1)];

                    // // Floors
                    floorCellTL = possibleTilesFloor[Random.Shared.Next(0, possibleTilesFloor.Count() - 1)];
                    floorCellTC = possibleTilesFloor[Random.Shared.Next(0, possibleTilesFloor.Count() - 1)];
                    floorCellTR = possibleTilesFloor[Random.Shared.Next(0, possibleTilesFloor.Count() - 1)];
                }
                // #  W  #
                // W [W] W
                // #  F  #
                else if (
                    (!cellN?.IsFloor ?? false) &&
                    (cellS?.IsFloor ?? false) &&
                    (!cellE?.IsFloor ?? false) &&
                    (!cellW?.IsFloor ?? false))
                {
                    wallCellBL = possibleWallFrontCenter[Random.Shared.Next(0, possibleWallFrontCenter.Count() - 1)];
                    wallCellBC = possibleWallFrontCenter[Random.Shared.Next(0, possibleWallFrontCenter.Count() - 1)];
                    wallCellBR = possibleWallFrontCenter[Random.Shared.Next(0, possibleWallFrontCenter.Count() - 1)];
                }
                // #  F  #
                // W [W] W
                // #  W  #
                else if (
                    (cellN?.IsFloor ?? false) &&
                    (!cellS?.IsFloor ?? false) &&
                    (!cellE?.IsFloor ?? false) &&
                    (!cellW?.IsFloor ?? false))
                {
                    wallCellTL = possibleWallBackCenter[Random.Shared.Next(0, possibleWallBackCenter.Count() - 1)];
                    wallCellTC = possibleWallBackCenter[Random.Shared.Next(0, possibleWallBackCenter.Count() - 1)];
                    wallCellTR = possibleWallBackCenter[Random.Shared.Next(0, possibleWallBackCenter.Count() - 1)];

                    floorCellTL = possibleTilesFloor[Random.Shared.Next(0, possibleTilesFloor.Count() - 1)];
                    floorCellTC = possibleTilesFloor[Random.Shared.Next(0, possibleTilesFloor.Count() - 1)];
                    floorCellTR = possibleTilesFloor[Random.Shared.Next(0, possibleTilesFloor.Count() - 1)];
                }
                // --- Horizontal Walls ---
                // #  W  #
                // F [W] F
                // #  W  #
                else if (
                    (!cellN?.IsFloor ?? false) &&
                    (!cellS?.IsFloor ?? false) &&
                    (cellE?.IsFloor ?? false) &&
                    (cellW?.IsFloor ?? false))
                {
                    // Walls
                    wallCellTL = possibleWallEdgeRight[Random.Shared.Next(0, possibleWallEdgeRight.Count() - 1)];
                    wallCellML = possibleWallEdgeRight[Random.Shared.Next(0, possibleWallEdgeRight.Count() - 1)];
                    wallCellBL = possibleWallEdgeRight[Random.Shared.Next(0, possibleWallEdgeRight.Count() - 1)];

                    wallCellTR = possibleWallEdgeLeft[Random.Shared.Next(0, possibleWallEdgeLeft.Count() - 1)];
                    wallCellMR = possibleWallEdgeLeft[Random.Shared.Next(0, possibleWallEdgeLeft.Count() - 1)];
                    wallCellBR = possibleWallEdgeLeft[Random.Shared.Next(0, possibleWallEdgeLeft.Count() - 1)];
                }
                // #  W  #
                // W [W] F
                // #  W  #
                else if (
                    (!cellN?.IsFloor ?? false) &&
                    (!cellS?.IsFloor ?? false) &&
                    (cellE?.IsFloor ?? false) &&
                    (!cellW?.IsFloor ?? false))
                {
                    // Walls
                    wallCellTR = possibleWallEdgeLeft[Random.Shared.Next(0, possibleWallEdgeLeft.Count() - 1)];
                    wallCellMR = possibleWallEdgeLeft[Random.Shared.Next(0, possibleWallEdgeLeft.Count() - 1)];
                    wallCellBR = possibleWallEdgeLeft[Random.Shared.Next(0, possibleWallEdgeLeft.Count() - 1)];
                }
                // #  W  #
                // F [W] W
                // #  W  #
                else if (
                    (!cellN?.IsFloor ?? false) &&
                    (!cellS?.IsFloor ?? false) &&
                    (!cellE?.IsFloor ?? false) &&
                    (cellW?.IsFloor ?? false))
                {
                    // Walls
                    wallCellTL = possibleWallEdgeRight[Random.Shared.Next(0, possibleWallEdgeRight.Count() - 1)];
                    wallCellML = possibleWallEdgeRight[Random.Shared.Next(0, possibleWallEdgeRight.Count() - 1)];
                    wallCellBL = possibleWallEdgeRight[Random.Shared.Next(0, possibleWallEdgeRight.Count() - 1)];
                }
                // --- Corners ---
                // #  F  #
                // F [W] W
                // #  W  #
                else if (
                    (cellN?.IsFloor ?? false) &&
                    (!cellS?.IsFloor ?? false) &&
                    (!cellE?.IsFloor ?? false) &&
                    (cellW?.IsFloor ?? false))
                {
                    // Walls
                    wallCellTC = possibleWallBackCenter[Random.Shared.Next(0, possibleWallBackCenter.Count() - 1)];
                    wallCellTR = possibleWallBackCenter[Random.Shared.Next(0, possibleWallBackCenter.Count() - 1)];

                    wallCellTL = possibleWallEdgeRight[Random.Shared.Next(0, possibleWallEdgeRight.Count() - 1)];
                    wallCellML = possibleWallEdgeRight[Random.Shared.Next(0, possibleWallEdgeRight.Count() - 1)];
                    wallCellBL = possibleWallEdgeRight[Random.Shared.Next(0, possibleWallEdgeRight.Count() - 1)];
                }

                // // Front Left
                // else if (cellN != null && !cellN.IsFloor &&
                //     (cellS == null || cellS.IsFloor) &&
                //     cellE != null && !cellE.IsFloor &&
                //     (cellW == null || cellW.IsFloor))
                // {
                //     tileMap.SetCell(layerWalls, position, WallFrontLeft.First().Atlas, WallFrontLeft.First().Coordinate);
                // }
                // // Front Right
                // else if (cellN != null && !cellN.IsFloor &&
                //     (cellS == null || cellS.IsFloor) &&
                //     (cellE == null || cellE.IsFloor) &&
                //     cellW != null && !cellW.IsFloor)
                // {
                //     tileMap.SetCell(layerWalls, position, WallFrontRight.First().Atlas, WallFrontRight.First().Coordinate);
                // }
                // // Back Center
                // else if (cellN != null && cellN.IsFloor &&
                //     (cellS == null || !cellS.IsFloor) &&
                //     cellE != null && !cellE.IsFloor &&
                //     cellW != null && !cellW.IsFloor)
                // {
                //     tileMap.SetCell(layerWalls, position, WallBackCenter.First().Atlas, WallBackCenter.First().Coordinate);
                // }
                // // Back Left
                // else if ((cellN == null || cellN.IsFloor) &&
                //     cellS != null && !cellS.IsFloor &&
                //     cellE != null && !cellE.IsFloor &&
                //     (cellW == null || cellW.IsFloor))
                // {
                //     tileMap.SetCell(layerWalls, position, WallFrontLeft.First().Atlas, WallFrontLeft.First().Coordinate);
                // }
                // // Back Right
                // else if ((cellN == null || cellN.IsFloor) &&
                //     cellS != null && !cellS.IsFloor &&
                //     (cellE == null || cellE.IsFloor) &&
                //     cellW != null && !cellW.IsFloor)
                // {
                //     tileMap.SetCell(layerWalls, position, WallFrontRight.First().Atlas, WallFrontRight.First().Coordinate);
                // }
                // // Edge Left
                // else if (cellN != null && !cellN.IsFloor &&
                //     cellS != null && !cellS.IsFloor &&
                //     (cellE == null || cellE.IsFloor) &&
                //     cellW != null && !cellW.IsFloor)
                // {
                //     tileMap.SetCell(layerWalls, position, WallEdgeLeft.First().Atlas, WallEdgeLeft.First().Coordinate);
                // }
                // // Edge Right
                // else if (cellN != null && !cellN.IsFloor &&
                //     cellS != null && !cellS.IsFloor &&
                //     cellE != null && cellE.IsFloor &&
                //     (cellW == null || cellW.IsFloor))
                // {
                //     tileMap.SetCell(layerWalls, position, WallEdgeLeft.First().Atlas, WallEdgeLeft.First().Coordinate);
                // }
                // else
                // {
                //     // GD.Print($"Unknown mapping at {x}:{y}! [{cell}]");
                // }


                placeCells(x, y,
                    floorCellTL, floorCellTC, floorCellTR,
                    floorCellML, floorCellMC, floorCellMR,
                    floorCellBL, floorCellBC, floorCellBR,
                    layerBackground
                );
                placeCells(x, y,
                    wallCellTL, wallCellTC, wallCellTR,
                    wallCellML, wallCellMC, wallCellMR,
                    wallCellBL, wallCellBC, wallCellBR,
                    layerWalls
                );
            }
    }
}
