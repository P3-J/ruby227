using Godot;
using System;

public partial class lasercontroller : Area3D
{
	[Export]
	CharacterBody3D player;

	[Signal]
	public delegate void HitPlayerEventHandler();

	private void _on_body_entered(Node3D body){
		GD.Print(body);
		if (body.Name == "player"){
			EmitSignal("HitPlayer");
		}
	}
}
