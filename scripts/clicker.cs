using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class clicker : Area2D
{
	List<CharacterBody2D> listy = new List<CharacterBody2D>();

	public void _on_body_entered(Node2D body){
		GD.Print(body.Name);
		listy.Add(body as CharacterBody2D);
	}

	public void _on_body_exited(Node2D body){
		CharacterBody2D realbody = body as CharacterBody2D;
		if (listy.Contains(realbody)){
			listy.Remove(realbody);
			GD.Print("removed", listy.Count);
		}
	}

	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("space")){
			if (listy.Count > 0){
				// select specific and kill order
				foreach (CharacterBody2D body in listy)
				{
					body.QueueFree();
				}
				listy = new List<CharacterBody2D>();
			}
		}
	}

}
