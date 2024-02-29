public class GridRoomRectangular : IGridRoom
{
    public Vector2I Position { get; private set; }
    public Vector2I MaxSize { get; private set; }

    public GridRoomRectangular(Vector2I position, Vector2I maxSize)
    {
        Position = position;
        MaxSize = maxSize;
    }

    GridRoomType IGridRoom.GetType() => GridRoomType.Rectangular;

    public Vector2I GetPosition() => Position;

    public Vector2I GetSize() => MaxSize;
}
