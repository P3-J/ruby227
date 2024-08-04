using Godot;
using System;

public partial class bossman : AnimatableBody3D
{
	// this is not meant to be a direct controller
	// alternative simple state machine, commands from parent
	[Export] CharacterBody3D player;
	[Export] public PackedScene Bullet;
	[Signal]
	public delegate void BossHitEventHandler();
	const int HP = 10;
	int cHP = 10;

	Timer shottimer;
	Marker3D shotspot;
	AnimationPlayer bossanim;
	public override void _Ready()
	{
		shottimer = GetNode<Timer>("shottimer");
		shotspot = GetNode<Marker3D>("shotspot");
		bossanim = GetNode<AnimationPlayer>("bossanimmanager");
	}
	public void StartSequence(){
		shottimer.Start();
	}

	public void ShootBullet()
    {
        CharacterBody3D bulletInstance = Bullet.Instantiate() as CharacterBody3D;
        bulletInstance.Position = GlobalPosition;
        bulletInstance.Call("SetDirection", (shotspot.GlobalPosition - GlobalTransform.Origin).Normalized() * 10);
		bulletInstance.Call("SetOwner", "enemy");
        GetParent().AddChild(bulletInstance);
    }

	public void GetHit(){
		GD.Print("got hit");
        cHP -= 1;
        EmitSignal("BossHit", cHP);
    }


	public void _on_shottimer_timeout(){
		shottimer.Start();
		SpinAttack();
	}

	public void SpinAttack(){
		bossanim.Play("spinattack");
	}

}
