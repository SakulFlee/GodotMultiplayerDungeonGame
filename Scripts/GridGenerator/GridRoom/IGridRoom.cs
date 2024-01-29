public interface IGridRoom
{
    public (uint, uint) GetPosition();

    public (uint, uint) GetMaxSize();

    public GridRoomType GetType();

    public uint GetRoomId();

    public bool IsInsideRoom((uint, uint) position)
    => position.Item1 >= GetPosition().Item1 &&
            position.Item2 >= GetPosition().Item2 &&
            position.Item1 < GetPosition().Item1 + GetMaxSize().Item1 &&
            position.Item2 < GetPosition().Item2 + GetMaxSize().Item2;

    public GridCell[,] Apply(GridCell[,] grid)
    {
        for (uint x = 0; x < GetMaxSize().Item1; x++)
            for (uint y = 0; y < GetMaxSize().Item2; y++)
            {
                var actualX = GetPosition().Item1 + x;
                var actualY = GetPosition().Item2 + y;

                // If out-of-bounce, skip
                if (GridGenerator.GetCell(grid, (actualX, actualY)) == null) continue;

                // Set wall to edge tiles
                if (x == 0 || y == 0 || x == GetMaxSize().Item1 - 1 || y == GetMaxSize().Item2 - 1)
                    grid[actualX, actualY].IsFloor = false;
                // Not on an edge, so floor
                else
                    grid[actualX, actualY].IsFloor = true;

                grid[actualX, actualY].Room = GetRoomId();
            }

        return grid;
    }
}