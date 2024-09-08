using Godot;
using System;

public partial class startscript : Node3D
{
	[Export] PackedScene world;
	public void _on_button_pressed(){
		GetTree().ChangeSceneToPacked(world);
	}

	public void switchToGame(){
		GetTree().ChangeSceneToPacked(world);
	}
}
