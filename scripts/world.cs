using Godot;
using System;

public partial class world : Node3D
{
	[Export] CharacterBody3D bossman;
	[Export] PackedScene Ending;
	[Export] CharacterBody3D Player;

	AnimationPlayer animsequencer;
	AnimationPlayer booster; 
	ProgressBar enemyHpBar;
	AnimationPlayer gatecontroller;
	AnimationPlayer Lazoranim;


	Area3D Laser1;
	Area3D Laser2;
	Area3D Laser3;
	Area3D Laser4;


    public override void _Ready()
    {
		enemyHpBar = GetNode<ProgressBar>("bossfightstuff/bosshud/bosshp");
		booster = GetNode<AnimationPlayer>("onetimethings/bossarea/AnimationPlayer");
		gatecontroller = GetNode<AnimationPlayer>("bossfightstuff/gates/gatecontroller");

		Laser1 = GetNode<Area3D>("bossfightstuff/laser");
		Laser2 = GetNode<Area3D>("bossfightstuff/laser2");
		Laser3 = GetNode<Area3D>("bossfightstuff/laser3");
		Laser4 = GetNode<Area3D>("bossfightstuff/laser4");
		Lazoranim = GetNode<AnimationPlayer>("bossfightstuff/lazor");

		StartUp();
    }

	private void StartUp(){
		Callable bosshit= new Callable(this, nameof(UpdateBossUI));
		bossman.Connect("BossHit", bosshit);

		Callable gateopen = new Callable(this, nameof(OpenGates));
		bossman.Connect("OpenGates", gateopen);

		Callable LaserAnim = new Callable(this, nameof(LaserPlay));
		bossman.Connect("LazorPlay", LaserAnim);

		Callable damagePlayer = new Callable(this, nameof(DamagePlayer));
		Area3D[] lazors = new Area3D[] {Laser1, Laser2, Laser3, Laser4};
		foreach (Area3D laser in lazors)
		{
			laser.Connect("HitPlayer", damagePlayer);
		}

		enemyHpBar.Visible = false;
		AudioServer.SetBusVolumeDb(0, 0f);
	}

	public void StartBossFight(){
		enemyHpBar.Visible = true;
		bossman.Call("StartCombat");
	}

	public void UpdateBossUI(int hp){
		// prob could do a simple move towards for anim.
		enemyHpBar.Value = hp;
		if (hp <= 0){
			GetTree().ChangeSceneToPacked(Ending);
		}
	}

	public void LaserPlay(){
		Lazoranim.Play("firelazor");
	}

	public void DamagePlayer(){
		Player.Call("GetHit", 1);
	}

	public void OpenGates(){
		gatecontroller.Play("openGate");
	}

	public void _on_bossarea_body_entered(Node3D body){
		if (body == Player){
			booster.Play("jump");
		}
	}

}
