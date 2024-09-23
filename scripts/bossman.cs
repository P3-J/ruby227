using Godot;
using System;

public partial class bossman : CharacterBody3D
{
	// this is not meant to be a direct controller
	// alternative simple state machine, commands from parent
	[Export] CharacterBody3D player;
	[Export] public PackedScene Bullet;

	[Signal]
	public delegate void BossHitEventHandler();
	const int MAXHP = 10;
	int HP = 10;

	Timer shottimer;
	Marker3D shotspot;
	AnimationPlayer bossanim;
	public override void _Ready()
	{
		shotspot = GetNode<Marker3D>("shotspot");
	}

	public void ShootBullet(int dmg)
    {
        CharacterBody3D bulletInstance = Bullet.Instantiate() as CharacterBody3D;
        bulletInstance.Position = GlobalPosition;
        bulletInstance.Call("SetDirection", (shotspot.GlobalPosition - GlobalTransform.Origin).Normalized() * 10);
		bulletInstance.Call("SetOwner", "enemy");
		bulletInstance.Call("SetDamage", dmg);
        GetParent().AddChild(bulletInstance);
    }



	public void GetHit(){
		GD.Print("got hit");
        HP -= 1;
        EmitSignal("BossHit", HP);
		CheckForStageChange();
    }



	private void CheckForStageChange(){
		// check for boss battle stage
		switch (HP){
			case 3:
				Stage3();
				break;
			case 6:
				Stage2();
				break;
			case 8:
				Stage1();
				break;
		}
	}

	private void Stage1(){

	}

	private void Stage2(){
		
	}

	private void Stage3(){
		
	}

}
