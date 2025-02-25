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

	private void onDungeonGridFinished(int _)
	{
		placePlayerInRoom();
	}

	private void placePlayerInRoom()
	{
		var dungeonGrid = GetNode<DungeonGrid>("%DungeonGrid");

		Position = dungeonGrid.GridToLocal(new Vector3I(
			dungeonGrid.gridGenerator.portalLocation.X * dungeonGrid.cellTranslationRatio.X,
			0,
			dungeonGrid.gridGenerator.portalLocation.Y * dungeonGrid.cellTranslationRatio.Y
		));
	}
}
