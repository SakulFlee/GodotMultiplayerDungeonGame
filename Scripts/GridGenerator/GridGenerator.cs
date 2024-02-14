#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;

public class GridGenerator
{
    public uint MinimumNeighbourWallsForFloor = 4;
    public uint DesiredRoomCount = 25;  // Maybe: Max Size / 3 ?
    public uint SmallAreaThresholdCells = 9;

    public Random R;
    public GridCell[,] Grid { get; private set; }
    public int SizeX { get => Grid.GetUpperBound(0); }
    public int SizeY { get => Grid.GetUpperBound(1); }

    public uint AreaCount { get; private set; } = 0;
    public uint RoomCount { get; private set; } = 0;

    public GridGenerator((uint, uint) gridSize, int seed = 12345, double floorPercentage = 0.60, bool printToConsole = false)
    {
        R = new Random(seed);

        Grid = new GridCell[gridSize.Item1, gridSize.Item2];
        for (var x = 0; x < gridSize.Item1; x++)
            for (var y = 0; y < gridSize.Item2; y++)
                Grid[x, y] = R.NextDouble() <= floorPercentage
                    ? new GridCell(isFloor: true)
                    : new GridCell(isFloor: false);

        if (printToConsole)
        {
            GD.Print(">>> Randomized Grid:");
            PrintToConsole();
        }
    }

    public void Automate(bool printAutomataStepsToConsole = false, bool printRoomToConsole = false, bool printAreasToConsole = false, bool printInvalidAreaFixToConsole = false, bool printDoorwaysToConsole = false, bool printFinalResultToConsole = false)
    {
        PerformAutomataRepetitive(printStepsToConsole: printAutomataStepsToConsole);
        PlaceRooms(printToConsole: printRoomToConsole);
        AssignAreas(printStepsToConsole: printAreasToConsole);
        FixInvalidAreas(printToConsole: printInvalidAreaFixToConsole);
        CheckForDoorways(printToConsole: printDoorwaysToConsole);

        if (printFinalResultToConsole) PrintToConsole();
    }

    public void CheckForDoorways(bool printToConsole = false)
    {
        for (uint x = 0; x < SizeX; x++)
            for (uint y = 0; y < SizeY; y++)
            {
                // Skip floor tiles
                if (Grid[x, y].IsFloor) continue;

                var cellN = GetCell((x - 1, y));
                var cellS = GetCell((x + 1, y));
                var cellE = GetCell((x, y + 1));
                var cellW = GetCell((x, y - 1));

                Grid[x, y].CanBeDoor = (cellN != null && cellN.IsFloor
                    && cellS != null && cellS.IsFloor
                    && cellE != null && !cellE.IsFloor
                    && cellW != null && !cellW.IsFloor)
                    ||
                    (cellE != null && cellE.IsFloor
                    && cellW != null && cellW.IsFloor
                    && cellN != null && !cellN.IsFloor
                    && cellS != null && !cellS.IsFloor);
            }

        if (printToConsole) PrintToConsole();
    }

    public void FixInvalidAreas(bool fixGridEdges = true, bool redoAreas = true, bool printToConsole = false)
    {
        if (fixGridEdges)
            for (var x = 0; x < SizeX; x++)
                for (var y = 0; y < SizeY; y++)
                    if (x == 0 || y == 0 || x == SizeX - 1 || y == SizeY - 1)
                        Grid[x, y] = new GridCell(isFloor: false);

        for (uint area = 1; area <= AreaCount; area++)
        {
            uint minX = int.MaxValue;
            uint minY = int.MaxValue;
            uint maxX = 0;
            uint maxY = 0;

            var cellsInArea = FindCellOfArea(area);
            foreach (var (x, y) in cellsInArea)
            {
                if (x < minX)
                {
                    minX = x;
                }
                else if (y < minY)
                {
                    minY = y;
                }
                else if (x > maxX)
                {
                    maxX = x;
                }
                else if (y > maxY)
                {
                    maxY = y;
                }
            }

            var removeArea = minX == int.MaxValue
                || minY == int.MaxValue
                || maxX == 0
                || maxY == 0
                || minX == maxX
                || minY == maxY
                || cellsInArea.Count() < SmallAreaThresholdCells;

            if (removeArea)
                foreach (var (x, y) in cellsInArea)
                    Grid[x, y] = new GridCell(isFloor: false);
        }

        if (redoAreas)
        {
            AreaCount = 0;
            for (var x = 0; x < SizeX; x++)
                for (var y = 0; y < SizeY; y++)
                    Grid[x, y].Area = 0;

            AssignAreas();
        }

        if (printToConsole) PrintToConsole();
    }

    public List<IGridRoom> MakeRandomRoomList()
    {
        var output = new List<IGridRoom>();
        while (output.Count() <= DesiredRoomCount)
        {
            uint roomLocationX = (uint)R.Next(0, SizeX);
            uint roomLocationY = (uint)R.Next(0, SizeY);

            var maxSizeX = SizeX - roomLocationX;
            var maxSizeY = SizeY - roomLocationY;

            const uint minRoomSize = 5;
            const uint maxRoomSize = 30;

            if (maxSizeX < minRoomSize || maxSizeY < minRoomSize) continue;

            uint roomSizeX = (uint)R.Next(
                (int)minRoomSize,
                (int)(maxSizeX < maxRoomSize ? maxSizeX : maxRoomSize)
            );
            uint roomSizeY = (uint)R.Next(
                (int)minRoomSize,
                (int)(maxSizeY < maxRoomSize ? maxSizeY : maxRoomSize)
            );

            var room = new GridRoomRectangular((roomLocationX, roomLocationY), (roomSizeX, roomSizeY), (uint)output.Count() + 1);
            output.Add(room);
        }

        return output;
    }

    public void PlaceRooms(bool printToConsole = false)
    {
        var rooms = MakeRandomRoomList();
        PlaceRooms(rooms, printToConsole: printToConsole);
    }

    public void PlaceRooms(List<IGridRoom> rooms, bool printToConsole = false)
    {
        foreach (var room in rooms)
            Grid = room.Apply(Grid);

        RoomCount = (uint)rooms.Count();

        if (printToConsole)
        {
            GD.Print(">>> Placed rooms:");
            PrintToConsole();
        }
    }

    public void FixAreas()
    {
        var floorsWithoutAreaList = FindCell((x, y, cell) => cell.IsFloor && cell.Area == 0);
        var floorsWithoutArea = new Queue<(uint, uint)>(floorsWithoutAreaList.Count());
        foreach (var a in floorsWithoutAreaList) floorsWithoutArea.Enqueue(a);

        while (floorsWithoutArea.Count() > 0)
        {
            var cellPosition = floorsWithoutArea.Dequeue();

            var cellN = GetCell((cellPosition.Item1 - 1, cellPosition.Item2));
            if (cellN != null && cellN.Area > 0)
            {
                Grid[cellPosition.Item1, cellPosition.Item2].Area = cellN.Area;
                continue;
            }

            var cellS = GetCell((cellPosition.Item1 + 1, cellPosition.Item2));
            if (cellS != null && cellS.Area > 0)
            {
                Grid[cellPosition.Item1, cellPosition.Item2].Area = cellS.Area;
                continue;
            }

            var cellE = GetCell((cellPosition.Item1, cellPosition.Item2 - 1));
            if (cellE != null && cellE.Area > 0)
            {
                Grid[cellPosition.Item1, cellPosition.Item2].Area = cellE.Area;
                continue;
            }

            var cellW = GetCell((cellPosition.Item1, cellPosition.Item2 + 1));
            if (cellW != null && cellW.Area > 0)
            {
                Grid[cellPosition.Item1, cellPosition.Item2].Area = cellW.Area;
                continue;
            }

            floorsWithoutArea.Enqueue(cellPosition);
        }
    }

    public void PerformAutomataRepetitive(int steps = 5, bool printStepsToConsole = false)
    {
        if (printStepsToConsole)
        {
            GD.Print($">>> Automata Basis:");
            PrintToConsole();
        }

        for (var step = 1; step <= steps; step++)
        {
            PerformAutomata();

            if (printStepsToConsole)
            {
                GD.Print($">>> Automata #{step}/{steps}:");
                PrintToConsole();
            }
        }
    }

    public void PerformAutomata()
    {
        var outputGrid = new GridCell[SizeX + 1, SizeY + 1];

        for (uint x = 0; x <= SizeX; x++)
            for (uint y = 0; y <= SizeY; y++)
                outputGrid[x, y] = CountNeighboursOfType((x, y), isFloor: false, countNull: true) < MinimumNeighbourWallsForFloor
                    ? new GridCell(isFloor: true)
                    : new GridCell(isFloor: false);

        Grid = outputGrid;
    }

    public void AssignAreas(bool printStepsToConsole = false)
    {
        uint area = 1;

        var currentX = -1;
        var currentY = 0;
        do
        {
            // Increment X & Y
            if (currentX++ >= SizeX)
            {
                currentX = 0;
                // Break the loop if we are out of bounce
                if (currentY++ >= SizeY)
                    break;
            }

            // If the cell at the current location is invalid, not a floor or already has an area assigned, skip it.
            var cell = GetCell(((uint)currentX, (uint)currentY));
            if (cell == null || !cell.IsFloor || cell.HasAreaData()) continue;

            // Cell must be a floor AND in an area we haven't been in yet
            AssignAreaNeighboursAndSelf(((uint)currentX, (uint)currentY), area);

            if (printStepsToConsole)
            {
                GD.Print($">>> Area #{area}:");
                PrintToConsole();
            }

            // Increment area counter
            area++;
        } while (true);

        // Set counter
        AreaCount = area;
    }

    public void AssignAreaNeighboursAndSelf((uint, uint) position, uint area)
    {
        var cellSelf = GetCell(position);

        // Skip invalid cells
        if (cellSelf == null) return;

        // Skip already assigned areas
        if (cellSelf.HasAreaData()) return;

        // Skip non-floor cells
        if (!cellSelf.IsFloor) return;

        Grid[position.Item1, position.Item2].Area = area;

        var cellN = GetCell((position.Item1 - 1, position.Item2));
        if (cellN != null && cellN.IsFloor)
        {
            // Grid[position.Item1 - 1, position.Item2].Area = area;
            AssignAreaNeighboursAndSelf((position.Item1 - 1, position.Item2), area);
        }

        var cellS = GetCell((position.Item1 + 1, position.Item2));
        if (cellS != null && cellS.IsFloor)
        {
            // Grid[position.Item1 + 1, position.Item2].Area = area;
            AssignAreaNeighboursAndSelf((position.Item1 + 1, position.Item2), area);
        }

        var cellE = GetCell((position.Item1, position.Item2 - 1));
        if (cellE != null && cellE.IsFloor)
        {
            // Grid[position.Item1, position.Item2 - 1].Area = area;
            AssignAreaNeighboursAndSelf((position.Item1, position.Item2 - 1), area);
        }

        var cellW = GetCell((position.Item1, position.Item2 + 1));
        if (cellW != null && cellW.IsFloor)
        {
            // Grid[position.Item1, position.Item2 + 1].Area = area;
            AssignAreaNeighboursAndSelf((position.Item1, position.Item2 + 1), area);
        }
    }

    public List<(uint, uint)> FindCell(Func<uint, uint, GridCell, bool> lambda)
    {
        var output = new List<(uint, uint)>();

        for (uint x = 0; x < SizeX; x++)
            for (uint y = 0; y < SizeY; y++)
                if (lambda.Invoke(x, y, Grid[x, y]))
                    output.Add((x, y));

        return output;
    }

    public List<(uint, uint)> FindCellOfType(bool isFloor)
    {
        return FindCell((x, y, cell) => cell.IsFloor == isFloor);
    }

    public List<(uint, uint)> FindCellOfArea(uint area)
    {
        return FindCell((x, y, cell) => cell.Area == area);
    }

    public GridCell? GetCell((int, int) position) => GetCell(Grid, ((uint)position.Item1, (uint)position.Item2));

    public GridCell? GetCell((uint, uint) position) => GetCell(Grid, position);

    public static GridCell? GetCell(GridCell[,] grid, (int, int) position) => GetCell(grid, ((int)position.Item1, (int)position.Item2));

    public static GridCell? GetCell(GridCell[,] grid, (uint, uint) position)
    {
        if (position.Item1 < 0 || position.Item2 < 0 || position.Item1 >= grid.GetUpperBound(0) || position.Item2 >= grid.GetUpperBound(1)) return null;
        else return grid[position.Item1, position.Item2];
    }

    public uint CountNeighboursOfType((uint, uint) position, bool isFloor, bool countNull = true, bool interCardinalsToo = true)
    {
        // Cardinals
        var valueN = GetCell((position.Item1 - 1, position.Item2));
        var valueS = GetCell((position.Item1 + 1, position.Item2));
        var valueE = GetCell((position.Item1, position.Item2 - 1));
        var valueW = GetCell((position.Item1, position.Item2 + 1));

        if (!interCardinalsToo)
        {
            return (uint)(0
                + (valueN == null
                    ? countNull ? 1 : 0
                    : valueN.IsFloor == isFloor ? 1 : 0)
                + (valueS == null
                    ? countNull ? 1 : 0
                    : valueS.IsFloor == isFloor ? 1 : 0)
                + (valueE == null
                    ? countNull ? 1 : 0
                    : valueE.IsFloor == isFloor ? 1 : 0)
                + (valueW == null
                    ? countNull ? 1 : 0
                    : valueW.IsFloor == isFloor ? 1 : 0)
            );
        }
        else
        {
            // Inter-Cardinals
            var valueNE = GetCell((position.Item1 - 1, position.Item2 - 1));
            var valueSE = GetCell((position.Item1 + 1, position.Item2 - 1));
            var valueNW = GetCell((position.Item1 - 1, position.Item2 + 1));
            var valueSW = GetCell((position.Item1 + 1, position.Item2 + 1));

            return (uint)(0
                + (valueN == null
                    ? countNull ? 1 : 0
                    : valueN.IsFloor == isFloor ? 1 : 0)
                + (valueS == null
                    ? countNull ? 1 : 0
                    : valueS.IsFloor == isFloor ? 1 : 0)
                + (valueE == null
                    ? countNull ? 1 : 0
                    : valueE.IsFloor == isFloor ? 1 : 0)
                + (valueW == null
                    ? countNull ? 1 : 0
                    : valueW.IsFloor == isFloor ? 1 : 0)
                + (valueNE == null
                    ? countNull ? 1 : 0
                    : valueNE.IsFloor == isFloor ? 1 : 0)
                + (valueSE == null
                    ? countNull ? 1 : 0
                    : valueSE.IsFloor == isFloor ? 1 : 0)
                + (valueNW == null
                    ? countNull ? 1 : 0
                    : valueNW.IsFloor == isFloor ? 1 : 0)
                + (valueSW == null
                    ? countNull ? 1 : 0
                    : valueSW.IsFloor == isFloor ? 1 : 0)
            );
        }
    }

    public void PrintToConsole()
    {
        var output = "";
        for (var x = 0; x < SizeX; x++)
        {
            for (var y = 0; y < SizeY; y++)
                output += Grid[x, y].GetCellString(y % 2 == 0);

            output += "\n";
        }
        GD.Print(output);
    }
}
