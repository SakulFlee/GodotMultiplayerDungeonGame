using System;

public class GridCell
{
    public bool IsFloor { get; set; }
    public uint Area { get; set; } = 0;
    public uint Room { get; set; } = 0;
    public bool CanBeDoor { get; set; } = false;

    public GridCell(bool isFloor)
    {
        IsFloor = isFloor;
    }

    public string GetCellString(bool alt = false)
    {
        if (!IsFloor)
            if (CanBeDoor)
                return "▒▒";
            else
                return "██";

        if (HasRoomData())
        {
            if (Room < 10)
                return $"R{Room}";
            else
                return $"R{Convert.ToString(Room).ToCharArray()[alt ? 1 : 0]}";
        }

        if (Area < 10)
            return $"A{Area}";
        else
            return $"A{Convert.ToString(Area).ToCharArray()[alt ? 1 : 0]}";
    }

    public bool HasAreaData() => Area > 0;

    public bool HasRoomData() => Room > 0;

    public override string ToString() => $"GridCell [is Floor = {IsFloor}, Area = {Area}, can be Door = {CanBeDoor}]";
}
