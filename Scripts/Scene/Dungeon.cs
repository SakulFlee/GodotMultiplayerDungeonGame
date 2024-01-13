using Godot;

public partial class Dungeon : Node3D
{
    private GridMap gridMap;

    public override void _EnterTree()
    {
        gridMap = GetNode<GridMap>("%GridMap");
    }

    public override void _Ready()
    {
        gridMap.Clear();

        var dirtId = -1;
        var stoneId = -1;

        foreach (var itemId in gridMap.MeshLibrary.GetItemList())
            switch (gridMap.MeshLibrary.GetItemName(itemId))
            {
                case "Dirt":
                    dirtId = itemId;
                    break;
                case "Stone":
                    stoneId = itemId;
                    break;
            }

        if (dirtId == -1) GD.PrintErr("Didn't find Dirt item id!");
        if (stoneId == -1) GD.PrintErr("Didn't find Stone item id!");

        for (var i = 0; i < 10; i++)
        {
            var d = new DungeonRoomGenerator(40, 40);
            d.DoWork(10, roomType: DungeonRoomType.Circular);
            d.Print();

            var startX = (i > 4 ? i - 5 : i) * 40;
            var startY = (i % 3) * 40;

            for (var x = 0; x <= d.GetWidth(); x++)
                for (var y = 0; y <= d.GetHeight(); y++)
                {
                    var cell = d.GetCell(x, y);

                    if (cell == DungeonRoomGenerator.FLOOR)
                    {
                        gridMap.SetCellItem(new Vector3I(startX + x, 0, startY + y), dirtId);
                    }
                    else if (cell == DungeonRoomGenerator.WALL)
                    {
                        gridMap.SetCellItem(new Vector3I(startX + x, 0, startY + y), dirtId);
                        gridMap.SetCellItem(new Vector3I(startX + x, 1, startY + y), stoneId);
                    }
                }
        }
    }
}
