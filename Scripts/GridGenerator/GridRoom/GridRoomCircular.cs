using System;

public class GridRoomCircular : IGridRoom
{
    public Vector2I Position { get; private set; }
    public Vector2I MaxSize { get; private set; }

    public GridRoomCircular(Vector2I position, Vector2I maxSize)
    {
        Position = position;
        MaxSize = maxSize;
    }

    public bool[,] Apply(bool[,] floorGrid)
    {
        var startX = GetPosition().X;
        var startY = GetPosition().Y;
        var endX = startX + GetSize().X;
        var endY = startY + GetSize().Y;

        int radius = (GetSize().X < GetSize().Y ? GetSize().X : GetSize().Y) / 2;
        var radiusSquared = Math.Pow(radius, 2);
        var radiusWallSquared = Math.Pow(radius - 1, 2);

        var centerX = startX + radius;
        var centerY = startY + radius;
        var center = new Vector2(centerX, centerY);

        for (var x = startX; x < endX; x++)
            for (var y = startY; y < endY; y++)
            {
                // Skip if out-of-bounce
                if (x < 0 ||
                    y < 0 ||
                    x >= floorGrid.GetUpperBound(0) ||
                    y >= floorGrid.GetUpperBound(1)) continue;

                var position = new Vector2(x, y);
                var distanceToCenter = position.DistanceSquaredTo(center);

                // Check if in bounds of the radius, if so check if we are about
                // one tile away from the outer edge. If yes, place down a wall
                // instead of a floor.
                if (distanceToCenter < radiusSquared)
                    floorGrid[x, y] = distanceToCenter < radiusWallSquared;
            }
        return floorGrid;
    }

    GridRoomType IGridRoom.GetType() => GridRoomType.Rectangular;

    public Vector2I GetPosition() => Position;

    public Vector2I GetSize() => MaxSize;
}
