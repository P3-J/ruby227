using Godot;
using System;

public partial class world : Node3D
{
	[Export] CharacterBody3D bossman;
	[Export] PackedScene Ending;
	[Export] CharacterBody3D player;

	AnimationPlayer animsequencer;
	AnimationPlayer booster; 
	ProgressBar enemyHpBar;

    public override void _Ready()
    {
		enemyHpBar = GetNode<ProgressBar>("bossfightstuff/bosshud/bosshp");
		booster = GetNode<AnimationPlayer>("onetimethings/bossarea/AnimationPlayer");

		enemyHpBar.Visible = false;
		Callable bosshit= new Callable(this, nameof(UpdateBossUI));
		bossman.Connect("BossHit", bosshit);

		AudioServer.SetBusVolumeDb(0, 0f); // audio levels normalized, use diff bus for music
    }

	public void UpdateBossUI(int hp){
		// prob could do a simple move towards for anim.
		enemyHpBar.Value = hp;
		if (hp <= 0){
			GetTree().ChangeSceneToPacked(Ending);
		}
	}

	public void AwakeBoss(){
		// call this when boss at required location
		// starts boss controller
		bossman.Call("StartSequence");
		enemyHpBar.Visible = true;
	}


	public void _on_bossarea_body_entered(Node3D body){
		if (body == player){
			booster.Play("jump");
		}
	}

}
