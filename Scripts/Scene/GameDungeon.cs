using System;
using System.Runtime.CompilerServices;
using Godot;
using Godot.Collections;

public partial class GameDungeon : Node3D
{
	#region Exports
	[ExportCategory("Dungeon")]
	[Export]
	public Vector2I DungeonSize = new Vector2I(75, 75);

	[Export]
	public int Seed = 0;

	[Export]
	public bool RandomizeSeedOnStart = true;

	[ExportCategory("Internals")]
	[Export]
	public bool PrintResultToConsole = true;

	[Export]
	public GridTheme GridTheme;

	[Export]
	public Vector2 CellSize = new Vector2(2, 2);

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

	public override void _Process(double delta)
	{
		GD.Print($"FPS: {Engine.GetFramesPerSecond()} ({Performance.GetMonitor(Performance.Monitor.TimeFps)}ms) - Draw Calls: {Performance.GetMonitor(Performance.Monitor.RenderTotalDrawCallsInFrame)} - Primitives: {Performance.GetMonitor(Performance.Monitor.RenderTotalPrimitivesInFrame)}");
	}

	private void PlacePlayer()
	{
		var roomId = GridGenerator.R.Next(0, (int)GridGenerator.RoomCount - 1);

		var roomCells = GridGenerator.FindCell((x, y, cell) => cell.Room == roomId);

		var cellId = GridGenerator.R.Next(0, roomCells.Count - 1);
		var cell = roomCells[cellId];

		var playerPosition = new Vector3(
			cell.Item1 * CellSize.X,
			1,
			cell.Item2 * CellSize.Y
		);

		Player.Position = playerPosition;
		Player.Rotation = Vector3.Zero;
	}

	private void PlaceDungeon()
	{
		// Clear children if any persists
		foreach (var child in Cells.GetChildren())
			Cells.RemoveChild(child);

		GridTranslator.TranslateAndPlace(Cells, GridGenerator, GridTheme, CellSize);
	}
}
