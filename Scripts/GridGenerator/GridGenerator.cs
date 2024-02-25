#nullable enable

using System;
using System.Collections.Generic;

public class GridGenerator
{
    public int roomSizeMinimum { get; set; } = 5;
    public int roomSizeMaximum { get; set; } = 30;
    public int minimumNeighbourWallsForFloor { get; set; } = 4;
    public int smallAreaThresholdCells { get; set; } = 9; // TODO

    public Random R = Random.Shared;

    /// <summary>
    /// The floor grid. 
    /// If at a given v = (X, Y) location the array returns true, 
    /// the probed cell is a floor. If it returns false instead,
    /// the probed tile is a wall.
    /// </summary>
    public bool[,] floorGrid { get; private set; } = new bool[0, 0];

    /// <summary>
    /// The area grid.
    /// Returns the area ID at a given location v = (X, Y).
    /// Or 0, if the probed cell doesn't have an area assigned.
    /// </summary>
    public uint[,] areaGrid { get; private set; } = new uint[0, 0];

    /// <summary>
    /// The door grid.
    /// If at a given v = (X, Y) location the array returns true, 
    /// the probed cell is a door. If it returns false instead,
    /// the probed tile is a NOT a door (e.g. a wall instead, check
    /// other layers though!).
    /// </summary>
    public bool[,] doorGrid { get; private set; } = new bool[0, 0];

    public Vector2I gridSize = Vector2I.Zero;

    public uint maxArea { get; private set; } = 0;

    public Vector2I portalLocation { get; private set; } = Vector2I.Zero;
    public uint bossAreaId { get; private set; } = 0;
    public List<GridConnection> gridConnections { get; private set; } = new();

    public GridGenerator(int seed = 12345)
    {
        InitializeRandomness(seed);
    }

    /// <summary>
    /// This will set (or reset) the internally used randomness with a given
    /// seed. If said randomness has been used already, it will effectively be
    /// reset, thus repeating the exact same results.
    /// </summary>
    /// <param name="seed">The seed to be initialized with</param>
    public void InitializeRandomness(int seed = 12345)
    {
        R = new Random(seed);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="gridSize">Size of the Grid starting at 0</param>
    /// <param name="floorPercentage">
    /// Percentage of floor tiles to appear.
    ///   0% equals 0.0
    /// 100% equals 1.0
    /// </param>
    public void InitializeAutomataGrid(
        Vector2I gridSize,
        double floorPercentage = 0.60
    )
    {
        // Sanity checks
        if (gridSize.X <= 0 || gridSize.Y <= 0)
            throw new Exception("GridSize must be > 0 on both X and Y!");
        if (floorPercentage <= 0.0)
            throw new Exception("Floor percentage must be > 0.0");
        if (floorPercentage > 1.0)
            throw new Exception("Floor percentage must be <= 1.0");

        // Assign variables
        this.gridSize = gridSize;
        floorGrid = new bool[gridSize.X, gridSize.Y];
        areaGrid = new uint[gridSize.X, gridSize.Y];
        doorGrid = new bool[gridSize.X, gridSize.Y];

        // Initialize grids with data
        for (var x = 0; x < gridSize.X; x++)
            for (var y = 0; y < gridSize.Y; y++)
            {
                floorGrid[x, y] = R.NextDouble() <= floorPercentage;
                areaGrid[x, y] = 0;
                doorGrid[x, y] = false;
            }
    }

    /// <summary>
    /// Automatically performs all necessary steps to generate a grid dungeon.
    /// 
    /// A dungeon is valid if there is a path from the portal
    /// to the boss room.
    /// </summary>
    /// <param name="gridSize">Size of the grid, must be > 0.</param>
    /// <param name="floorPercentage">Percentage of floor tiles to appear in 
    /// the initial randomized grid. 0% = 0.0; 100% = 1.0.</param>
    /// <returns>true, if the dungeon is valid, false otherwise.</returns>
    public bool Automate(Vector2I gridSize, double floorPercentage = 0.60)
    {
        bool result;

        // Initialize the grid randomly, using the size and percentage given
        InitializeAutomataGrid(gridSize, floorPercentage);

        // Repetitively, perform cellular automata
        PerformAutomataRepetitive();

        GD.Print("Before:");
        PrintToConsole();

        // Place rooms
        var rooms = MakeRandomizedRoomQueue();
        PlaceRooms(rooms);

        // Ensure that all edges of the grid are walls
        EnsureEdgesOfGridAreWalls();

        // Assign areas to each enclosed floor area
        AssignAreas();

        // Assign the boss room and portal location
        AssignBossRoom();
        AssignPortalLocation();

        // Find possible door locations and place doors where needed
        var doorwayGrid = CheckForDoorways();
        result = PlaceDoors(doorwayGrid);

        return result;
    }

    public void AssignPortalLocation()
    {
        do
        {
            var pool = new HashSet<Vector2I>();
            foreach (var roomCell in FindCell((v, floor, area, door) =>
                floor &&
                area > 0 &&
                (GetFloorCell(v + new Vector2I(-1, 0)) ?? false) &&
                (GetFloorCell(v + new Vector2I(1, 0)) ?? false) &&
                (GetFloorCell(v + new Vector2I(0, -1)) ?? false) &&
                (GetFloorCell(v + new Vector2I(0, 1)) ?? false)))
                pool.Add(roomCell);

            portalLocation = pool.ElementAt(R.Next(pool.Count));
        } while (GetAreaCell(portalLocation) != bossAreaId);
    }

    public void AssignBossRoom()
    {
        bossAreaId = FindBiggestArea();
    }

    public bool PlaceDoors(bool[,] doorwayGrid)
    {
        // Compute connections between rooms and areas.
        // On failure (== no connection between main and boss room), 
        // return false and end early.
        var gridConnectionsResult = GridConnection.BuildFromGenerator(this, doorwayGrid);
        if (gridConnectionsResult == null) return false;
        else
        {
            gridConnections.Clear();
            gridConnections.AddRange(gridConnectionsResult);
        }

        // Put all connections that have been found in a queue for processing.
        // Tuple: (From Room/Area ID, is from ID area?, To Room/Area ID, is to ID area?)
        var connectionsToBeMade = new Queue<(uint, uint)>();
        foreach (var gridConnection in gridConnections)
        {
            foreach (var areaConnection in gridConnection.areaConnections)
                connectionsToBeMade.Enqueue((gridConnection.id, areaConnection));
        }

        while (connectionsToBeMade.Count() > 0)
        {
            var connectionToBeMade = connectionsToBeMade.Dequeue();

            // Find all "possible door cell" candidates that match the
            // from and to ID (and area/room specification)
            // TODO
            var connectedTiles = GridConnection.FindConnectingCells(this, doorwayGrid, connectionToBeMade.Item1, connectionToBeMade.Item2);

            GD.Print($"{connectionToBeMade.Item1} -> {connectionToBeMade.Item2}: {connectedTiles.Count()}");

            // Pick any of the candidates and mark it as a door
            // TODO
        }

        return true;
    }

    public uint FindBiggestArea()
    {
        uint biggestAreaId = 0;
        var biggestAreaCount = 0;
        for (uint areaId = 1; areaId <= maxArea; areaId++)
        {
            var areaCells = FindAreaCells(areaId);
            if (areaCells.Count() > biggestAreaCount)
            {
                biggestAreaId = areaId;
                biggestAreaCount = areaCells.Count();
            }
        }
        return biggestAreaId;
    }

    public bool[,] CheckForDoorways()
    {
        var result = new bool[gridSize.X, gridSize.Y];
        for (var x = 0; x < gridSize.X; x++)
            for (var y = 0; y < gridSize.Y; y++)
            {
                var v = new Vector2I(x, y);

                // Skip floor tiles
                if (GetFloorCell(v) ?? false) continue;

                var cellN = GetFloorCell(v + new Vector2I(-1, 0));
                var cellS = GetFloorCell(v + new Vector2I(1, 0));
                var cellE = GetFloorCell(v + new Vector2I(0, 1));
                var cellW = GetFloorCell(v + new Vector2I(0, -1));

                result[x, y] = (
                    (cellN ?? false) &&
                    (cellS ?? false) &&
                    (!cellE ?? false) &&
                    (!cellW ?? false)
                ) || (
                    (cellE ?? false) &&
                    (cellW ?? false) &&
                    (!cellN ?? false) &&
                    (!cellS ?? false)
                );
            }
        return result;
    }

    private void EnsureEdgesOfGridAreWalls()
    {
        for (var x = 0; x < gridSize.X; x++)
        {
            floorGrid[x, 0] = false;
            floorGrid[x, gridSize.Y - 1] = false;
        }

        for (var y = 0; y < gridSize.Y; y++)
        {
            floorGrid[0, y] = false;
            floorGrid[gridSize.X - 1, y] = false;
        }
    }

    // private void FixTooSmallOrInvalidPlacedAreas()
    // {
    //     for (uint area = 1; area <= maxArea; area++)
    //     {
    //         uint minX = int.MaxValue;
    //         uint minY = int.MaxValue;
    //         uint maxX = 0;
    //         uint maxY = 0;

    //         var cellsInArea = FindCellOfArea(area);
    //         foreach (var (x, y) in cellsInArea)
    //         {
    //             if (x < minX)
    //             {
    //                 minX = x;
    //             }
    //             else if (y < minY)
    //             {
    //                 minY = y;
    //             }
    //             else if (x > maxX)
    //             {
    //                 maxX = x;
    //             }
    //             else if (y > maxY)
    //             {
    //                 maxY = y;
    //             }
    //         }

    //         var removeArea = minX == int.MaxValue
    //             || minY == int.MaxValue
    //             || maxX == 0
    //             || maxY == 0
    //             || minX == maxX
    //             || minY == maxY
    //             || cellsInArea.Count() < smallAreaThresholdCells;

    //         if (removeArea)
    //             foreach (var (x, y) in cellsInArea)
    //                 Grid[x, y] = new GridCell(isFloor: false);
    //     }
    // }

    // private void FixRedoAreas()
    // {
    //     maxArea = 0;
    //     for (var x = 0; x < GridSizeX; x++)
    //         for (var y = 0; y < GridSizeY; y++)
    //             Grid[x, y].Area = 0;

    //     AssignAreas();
    // }

    // private void FixWalledRooms()
    // {
    //     FindCell((x, y, cell) => !cell.IsFloor && cell.HasRoomData())
    //         .ForEach(i => Grid[i.Item1, i.Item2].Room = 0);
    // }

    // public void FixInvalidAreas(bool fixGridEdges = true, bool redoAreas = true, bool fixWalledRooms = true, bool printToConsole = false)
    // {
    //     if (fixGridEdges)
    //         EnsureEdgesOfGridAreWalls();

    //     FixTooSmallOrInvalidPlacedAreas();

    //     if (redoAreas)
    //         FixRedoAreas();

    //     if (fixWalledRooms)
    //         FixWalledRooms();

    //     if (printToConsole) PrintToConsole();
    // }

    /// <summary>
    /// Creates a queue of randomized rooms to be used in <see cref="PlaceRooms(Queue{IGridRoom})"/>
    /// </summary>
    /// <returns>Queue of randomized rooms</returns>
    public Queue<IGridRoom> MakeRandomizedRoomQueue()
    {
        // Taking the maximum size of our grid and dividing it by three roughly 
        // gives us a value that approximates how many rooms we want to have.
        // Note, that many rooms will overlap and/or be inside of each other.
        // Many rooms will be replaced and lost, thus we have a higher number
        // in rooms here than actually visible in the game!
        var smallerSize = gridSize.X < gridSize.Y ? gridSize.X : gridSize.Y;
        int desiredRoomCount = R.Next(smallerSize / 16, smallerSize / 2);

        var result = new Queue<IGridRoom>();
        while (result.Count() <= desiredRoomCount)
        {
            // Pick a random location
            var roomLocationX = R.Next(0, gridSize.X - 1);
            var roomLocationY = R.Next(0, gridSize.Y - 1);

            // Calculate the maximum size 
            var maxSizeX = gridSize.X - roomLocationX;
            var maxSizeY = gridSize.Y - roomLocationY;

            // Skip rooms that are too small
            if (maxSizeX < roomSizeMinimum ||
                maxSizeY < roomSizeMinimum) continue;

            // Pick a maximum room size, based on the smaller value
            var roomSizeX = R.Next(
                roomSizeMinimum,
                maxSizeX < roomSizeMaximum ? maxSizeX : roomSizeMaximum
            );
            var roomSizeY = R.Next(
                roomSizeMinimum,
                maxSizeY < roomSizeMaximum ? maxSizeY : roomSizeMaximum
            );

            // Make the room construct and put it in the queue
            var room = new GridRoomRectangular(
                new Vector2I(roomLocationX, roomLocationY),
                new Vector2I(roomSizeX, roomSizeY),
                (uint)result.Count() + 1
            );
            result.Enqueue(room);
        }

        return result;
    }

    public void PlaceRooms(Queue<IGridRoom> roomQueue)
    {
        while (roomQueue.Count() > 0)
        {
            var room = roomQueue.Dequeue();
            floorGrid = room.Apply(floorGrid);
        }
    }

    // public void FixAreas()
    // {
    //     var floorsWithoutAreaList = FindCell((x, y, cell) => cell.IsFloor && cell.Area == 0);
    //     var floorsWithoutArea = new Queue<(uint, uint)>(floorsWithoutAreaList.Count());
    //     foreach (var a in floorsWithoutAreaList) floorsWithoutArea.Enqueue(a);

    //     while (floorsWithoutArea.Count() > 0)
    //     {
    //         var cellPosition = floorsWithoutArea.Dequeue();

    //         var cellN = GetCell((cellPosition.Item1 - 1, cellPosition.Item2));
    //         if (cellN != null && cellN.Area > 0)
    //         {
    //             Grid[cellPosition.Item1, cellPosition.Item2].Area = cellN.Area;
    //             continue;
    //         }

    //         var cellS = GetCell((cellPosition.Item1 + 1, cellPosition.Item2));
    //         if (cellS != null && cellS.Area > 0)
    //         {
    //             Grid[cellPosition.Item1, cellPosition.Item2].Area = cellS.Area;
    //             continue;
    //         }

    //         var cellE = GetCell((cellPosition.Item1, cellPosition.Item2 - 1));
    //         if (cellE != null && cellE.Area > 0)
    //         {
    //             Grid[cellPosition.Item1, cellPosition.Item2].Area = cellE.Area;
    //             continue;
    //         }

    //         var cellW = GetCell((cellPosition.Item1, cellPosition.Item2 + 1));
    //         if (cellW != null && cellW.Area > 0)
    //         {
    //             Grid[cellPosition.Item1, cellPosition.Item2].Area = cellW.Area;
    //             continue;
    //         }

    //         floorsWithoutArea.Enqueue(cellPosition);
    //     }
    // }

    public void PerformAutomataRepetitive(int steps = 5)
    {
        for (var step = 0; step < steps; step++)
        {
            PerformAutomata();

            // GD.Print($"> #{step}");
            // PrintToConsole();
        }
    }

    public void PerformAutomata()
    {
        var outputGrid = new bool[gridSize.X, gridSize.Y];

        for (var x = 0; x < gridSize.X; x++)
            for (var y = 0; y < gridSize.Y; y++)
                outputGrid[x, y] = CountWallNeighbours(
                    new Vector2I(x, y),
                    countNull: true) < minimumNeighbourWallsForFloor;

        floorGrid = outputGrid;
    }

    public void AssignAreas()
    {
        uint currentArea = 1;
        var currentPosition = new Vector2I(-1, 0);
        do
        {
            // Increment X & Y
            currentPosition += new Vector2I(1, 0);
            if (currentPosition.X >= gridSize.X)
            {
                currentPosition = new Vector2I(0, currentPosition.Y + 1);

                // Break the loop if we reached the end
                if (currentPosition.Y >= gridSize.Y)
                    break;
            }

            // If the cell at the current location is invalid, not a floor or already has an area assigned, skip it.
            // Note: this has to stay in here to not advance the area counter
            // This also is checked inside AssignArea.
            var floorCell = GetFloorCell(currentPosition);
            var areaCell = GetAreaCell(currentPosition);
            if (!(floorCell ?? false) || areaCell != 0) continue;

            // Cell must be a floor AND in an area we haven't been in yet
            AssignArea(currentPosition, currentArea);

            // Increment area counter
            currentArea++;
        } while (true);

        // Set counter
        maxArea = currentArea;
    }

    public void AssignArea(Vector2I position, uint area)
    {
        // Skip any invalid cells
        var floorCell = GetFloorCell(position);
        var areaCell = GetAreaCell(position);
        if (!(floorCell ?? false) || areaCell != 0) return;

        // Assign area
        areaGrid[position.X, position.Y] = area;

        // Recursively call this functions on any neighbour that is not null,
        // i.e. exists, and is a floor
        var positionN = position + new Vector2I(-1, 0);
        var positionS = position + new Vector2I(1, 0);
        var positionW = position + new Vector2I(0, -1);
        var positionE = position + new Vector2I(0, 1);

        var cellN = GetFloorCell(positionN);
        var cellS = GetFloorCell(positionS);
        var cellW = GetFloorCell(positionW);
        var cellE = GetFloorCell(positionE);

        if (cellN ?? false)
            AssignArea(positionN, area);
        if (cellS ?? false)
            AssignArea(positionS, area);
        if (cellW ?? false)
            AssignArea(positionW, area);
        if (cellE ?? false)
            AssignArea(positionE, area);
    }

    public HashSet<Vector2I> FindCell(Func<Vector2I, bool, uint, bool, bool> lambda)
    {
        var output = new HashSet<Vector2I>();

        for (var x = 0; x < gridSize.X; x++)
            for (var y = 0; y < gridSize.Y; y++)
            {
                var v = new Vector2I(x, y);

                var floorCell = floorGrid[x, y];
                var areaCell = areaGrid[x, y];
                var doorCell = doorGrid[x, y];

                if (lambda.Invoke(v, floorCell, areaCell, doorCell))
                    output.Add(v);
            }

        return output;
    }

    public HashSet<Vector2I> FindFloorCells() =>
        FindCell((v, floor, area, door) => floor);

    public HashSet<Vector2I> FindWallCells() =>
    FindCell((v, floor, area, door) => !floor);

    public HashSet<Vector2I> FindDoorCells() =>
            FindCell((v, floor, area, door) => door);

    public HashSet<Vector2I> FindAreaCells(uint targetArea) =>
        FindCell((v, floor, area, door) => floor && area == targetArea);

    public bool? GetFloorCell(Vector2I position)
    {
        if (position.X < 0 ||
            position.Y < 0 ||
            position.X >= floorGrid.GetUpperBound(0) ||
            position.Y >= floorGrid.GetUpperBound(1))
            return null;
        else
            return floorGrid[position.X, position.Y];
    }

    public uint? GetAreaCell(Vector2I position)
    {
        if (position.X < 0 ||
            position.Y < 0 ||
            position.X >= floorGrid.GetUpperBound(0) ||
            position.Y >= floorGrid.GetUpperBound(1))
            return null;
        else
            return areaGrid[position.X, position.Y];
    }

    public bool? GetDoorCell(Vector2I position)
    {
        if (position.X < 0 ||
            position.Y < 0 ||
            position.X >= floorGrid.GetUpperBound(0) ||
            position.Y >= floorGrid.GetUpperBound(1))
            return null;
        else
            return doorGrid[position.X, position.Y];
    }

    public uint CountWallNeighbours(Vector2I position, bool countNull = true, bool interCardinalsToo = true)
    {
        // Cardinals
        var valueN = GetFloorCell(position + new Vector2I(-1, 0));
        var valueS = GetFloorCell(position + new Vector2I(1, 0));
        var valueE = GetFloorCell(position + new Vector2I(0, -1));
        var valueW = GetFloorCell(position + new Vector2I(0, 1));

        if (!interCardinalsToo)
        {
            // Cardinals
            return (uint)(((valueN ?? countNull) ? 0 : 1)
                + ((valueS ?? countNull) ? 0 : 1)
                + ((valueW ?? countNull) ? 0 : 1)
                + ((valueE ?? countNull) ? 0 : 1));
        }
        else
        {
            // Inter-Cardinals
            var valueNE = GetFloorCell(position + new Vector2I(-1, -1));
            var valueSE = GetFloorCell(position + new Vector2I(1, -1));
            var valueNW = GetFloorCell(position + new Vector2I(-1, 1));
            var valueSW = GetFloorCell(position + new Vector2I(1, 1));

            return (uint)(((valueN ?? countNull) ? 0 : 1)
                 + ((valueS ?? countNull) ? 0 : 1)
                 + ((valueW ?? countNull) ? 0 : 1)
                 + ((valueE ?? countNull) ? 0 : 1)
                 + ((valueNE ?? countNull) ? 0 : 1)
                 + ((valueSE ?? countNull) ? 0 : 1)
                 + ((valueNW ?? countNull) ? 0 : 1)
                 + ((valueSW ?? countNull) ? 0 : 1));
        }
    }

    public void PrintToConsole()
    {
        var output = "";
        for (var y = 0; y < gridSize.Y; y++)
        {
            for (var x = 0; x < gridSize.X; x++)
                output +=
                    x == portalLocation.X &&
                    y == portalLocation.Y
                        ? "PP"
                        : floorGrid[x, y]
                            ? "  "
                            : "██";

            output += "\n";
        }
        GD.Print(output);
    }
}
