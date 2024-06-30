using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

public partial class gamemanager : Node2D
{
	List<Area2D> Clickers = new List<Area2D>();
	Label ComboLabel;
	int Combo = 0;
	public override void _Ready()
	{
		Clickers.Add(GetNode<Area2D>("clickerController/inputField2"));
		Clickers.Add(GetNode<Area2D>("clickerController/inputField3"));
		Clickers.Add(GetNode<Area2D>("clickerController/inputField"));

		ComboLabel = GetNode<Label>("hud/combo");

		Callable hitter = new(this, nameof(Hit));
		Callable misser = new(this, nameof(Miss));
		foreach (Area2D clicker in Clickers)
		{
			clicker.Connect("Hit", hitter);
			clicker.Connect("Miss", misser);
		}
		RefreshHud();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}


	public void RefreshHud()
	{
		ComboLabel.Text = Combo.ToString() + "X";
	}
	public void Hit(){
		Combo += 1;
		RefreshHud();
	}
	
	public void Miss(){
		Combo = 0;
		RefreshHud();
	}
}
