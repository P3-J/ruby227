using Godot;
using System;

public partial class arrowScript : CharacterBody2D
{

	public const float Speed = 500.0f;
	[Export]
	string direction;
	Sprite2D arrowSprite;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		arrowSprite = GetNode<Sprite2D>("inputSprite");
		SetDirection();
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 Pos = GlobalPosition;
		Pos.Y += 6;
		GlobalPosition = Pos;
	}
	public void SetDirection()
	{
		switch (direction)
		{
			case "right":
				arrowSprite.RotationDegrees = 180;
				break;
			case "left":
				break;
			case "up":
				arrowSprite.RotationDegrees = 90;
				break;
			case "down":
				arrowSprite.RotationDegrees = -90;
				break;
		}
	}
}
