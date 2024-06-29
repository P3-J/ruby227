using Godot;
using System;

public partial class arrow : CharacterBody2D
{
	public const float Speed = 300.0f;

	public override void _PhysicsProcess(double delta)
	{
		Vector2 velocity = Velocity;
		Vector2 Lefty = Vector2.Left;

		velocity.X =  Lefty.X * Speed;
		
		Velocity = velocity;
		MoveAndSlide();
	}
}
