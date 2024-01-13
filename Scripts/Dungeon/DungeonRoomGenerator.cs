using System;
using System.Collections.Generic;
using Godot;

public class DungeonRoomGenerator
{
    public const char FLOOR = '□';
    public const char WALL = '■';
    public const char EMPTY = ' ';
    public const char NULL = '0';

    private static Random R = Random.Shared;

    public char[,] Grid { get; private set; }

    public DungeonRoomGenerator(int maxWidth, int maxHeight)
    {
        InitializeGrid(maxWidth, maxHeight);
    }

    public void DoWork(int minSize, bool exactSize = false, DungeonRoomType roomType = DungeonRoomType.RandomPlaceSquare)
    {
        Generate(minSize, exactSize, roomType);
        Walls();
        Reduce();
    }

    private void InitializeGrid(int maxX, int maxY)
    {
        Grid = new char[maxX, maxY];
        for (var x = 0; x < maxX; x++)
            for (var y = 0; y < maxY; y++)
                Grid[x, y] = EMPTY;
    }

    private void ReinitializeGrid()
    {
        InitializeGrid(GetWidth(), GetHeight());
    }

    public void Generate(int minSize, bool exactSize = false, DungeonRoomType roomType = DungeonRoomType.RandomPlaceSquare)
    {
        switch (roomType)
        {
            case DungeonRoomType.RandomPlaceSquare:
                GenerateRandomPlaceSquare(minSize, exactSize);
                break;
            case DungeonRoomType.Circular:
                GenerateCircular(minSize, exactSize);
                break;
        }
    }

    private void GenerateRandomPlaceSquare(int minSize, bool exactSize)
    {
        var iEnd = R.Next(5, 15);
        for (var i = 0; i < iEnd; i++)
        {
            var originX = exactSize ? GetWidth() : R.Next(0, GetWidth());
            var originY = exactSize ? GetHeight() : R.Next(0, GetHeight());

            if (originX + minSize >= GetWidth() || originY + minSize >= GetHeight())
                continue; // Skip

            var sizeLeftX = GetWidth() - originX - 1;
            var sizeLeftY = GetHeight() - originY - 1;

            var endX = exactSize ? sizeLeftX : R.Next(originX + minSize, originX + sizeLeftX);
            var endY = exactSize ? sizeLeftY : R.Next(originY + minSize, originY + sizeLeftY);

            // Last one must be additive
            var additive = i == iEnd - 1 || R.NextDouble() <= 0.75;

            for (var x = originX; x <= endX; x++)
                for (var y = originY; y <= endY; y++)
                    Grid[x, y] = additive ? FLOOR : EMPTY;
        }

        // Sanity check
        var filledTileCount = 0;
        for (var x = 0; x < GetWidth(); x++)
            for (var y = 0; y < GetHeight(); y++)
                if (GetCell(x, y) == FLOOR) filledTileCount++;

        if (filledTileCount < minSize * minSize)
        {
            // Something went wrong, regenerate the room!
            ReinitializeGrid();
            Generate(minSize, exactSize, DungeonRoomType.RandomPlaceSquare);
        }
    }

    private void GenerateCircular(int minSize, bool exactSize)
    {
        var smallestDimension = GetWidth() > GetHeight()
                ? GetHeight()
                : GetWidth();
        var offset = (exactSize
            ? smallestDimension
            : R.Next(minSize, smallestDimension)) / 2;
        var radius = offset / 2;

        for (var x = 0; x <= GetWidth(); x++)
            for (var y = 0; y <= GetHeight(); y++)
            {
                if (x * x + y * y < radius * radius)
                    Grid[offset + x, offset + y] = FLOOR;
            }

        // Sanity check
        var filledTileCount = 0;
        for (var x = 0; x < GetWidth(); x++)
            for (var y = 0; y < GetHeight(); y++)
                if (GetCell(x, y) == FLOOR) filledTileCount++;

        if (filledTileCount < minSize * minSize)
        {
            // Something went wrong, regenerate the room!
            ReinitializeGrid();
            Generate(minSize, exactSize, DungeonRoomType.Circular);
        }
    }

    public void Walls()
    {
        var result = new char[GetWidth() + 1, GetHeight() + 1];
        Array.Copy(Grid, result, Grid.Length);

        // For each empty cell, check the cell above (up), below (down), left 
        // and right of it. If there is a floor, fill the current tile with a
        // wall.
        foreach (var emptyCell in FindEmptyCells())
        {
            var up = GetCell(emptyCell.Item1 + 1, emptyCell.Item2);
            var down = GetCell(emptyCell.Item1 - 1, emptyCell.Item2);
            var left = GetCell(emptyCell.Item1, emptyCell.Item2 - 1);
            var right = GetCell(emptyCell.Item1, emptyCell.Item2 + 1);

            if (up == FLOOR || down == FLOOR || left == FLOOR || right == FLOOR)
                result[emptyCell.Item1, emptyCell.Item2] = WALL;
        }

        // Run around the edges of our data array.
        // If a floor is at the edge of it, replace it with a wall.
        for (var x = 0; x <= GetWidth(); x++)
            for (var y = 0; y <= GetHeight(); y++)
                if (x == 0 || y == 0 || x == GetWidth() || y == GetHeight())
                    if (GetCell(x, y) == FLOOR) result[x, y] = WALL;

        // check for wall "extensions"
        for (var x = 0; x <= GetWidth(); x++)
            for (var y = 0; y <= GetHeight(); y++)
                if (GetCell(x, y) == WALL)
                    if (x == 0 || y == 0 || x == GetWidth() || y == GetHeight())
                        if (GetCell(x, y) == FLOOR) result[x, y] = WALL;

        Grid = result;
    }

    public void Reduce()
    {
        var smallestFilledTileX = int.MaxValue;
        var biggestFilledTileX = 0;
        var smallestFilledTileY = int.MaxValue;
        var biggestFilledTileY = 0;

        for (var x = 0; x <= GetWidth(); x++)
            for (var y = 0; y <= GetHeight(); y++)
            {
                var cell = GetCell(x, y);

                if (cell != EMPTY && cell != NULL)
                {
                    if (x < smallestFilledTileX) smallestFilledTileX = x;
                    if (y < smallestFilledTileY) smallestFilledTileY = y;

                    if (x > biggestFilledTileX) biggestFilledTileX = x;
                    if (y > biggestFilledTileY) biggestFilledTileY = y;
                }
            }

        var sizeX = biggestFilledTileX - smallestFilledTileX + 1;
        var sizeY = biggestFilledTileY - smallestFilledTileY + 1;

        var result = new char[sizeX, sizeY];

        for (var x = 0; x < sizeX; x++)
            for (var y = 0; y < sizeY; y++)
                result[x, y] = Grid[smallestFilledTileX + x, smallestFilledTileY + y];

        Grid = result;
    }

    public int GetWidth() => Grid.GetUpperBound(0);
    public int GetHeight() => Grid.GetUpperBound(1);

    public void Print()
    {
        var output = "";

        for (var x = 0; x <= GetWidth(); x++)
        {
            output += $"#{x:000} :: ";
            for (var y = 0; y <= GetHeight(); y++)
                output += $"{Grid[x, y]}";
            output += "\n";
        }

        GD.Print(output);
    }

    private List<(int, int)> FindEmptyCells()
    {
        var emptyCells = new List<(int, int)>();

        for (var x = 0; x <= GetWidth(); x++)
            for (var y = 0; y <= GetHeight(); y++)
                if (Grid[x, y] == EMPTY) emptyCells.Add((x, y));

        return emptyCells;
    }

    public char GetCell(int x, int y)
    {
        if (IsInBounds(x, y)) return Grid[x, y];
        else return NULL;
    }

    public bool IsInBounds(int x, int y)
    {
        return x >= 0 && y >= 0 && x <= GetWidth() && y <= GetHeight();
    }
}
