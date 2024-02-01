using System.Collections.Generic;
using Godot;

public class GridTranslator
{
	public static void TranslateAndPlace(Node3D parent, GridGenerator gridGenerator, GridTheme gridTheme)
	{
		var translationOutput = Translate(gridGenerator, gridTheme);

		foreach (var (position, cell) in translationOutput)
		{
			var cellInstance = cell.Instantiate<Node3D>();
			cellInstance.Position = position;
			parent.AddChild(cellInstance);
		}
	}

	public static Dictionary<Vector3, PackedScene> Translate(GridGenerator gridGenerator, GridTheme gridTheme)
	{
		var output = new Dictionary<Vector3, PackedScene>();

		for (uint x = 0; x < gridGenerator.SizeX; x++)
			for (uint y = 0; y < gridGenerator.SizeY; y++)
			{
				var position = new Vector3(x, 0, y);

				var cell = gridGenerator.GetCell((x, y));
				if (cell.IsFloor)
					output.Add(position, gridTheme.FloorTile);
				else
				{
					var cellN = gridGenerator.GetCell((x - 1, y));
					var cellS = gridGenerator.GetCell((x + 1, y));
					var cellE = gridGenerator.GetCell((x, y - 1));
					var cellW = gridGenerator.GetCell((x, y + 1));

					var wallNeighbourCount =
						  (cellN == null || !cellN.IsFloor ? 1 : 0)
						+ (cellS == null || !cellS.IsFloor ? 1 : 0)
						+ (cellE == null || !cellE.IsFloor ? 1 : 0)
						+ (cellW == null || !cellW.IsFloor ? 1 : 0);

					if (wallNeighbourCount == 4)
						output.Add(position, gridTheme.WallTile);
					else if (wallNeighbourCount == 2)
						output.Add(position, gridTheme.WallCornerTile);
					else
						output.Add(position, gridTheme.WallEdgeTile);
				}
			}

		return output;
	}

	public static Vector3 GridPositionToGodotPosition((uint, uint) gridPosition) => new Vector3(gridPosition.Item1, 0, gridPosition.Item2);

	public static (uint, uint) GodotPositionToGridPosition(Vector3 godotPosition) => ((uint)godotPosition.X, (uint)godotPosition.Z);
}
