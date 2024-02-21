#nullable enable

using System.Collections.Generic;

public class GridConnection
{
    public uint id { get; private set; }

    public bool isArea { get; private set; }

    public List<uint> areaConnections { get; private set; } = new();
    public List<uint> roomConnections { get; private set; } = new();

    public bool isOnMainPath { get; private set; } = false;

    public static List<GridConnection>? BuildFromGenerator(GridGenerator generator)
    {
        bool bossRoomFound = false;

        // Make a list of rooms and areas and find connections
        var dictionary = new Dictionary<(uint, bool), GridConnection>();
        for (uint roomId = 1; roomId <= generator.RoomCount; roomId++)
        {
            dictionary.Add((roomId, false), new GridConnection(roomId, false));
            foreach (var possibleConnection in CheckForConnections(generator, roomId))
                if (possibleConnection.Item2)
                    dictionary[(roomId, false)].roomConnections.Add(possibleConnection.Item1);
                else
                    dictionary[(roomId, false)].areaConnections.Add(possibleConnection.Item1);
        }
        for (uint areaId = 1; areaId <= generator.AreaCount; areaId++)
        {
            dictionary.Add((areaId, true), new GridConnection(areaId, true));
            foreach (var possibleConnection in CheckForConnections(generator, areaId))
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
    public static HashSet<(uint, bool)> CheckForConnections(GridGenerator generator, uint roomId)
    {
        var result = new HashSet<(uint, bool)>();

        var possibleDoorCells = generator.FindCell((x, y, cell) =>
            cell.CanBeDoor &&
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

    private GridConnection(uint id, bool isArea)
    {
        this.id = id;
        this.isArea = isArea;
    }
}