using Godot;
using System;

public partial class input_field : Area2D
{
	
	[Export]
	string direction;
	Sprite2D arrowSprite;
	CharacterBody2D currentKey;

	public override void _Ready()
	{
		arrowSprite = GetNode<Sprite2D>("inputSprite");
		SetDirection();
	}

	
	public override void _Process(double delta)
	{

		if (Input.IsActionJustPressed("up") && direction == "up"){
			ClearKey();
		}
		if (Input.IsActionJustPressed("left") && direction == "left"){
			ClearKey();
		}
		if (Input.IsActionJustPressed("right") && direction == "right"){
			ClearKey();
		}

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

	public void ClearKey()
	{
		if (currentKey != null){
			currentKey.QueueFree();
			currentKey = null;
		} 
	}

	public void _on_body_entered(Node2D body)
	{
		if (body is CharacterBody2D){
			currentKey = body as CharacterBody2D;
		}
	}

	public void _on_body_exited(Node2D body)
	{
		currentKey = null;
	}

}
