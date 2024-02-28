public interface IGridRoom
{
    public Vector2I GetPosition();
    public Vector2I GetSize();
    public GridRoomType GetType();

    public bool[,] Apply(bool[,] floorGrid)
    {
        var startX = GetPosition().X;
        var startY = GetPosition().Y;
        var endX = startX + GetSize().X;
        var endY = startY + GetSize().Y;

        for (var x = startX; x < endX; x++)
            for (var y = startY; y < endY; y++)
            {
                // Skip if out-of-bounce
                if (x < 0 ||
                    y < 0 ||
                    x >= floorGrid.GetUpperBound(0) ||
                    y >= floorGrid.GetUpperBound(1)) continue;

                // Set any outer edge tiles to walls
                if (x == startX ||
                    y == startY ||
                    x == endX - 1 ||
                    y == endY - 1)
                    floorGrid[x, y] = false;
                // and everything else to floor
                else
                    floorGrid[x, y] = true;
            }
        return floorGrid;
    }
}