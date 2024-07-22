using Godot;
using System;

public partial class world : Node3D
{

	[Export] Node3D PlayerCamBase;
	[Export] AnimatableBody3D bossman;
	[Export] PackedScene Ending;
	[Export] AudioStreamPlayer bossmusic;

	AnimationPlayer animsequencer;
	ProgressBar enemyHpBar;


    public override void _Ready()
    {
        animsequencer = GetNode<AnimationPlayer>("bossfightstuff/AnimationPlayer");
		enemyHpBar = GetNode<ProgressBar>("bossfightstuff/bosshud/bosshp");

		enemyHpBar.Visible = false;
		Callable bosshit= new Callable(this, nameof(UpdateBossUI));
		bossman.Connect("BossHit", bosshit);

    }

    public override void _PhysicsProcess(double delta)
    {

		// lmao wtf
         if (Input.IsActionPressed("camleft")){
			Vector3 v = PlayerCamBase.RotationDegrees;
			v.Y += 3f;
			PlayerCamBase.RotationDegrees = v;
		}

		 if (Input.IsActionPressed("camright")){
			Vector3 v = PlayerCamBase.RotationDegrees;
			v.Y -= 3f;
			PlayerCamBase.RotationDegrees = v;
		}
    }

	public void UpdateBossUI(int hp){
		// prob could do a simple move towards for anim.
		GD.Print("updated");
		enemyHpBar.Value = hp;
		if (hp <= 0){
			GetTree().ChangeSceneToPacked(Ending);
		}
	}


	public void _on_bossfighttrigger_body_entered(Node3D body){
		if (body is CharacterBody3D && body.Name == "player"){
			animsequencer.Play("entry");
		}
	}

	public void AwakeBoss(){
		// call this when boss at required location
		// starts boss controller
		bossman.Call("StartSequence");
		bossmusic.Play();
		enemyHpBar.Visible = true;
	}

}
