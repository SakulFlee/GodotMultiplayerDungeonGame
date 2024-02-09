using System;
using System.Collections.Generic;
using Godot.Collections;

[GlobalClass]
public partial class DungeonGrid : Node2D
{
    [Export]
    public TileSet tileSet { get; set; }

    [ExportGroup("Tiles")]
    [Export]
    public Array<TileConfig> FloorTiles;

    [Export]
    public Array<TileConfig> WallTiles;

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

        // Retrieve existing tile information to restore later
        var existingTileAtlas = tileMap.GetCellSourceId(zeroLayer, zeroCoordinate);
        var existingTileCoordinate = tileMap.GetCellAtlasCoords(zeroLayer, zeroCoordinate);

        // Set the cell we want to know the probability of
        tileMap.SetCell(zeroLayer, zeroCoordinate, tileConfig.Atlas, tileConfig.Coordinate);

        // Retrieve probability of the cell
        var probabilityCellData = tileMap.GetCellTileData(zeroLayer, zeroCoordinate);
        var probability = (float)probabilityCellData.Get("probability");

        // Restore previous cell
        tileMap.SetCell(zeroLayer, zeroCoordinate, existingTileAtlas, existingTileCoordinate);

        // Return findings!
        return probability;
    }

    private List<(int, Vector2I)> CompileProbabilityList(Array<TileConfig> tileConfigs)
    {
        // List of possible tiles to be chosen from.
        // There will be **intentionally** duplicates in here.
        // Example: A probability of 0.95 will result in 95 duplicated objects
        // being added (this number will be adjusted based on 
        // the maximum probability).
        var possibleTiles = new List<(int, Vector2I)>();
        var probabilityMap = new System.Collections.Generic.Dictionary<(int, Vector2I), float>();

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
            probabilityMap.Add((tileConfig.Atlas, tileConfig.Coordinate), probability);
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
            var probability = probabilityMap[(tileConfig.Atlas, tileConfig.Coordinate)];
            var adjustedProbability = probability / relativeMaximumProbability;
            var objectCountToAdd = (int)(adjustedProbability * 100);
            var localTile = (tileConfig.Atlas, tileConfig.Coordinate);

            for (var i = 0; i < objectCountToAdd; i++)
                possibleTiles.Add(localTile);
        }

        return possibleTiles;
    }

    private void MakeBackgroundLayerFromGenerator(GridGenerator generator)
    {
        var possibleTiles = CompileProbabilityList(FloorTiles);

        for (uint x = 0; x < generator.SizeX; x++)
            for (uint y = 0; y < generator.SizeY; y++)
            {
                var pickedCell = possibleTiles[Random.Shared.Next(0, possibleTiles.Count() - 1)];
                tileMap.SetCell(
                    layerBackground,
                    new Vector2I((int)x, (int)y),
                    pickedCell.Item1,
                    pickedCell.Item2
                );
            }
    }

    private void MakeWallLayerFromGenerator(GridGenerator generator)
    {
        var possibleTiles = CompileProbabilityList(WallTiles);

        for (uint x = 0; x < generator.SizeX; x++)
            for (uint y = 0; y < generator.SizeY; y++)
            {
                var cell = generator.GetCell((x, y));
                if (cell.IsFloor) continue;

                var pickedCell = possibleTiles[Random.Shared.Next(0, possibleTiles.Count() - 1)];
                tileMap.SetCell(
                    layerBackground,
                    new Vector2I((int)x, (int)y),
                    pickedCell.Item1,
                    pickedCell.Item2
                );
            }
    }
}
