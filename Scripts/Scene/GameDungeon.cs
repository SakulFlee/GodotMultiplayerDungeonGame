using System;
using Godot;
using Godot.Collections;

public partial class GameDungeon : Node3D
{
	#region Exports
	[ExportGroup("Dungeon")]
	[Export]
	public Vector2I DungeonSize = new Vector2I(75, 75);

	[Export]
	public int Seed = 0;

	[Export]
	public bool RandomizeSeedOnStart = true;

	[ExportGroup("Internals")]
	[Export]
	public bool PrintResultToConsole = true;

	[Export]
	public GridTheme GridTheme;

	[Export]
	public Dictionary CellLookup = new()
	{
		{"Stone", 0},
		{"Dirt", 1},
	};
	#endregion

	private Node3D Cells;

	private Player Player;

	private GridGenerator GridGenerator;

	public override void _EnterTree()
	{
		Cells = GetNode<Node3D>("%Cells");
		Player = GetNode<Player>("%Player");
	}

	public override void _Ready()
	{
		if (RandomizeSeedOnStart) Seed = Random.Shared.Next(int.MaxValue);
		GD.Print($"Seed: {Seed}");

		GridGenerator = new GridGenerator(((uint)DungeonSize.X, (uint)DungeonSize.Y), seed: Seed);
		GridGenerator.Automate(printFinalResultToConsole: PrintResultToConsole);

		PlaceDungeon();

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

	private void PlaceDungeon()
	{
		// Clear children if any persists
		foreach(var child in Cells.GetChildren())
			Cells.RemoveChild(child);

		GridTranslator.TranslateAndPlace(Cells, GridGenerator, GridTheme);
	}
}
