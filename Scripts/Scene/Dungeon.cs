using System;

public partial class Dungeon : Node2D
{
    [ExportCategory("Dungeon")]
    [Export]
    public Vector2I dungeonSize = new Vector2I(75, 75);

    [Export]
    public int seed = 0;

    [ExportCategory("Internals")]
    [Export]
    public bool printFinalResultToConsole = true;

    private DungeonGrid dungeonGrid;
    private GridGenerator gridGenerator;

    public override void _EnterTree()
    {
        dungeonGrid = GetNode<DungeonGrid>("%DungeonGrid");
    }

    public override void _Ready()
    {
        if (seed <= 0) seed = Random.Shared.Next();
        GD.Print($"Seed: {seed}");

        gridGenerator = new(
            ((uint)dungeonSize.X, (uint)dungeonSize.Y),
            seed
        );

        gridGenerator.Automate(printFinalResultToConsole: printFinalResultToConsole);
        dungeonGrid.FromGridGenerator(gridGenerator);
    }
}