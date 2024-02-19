using System;

public partial class Player : CharacterBody3D
{
	[Export]
	public float speedModifier = 10.0f;

	[Export]
	public float walkThreshold = 0.1f;

	[Export]
	public float rotationDurationInSeconds = 0.1f;

	private Direction direction = Direction.Down;
	private State state = State.Idle;

	private AnimatedSprite3D animatedSprite3D;

	public override void _Ready()
	{
		animatedSprite3D = GetNode<AnimatedSprite3D>("AnimatedSprite3D");
	}

	public override void _Process(double delta)
	{
		var inputVector = Input.GetVector("move_left", "move_right", "move_up", "move_down");
		var rotatedInputVector = inputVector.Rotated(-Rotation.Y);

		updateAnimation(inputVector);
		doMovement(rotatedInputVector);
		doRotation();
	}

	private void updateAnimation(Vector2 inputVector)
	{
		if (inputVector.X >= walkThreshold)
		{
			state = State.Walk;
			direction = Direction.Right;
		}
		else if (inputVector.X <= -walkThreshold)
		{
			state = State.Walk;
			direction = Direction.Left;
		}
		else if (inputVector.Y >= walkThreshold)
		{
			state = State.Walk;
			direction = Direction.Down;
		}
		else if (inputVector.Y <= -walkThreshold)
		{
			state = State.Walk;
			direction = Direction.Up;
		}
		else
		{
			state = State.Idle;
		}

		animatedSprite3D.Play($"{state.ToString().ToLower()}_{direction.ToString().ToLower()}");
	}

	private void doMovement(Vector2 inputVector)
	{
		Velocity = new Vector3(inputVector.X, 0, inputVector.Y) * speedModifier;
		MoveAndSlide();
	}

	private Tween rotationTween;

	private const float interCardinalAngle = 2f * (float)Math.PI / (360f / 45f);

	private void doRotation()
	{
		if (Input.IsActionJustPressed("rotate_left"))
		{
			if (rotationTween != null)
				rotationTween.Kill();
			rotationTween = GetTree().CreateTween();

			for (var i = 0; i < 45; i++)
				rotationTween.TweenCallback(
					Callable.From(() => RotateY(interCardinalAngle / 45))
				).SetDelay(rotationDurationInSeconds / 45);
		}
		else if (Input.IsActionJustPressed("rotate_right"))
		{
			if (rotationTween != null)
				rotationTween.Kill();
			rotationTween = GetTree().CreateTween();

			for (var i = 0; i < 45; i++)
				rotationTween.TweenCallback(
					Callable.From(() => RotateY(-interCardinalAngle / 45))
				).SetDelay(rotationDurationInSeconds / 45);
		}
	}

	private void onDungeonGridFinished()
	{
		placePlayerInRoom();
	}

	private void placePlayerInRoom()
	{
		var dungeonGrid = GetNode<DungeonGrid>("%DungeonGrid");

		while (true)
		{
			var chosenRoom = dungeonGrid.gridGenerator.R.Next(0, (int)dungeonGrid.gridGenerator.RoomCount);
			var roomCells = dungeonGrid.gridGenerator.FindCellOfRoom((uint)chosenRoom);

			// Check room cell count, if less or equal than zero -> repeat
			var cellCount = roomCells.Count() - 1;
			if (cellCount <= 0) continue;

			var chosenCellId = dungeonGrid.gridGenerator.R.Next(0, cellCount);
			var chosenCell = roomCells[chosenCellId];

			Position = new Vector3(
				chosenCell.Item1 * dungeonGrid.cellSizeOffset.X - 2,
				0,
				chosenCell.Item2 * dungeonGrid.cellSizeOffset.Y - 2
			);

			// Check for collision, if so: repeat
			if (MoveAndSlide()) continue;

			// End loop
			return;
		}
	}
}
