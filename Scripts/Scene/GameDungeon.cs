using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class GameDungeon : Node3D
{
	#region Exports
	[Export]
	[ExportGroup("General")]
	public uint SafetyUnitsAroundRoom = 50;

	[Export]
	[ExportGroup("Boss Room")]
	public bool GenerateBossRoom = true;

	[Export]
	[ExportGroup("Boss Room")]
	public Vector2I BossRoomSize = new Vector2I(100, 100);

	[Export]
	[ExportGroup("Boss Room")]
	public bool BossRoomExactSize = true;

	[Export]
	[ExportGroup("Boss Room")]
	public double BossRoomMinFilledPercent = 50.0;

	[Export]
	[ExportGroup("Spawn Room")]
	public bool GenerateSpawnRoom = true;

	[Export]
	[ExportGroup("Spawn Room")]
	public Vector2I SpawnRoomSize = new Vector2I(25, 25);

	[Export]
	[ExportGroup("Spawn Room")]
	public bool SpawnRoomExactSize = true;

	[Export]
	[ExportGroup("Spawn Room")]
	public double SpawnRoomMinFilledPercent = 25.0;

	[Export]
	[ExportGroup("Rooms")]
	public uint Rooms = 15;

	[Export]
	[ExportGroup("Rooms")]
	public Vector2I RoomSize = new Vector2I(40, 40);

	[Export]
	[ExportGroup("Rooms")]
	public double RoomMinFilledPercent = 25.0;

	[Export]
	[ExportGroup("Generator")]
	public Godot.Collections.Dictionary GeneratorLookup = new()
	{
		{ "Dirt", 0 },
		{ "Stone", 1 },
	};
	#endregion

	private GridMap gridMap;

	private List<PlacedDungeonRoom> placedDungeonRooms = new();

	private Player player;

	public override void _EnterTree()
	{
		gridMap = GetNode<GridMap>("%GridMap");
		player = GetNode<Player>("%Player");
	}

	public override void _Ready()
	{
		gridMap.Clear();

		var rooms = GenerateRooms();
		ProcessGeneratedRooms(rooms);
		RoomsToGodot();
		PlacePlayer();
	}

	private void PlacePlayer()
	{
		// Find spawn
		var spawnRoom = placedDungeonRooms.Find(x => x.Flag == "spawn");

		// TODO: Generate doors/entrances and use those instead
		// For now: Place in the center of the room and pray it's a floor? xD

		var centerX = spawnRoom.Location.X + spawnRoom.Width / 2;
		var centerY = spawnRoom.Location.Y + spawnRoom.Height / 2;

		var gridPosition = gridMap.MapToLocal(new Vector3I(centerX, 2, centerY));

		GD.Print($"Placing player at {centerX}-{centerY}; Grid: {gridPosition} spawn room: {spawnRoom.Location}");
		player.Position = gridPosition;
	}

	private void RoomsToGodot()
	{
		var index = 0;
		foreach (var room in placedDungeonRooms)
		{
			var startX = room.Location.X;
			var startY = room.Location.Y;

			for (var indexX = 0; indexX < room.Width; indexX++)
				for (var indexY = 0; indexY < room.Height; indexY++)
				{
					var gridX = startX + indexX;
					var gridY = startY + indexY;

					var cell = room.Grid[indexX, indexY];
					switch (cell)
					{
						case DungeonRoomGenerator.FLOOR:
							gridMap.SetCellItem(
								new Vector3I(gridX, 1, gridY),
								GeneratorLookup["Dirt"].As<int>()
							);
							break;
						case DungeonRoomGenerator.WALL:
							gridMap.SetCellItem(
								new Vector3I(gridX, 1, gridY),
								GeneratorLookup["Stone"].As<int>()
							);
							break;
					}
				}

			index++;
		}
	}

	private void ProcessGeneratedRooms(List<DungeonRoomGenerator> rooms)
	{
		placedDungeonRooms.Clear();

		var maxWidth = rooms.Max(room => room.GetWidth() + SafetyUnitsAroundRoom);
		var maxHeight = rooms.Max(room => room.GetHeight() + SafetyUnitsAroundRoom);

		// We need to find NxM grid to place all rooms on.
		// The issue is, that only rarely all rooms fit on a
		// uniform grid. Thus, we can adjust one axis to be longer
		// if needed to fit all rooms.
		// By taking the square root of the room count, we know
		// how many rows and columns are needed to fit all rooms.
		// If we then ceil (round up) one of them and floor
		// (round down) the other, we can fit all rooms on the grid
		// in a almost square shape.
		// ---
		// Example: 30 rooms to place 
		// sqrt(30) = 5.4
		// Rows = ceil(5.4) == 6
		// Columns = floor(5.4) == 5
		// NxM: 6x5 grid to place 30 rooms
		// 1 2 3 4 5
		// 2 x x x x
		// 3 x x x x
		// 4 x x x x
		// 5 x x x x
		// 6 x x x x
		var roomCount = rooms.Count();
		var roomCountSqrt = Math.Sqrt(roomCount);
		var rows = Math.Ceiling(roomCountSqrt);
		var columns = Math.Floor(roomCountSqrt);

		var index = 0;
		for (var row = 0; row < rows; row++)
			for (var column = 0; column < columns; column++)
			{
				if (index >= rooms.Count) break;

				var x = (int)(row * maxWidth + SafetyUnitsAroundRoom);
				var y = (int)(column * maxHeight + SafetyUnitsAroundRoom);
				var location = new Vector2I(x, y);

				var generatedRoom = rooms[index++];
				var placedRoom = new PlacedDungeonRoom(generatedRoom, location);
				placedDungeonRooms.Add(placedRoom);
			}
	}

	private List<DungeonRoomGenerator> GenerateRooms()
	{
		var rooms = new List<DungeonRoomGenerator>();

		if (GenerateBossRoom)
		{
			var bossRoom = new DungeonRoomGenerator(BossRoomSize.X, BossRoomSize.Y, "boss");
			bossRoom.DoWork(BossRoomMinFilledPercent, BossRoomExactSize, DungeonRoomType.Circular);
			rooms.Add(bossRoom);
		}

		if (GenerateSpawnRoom)
		{
			var spawnRoom = new DungeonRoomGenerator(SpawnRoomSize.X, SpawnRoomSize.Y, "spawn");
			spawnRoom.DoWork(SpawnRoomMinFilledPercent, SpawnRoomExactSize, DungeonRoomType.RandomPlaceSquare); // TODO: Square?
			rooms.Add(spawnRoom);
		}

		for (var i = 0; i < Rooms; i++)
		{
			var room = new DungeonRoomGenerator(RoomSize.X, RoomSize.Y);
			room.DoWork(RoomMinFilledPercent, false, DungeonRoomType.RandomPlaceSquare);
			rooms.Add(room);
		}

		return rooms;
	}
}
