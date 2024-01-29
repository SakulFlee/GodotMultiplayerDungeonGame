using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;

public partial class GameDungeon : Node3D
{
	#region Exports
	[Export]
	public Vector2I DungeonSize = new Vector2I(75, 75);

	[Export]
	public bool PrintResultToConsole = true;

	[Export]
	public Dictionary CellLookup = new()
	{
		{"Stone", 0},
		{"Dirt", 1},
	};
	#endregion

	private GridMap GridMap;

	private Player Player;

	private GridGenerator GridGenerator;

	public override void _EnterTree()
	{
		GridMap = GetNode<GridMap>("%GridMap");
		Player = GetNode<Player>("%Player");
	}

	public override void _Ready()
	{
		GridGenerator = new GridGenerator(((uint)DungeonSize.X, (uint)DungeonSize.Y));
		GridGenerator.Automate(printFinalResultToConsole: PrintResultToConsole);

		PlaceOnGridMap();

		PlacePlayer();
	}

	private void PlacePlayer()
	{
		// TODO
		// // Find spawn
		// var spawnRoom = placedDungeonRooms.Find(x => x.Flag == "spawn");

		// // TODO: Generate doors/entrances and use those instead
		// // For now: Place in the center of the room and pray it's a floor? xD

		// var centerX = spawnRoom.Location.X + spawnRoom.Width / 2;
		// var centerY = spawnRoom.Location.Y + spawnRoom.Height / 2;

		// var gridPosition = gridMap.MapToLocal(new Vector3I(centerX, 2, centerY));

		// GD.Print($"Placing player at {centerX}-{centerY}; Grid: {gridPosition} spawn room: {spawnRoom.Location}");
		// player.Position = gridPosition;
	}

	private void PlaceOnGridMap()
	{
		GridMap.Clear();

		for (var x = 0; x < DungeonSize.X; x++)
		{
			for (var y = 0; y < DungeonSize.Y; y++)
			{
				var cell = GridGenerator.Grid[x, y];

				GridMap.SetCellItem(new Vector3I(x, 0, y), (int)CellLookup["Dirt"]);

				if (!cell.IsFloor)
					GridMap.SetCellItem(new Vector3I(x, 1, y), (int)CellLookup["Stone"]);
			}
		}
	}
}
