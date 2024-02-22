#nullable enable

using System.Collections.Generic;

public class GridConnection
{
    public uint id { get; private set; }

    public bool isArea { get; private set; }

    public List<uint> areaConnections { get; private set; } = new();
    public List<uint> roomConnections { get; private set; } = new();

    public bool isOnMainPath { get; private set; } = false;

    public static List<GridConnection>? BuildFromGenerator(GridGenerator generator, bool[,] doorwayGrid)
    {
        bool bossRoomFound = false;

        // Make a list of rooms and areas and find connections
        var dictionary = new Dictionary<(uint, bool), GridConnection>();
        for (uint roomId = 1; roomId <= generator.RoomCount; roomId++)
        {
            dictionary.Add((roomId, false), new GridConnection(roomId, false));
            foreach (var possibleConnection in CheckForConnections(generator, roomId, doorwayGrid))
                if (possibleConnection.Item1 != roomId)
                    if (possibleConnection.Item2)
                        dictionary[(roomId, false)].roomConnections.Add(possibleConnection.Item1);
                    else
                        dictionary[(roomId, false)].areaConnections.Add(possibleConnection.Item1);
        }
        for (uint areaId = 1; areaId <= generator.AreaCount; areaId++)
        {
            dictionary.Add((areaId, true), new GridConnection(areaId, true));
            foreach (var possibleConnection in CheckForConnections(generator, areaId, doorwayGrid))
                if (possibleConnection.Item1 != areaId)
                    if (possibleConnection.Item2)
                        dictionary[(areaId, true)].areaConnections.Add(possibleConnection.Item1);
                    else
                        dictionary[(areaId, true)].areaConnections.Add(possibleConnection.Item1);
        }

        // Start queue at portal room
        var queue = new Queue<(uint, bool)>();
        var portalRoom = generator.GetCell(generator.PortalLocation)!.Room;
        queue.Enqueue((portalRoom, false));

        do
        {
            var currentEntry = queue.Dequeue();
            var currentGraph = dictionary[(currentEntry.Item1, currentEntry.Item2)];

            // Set as being on the main path
            currentGraph.isOnMainPath = true;

            // check for boss room
            if (!currentGraph.isArea && currentGraph.id == generator.BossRoomId)
                bossRoomFound = true;

            // add other non-main path rooms
            foreach (var neighbourRoom in currentGraph.roomConnections)
            {
                var neighbourEntry = dictionary[(neighbourRoom, false)];
                if (!neighbourEntry.isOnMainPath)
                    queue.Enqueue((neighbourRoom, false));
            }
            foreach (var neighbourArea in currentGraph.areaConnections)
            {
                var neighbourEntry = dictionary[(neighbourArea, true)];
                if (!neighbourEntry.isOnMainPath)
                    queue.Enqueue((neighbourArea, true));
            }
        } while (queue.Count() > 0);

        if (bossRoomFound)
        {
            var output = new List<GridConnection>();
            foreach (var gridConnection in dictionary.Values)
                output.Add(gridConnection);
            return output;
        }
        else return null;
    }

    /// <summary>
    /// First parameter is the area or room ID
    /// Second parameter will be FALSE if room, TRUE if area
    /// </summary>
    /// <param name="generator"></param>
    /// <param name="roomId"></param>
    public static HashSet<(uint, bool)> CheckForConnections(GridGenerator generator, uint roomId, bool[,] doorwayGrid)
    {
        var result = new HashSet<(uint, bool)>();

        var possibleDoorCells = generator.FindCell((x, y, cell) =>
            doorwayGrid[x, y] &&
            (
                (generator.GetCell((x - 1, y))?.Room == roomId) ||
                (generator.GetCell((x + 1, y))?.Room == roomId) ||
                (generator.GetCell((x, y - 1))?.Room == roomId) ||
                (generator.GetCell((x, y + 1))?.Room == roomId)
            )
        );

        foreach (var possibleDoorCell in possibleDoorCells)
        {
            var cellN = generator.GetCell((
                possibleDoorCell.Item1 - 1,
                possibleDoorCell.Item2
            ));
            var cellS = generator.GetCell((
                possibleDoorCell.Item1 + 1,
                possibleDoorCell.Item2
            ));
            var cellW = generator.GetCell((
                possibleDoorCell.Item1,
                possibleDoorCell.Item2 - 1
            ));
            var cellE = generator.GetCell((
                possibleDoorCell.Item1,
                possibleDoorCell.Item2 + 1
            ));

            uint id;
            bool isArea;

            if (cellN?.HasRoomData() ?? false && cellN.Room != roomId)
            {
                id = cellN.Room;
                isArea = false;
            }
            else if (cellS?.HasRoomData() ?? false && cellS.Room != roomId)
            {
                id = cellS.Room;
                isArea = false;
            }
            else if (cellW?.HasRoomData() ?? false && cellW.Room != roomId)
            {
                id = cellW.Room;
                isArea = false;
            }
            else if (cellE?.HasRoomData() ?? false && cellE.Room != roomId)
            {
                id = cellE.Room;
                isArea = false;
            }
            else if (cellN?.HasAreaData() ?? false)
            {
                id = cellN.Area;
                isArea = true;
            }
            else if (cellS?.HasAreaData() ?? false)
            {
                id = cellS.Area;
                isArea = true;
            }
            else if (cellW?.HasAreaData() ?? false)
            {
                id = cellW.Area;
                isArea = true;
            }
            else if (cellE?.HasAreaData() ?? false)
            {
                id = cellE.Area;
                isArea = true;
            }
            else continue;

            result.Add((id, isArea));
        }

        return result;
    }

    public static HashSet<(uint, uint)> FindConnectingCells(GridGenerator generator, bool[,] doorwayGrid, (uint, bool) from, (uint, bool) to)
    {
        var result = new HashSet<(uint, uint)>();

        var cellsToCheck = new Queue<(uint, uint)>();
        for (uint x = 0; x < generator.SizeX; x++)
            for (uint y = 0; y < generator.SizeY; y++)
                if (doorwayGrid[x, y])
                    cellsToCheck.Enqueue((x, y));

        while (cellsToCheck.Count() > 0)
        {
            var cellToCheck = cellsToCheck.Dequeue();

            var cellN = generator.GetCell((
                cellToCheck.Item1 - 1,
                cellToCheck.Item2
            ));
            var cellS = generator.GetCell((
                cellToCheck.Item1 + 1,
                cellToCheck.Item2
            ));
            var cellW = generator.GetCell((
                cellToCheck.Item1,
                cellToCheck.Item2 - 1
            ));
            var cellE = generator.GetCell((
                cellToCheck.Item1,
                cellToCheck.Item2 + 1
            ));

            var foundFrom = from.Item2
                ? // Looking for area
                    cellN?.Area == from.Item1 ||
                    cellS?.Area == from.Item1 ||
                    cellE?.Area == from.Item1 ||
                    cellW?.Area == from.Item1
                : // Looking for room
                    cellN?.Room == from.Item1 ||
                    cellS?.Room == from.Item1 ||
                    cellE?.Room == from.Item1 ||
                    cellW?.Room == from.Item1;
            var foundTo = to.Item2
                ? // Looking for area
                    cellN?.Area == to.Item1 ||
                    cellS?.Area == to.Item1 ||
                    cellE?.Area == to.Item1 ||
                    cellW?.Area == to.Item1
                : // Looking for room
                    cellN?.Room == to.Item1 ||
                    cellS?.Room == to.Item1 ||
                    cellE?.Room == to.Item1 ||
                    cellW?.Room == to.Item1;

            if (foundFrom && foundTo) result.Add(cellToCheck);
        }

        return result;
    }

    private GridConnection(uint id, bool isArea)
    {
        this.id = id;
        this.isArea = isArea;
    }
}