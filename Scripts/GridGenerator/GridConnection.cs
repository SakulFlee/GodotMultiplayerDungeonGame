#nullable enable

using System.Collections.Generic;

public class GridConnection
{
    public uint id { get; private set; }

    public List<uint> areaConnections { get; private set; } = new();

    public bool isOnMainPath { get; private set; } = false;

    public static List<GridConnection>? BuildFromGenerator(GridGenerator generator, bool[,] doorwayGrid)
    {
        bool bossRoomFound = false;

        // Make a list of areas and find connections
        var dictionary = new Dictionary<uint, GridConnection>();
        for (uint areaId = 1; areaId <= generator.maxArea; areaId++)
        {
            dictionary.Add(areaId, new GridConnection(areaId));
            foreach (var possibleConnection in CheckForConnections(generator, areaId, doorwayGrid))
                if (possibleConnection != areaId)
                    dictionary[areaId].areaConnections.Add(possibleConnection);
        }

        // Start queue at portal room
        var queue = new Queue<uint>();
        var portalRoom = generator.areaGrid[
            generator.portalLocation.X,
            generator.portalLocation.Y
        ];
        queue.Enqueue(portalRoom);

        do
        {
            var currentEntry = queue.Dequeue();
            var currentGraph = dictionary[currentEntry];

            // Set as being on the main path
            currentGraph.isOnMainPath = true;

            // check for boss room
            if (currentGraph.id == generator.bossAreaId)
                bossRoomFound = true;

            // add other non-main path areas
            foreach (var neighbourArea in currentGraph.areaConnections)
            {
                var neighbourEntry = dictionary[neighbourArea];
                if (!neighbourEntry.isOnMainPath)
                    queue.Enqueue(neighbourArea);
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
    public static HashSet<uint> CheckForConnections(
        GridGenerator generator,
        uint roomId,
        bool[,] doorwayGrid)
    {
        var result = new HashSet<uint>();

        var possibleDoorCells = generator.FindCell((v, floor, area, door) =>
            doorwayGrid[v.X, v.Y] &&
            ((generator.GetAreaCell(v + new Vector2I(-1, 0)) == roomId) ||
                (generator.GetAreaCell(v + new Vector2I(1, 0)) == roomId) ||
                (generator.GetAreaCell(v + new Vector2I(0, -1)) == roomId) ||
                (generator.GetAreaCell(v + new Vector2I(0, 1)) == roomId))
        );

        foreach (var possibleDoorCell in possibleDoorCells)
        {
            var cellN = generator.GetAreaCell(possibleDoorCell + new Vector2I(-1, 0));
            var cellS = generator.GetAreaCell(possibleDoorCell + new Vector2I(1, 0));
            var cellW = generator.GetAreaCell(possibleDoorCell + new Vector2I(0, -1));
            var cellE = generator.GetAreaCell(possibleDoorCell + new Vector2I(0, 1));

            uint? id;
            if (cellN != null && cellN != roomId)
            {
                id = cellN;
            }
            else if (cellS != null && cellS! != roomId)
            {
                id = cellS;
            }
            else if (cellW != null && cellW! != roomId)
            {
                id = cellW;
            }
            else if (cellE != null && cellE! != roomId)
            {
                id = cellE;
            }
            else continue;

            var x = id ?? 0;
            if (x == 0) continue;

            result.Add(x);
        }

        return result;
    }

    public static HashSet<Vector2I> FindConnectingCells(
        GridGenerator generator,
        bool[,] doorwayGrid,
        uint from,
        uint to)
    {
        var result = new HashSet<Vector2I>();

        var cellsToCheck = new Queue<Vector2I>();
        for (var x = 0; x < generator.gridSize.X; x++)
            for (var y = 0; y < generator.gridSize.Y; y++)
                if (doorwayGrid[x, y])
                    cellsToCheck.Enqueue(new Vector2I(x, y));

        while (cellsToCheck.Count() > 0)
        {
            var cellToCheck = cellsToCheck.Dequeue();

            var cellN = generator.GetAreaCell(cellToCheck + new Vector2I(-1, 0));
            var cellS = generator.GetAreaCell(cellToCheck + new Vector2I(1, 0));
            var cellW = generator.GetAreaCell(cellToCheck + new Vector2I(0, -1));
            var cellE = generator.GetAreaCell(cellToCheck + new Vector2I(0, 1));

            var foundFrom = cellN == from ||
                    cellS == from ||
                    cellE == from ||
                    cellW == from;
            var foundTo = cellN == to ||
                    cellS == to ||
                    cellE == to ||
                    cellW == to;

            if (foundFrom && foundTo) result.Add(cellToCheck);
        }

        return result;
    }

    private GridConnection(uint id)
    {
        this.id = id;
    }
}