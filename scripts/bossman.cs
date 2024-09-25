
using Godot;
public partial class bossman : CharacterBody3D
{
	[Export] CharacterBody3D player;
	[Export] public PackedScene Bullet;

	[Signal]
	public delegate void BossHitEventHandler();


	bool canMove = true;
	bool canShoot = true;
	bool disabled = false;
	const int MAXHP = 10;
	int HP = 10;

	Timer shottimer;
	Marker3D missileSpot1;
	Marker3D missileSpot2;
	Marker3D missileSpot3;
	Marker3D missileSpot4;

	Marker3D[] missileSpots;

	public override void _Ready(){
		shottimer = GetNode<Timer>("utils/shoottimer");
		missileSpot1 = GetNode<Marker3D>("body/root/missilelauncherswivel/missilelauncher/missilelauncherspot");
		missileSpot2 = GetNode<Marker3D>("body/root/missilelauncherswivel2/missilelauncher/missilelauncherspot");
		missileSpot3 = GetNode<Marker3D>("body/root/missilelauncherswivel3/missilelauncher/missilelauncherspot");
		missileSpot4 = GetNode<Marker3D>("body/root/missilelauncherswivel4/missilelauncher/missilelauncherspot");
		missileSpots = new Marker3D[] {missileSpot1, missileSpot2, missileSpot3, missileSpot4};
		StartCombat();
	}

	public override void _PhysicsProcess(double delta){
		if (disabled){return;}

		LookAt(player.GlobalPosition);
	}

	private void StartCombat(){
		shottimer.Start();
	}

	private void ShootBullet(int dmg, Marker3D spot, Vector3 offset){
		bullet bulletInstance = Bullet.Instantiate() as bullet;
		bulletInstance.Position = spot.GlobalPosition;
		Vector3 playerPos = player.GlobalPosition;
		playerPos += offset;
		
		bulletInstance.SetDirection((playerPos - spot.GlobalPosition).Normalized());
		bulletInstance.SetOwner("enemy");
		bulletInstance.SetDamage(dmg);

		GetParent().AddChild(bulletInstance);
	}


	private void ShootFromAllCannons(){
		if (!canShoot){return;}
		int cannon = 0;
		Vector3 offset = new (0, 0, 0);
		foreach (Marker3D missileSpot in missileSpots)
		{

			switch (cannon){
				case 0:
					offset.Y = 1;
					offset.Z = 1;
					break;
				case 1:
					offset.Y -= 1;
					offset.Z -= 1;
					break;
				case 2:
					offset.Y -= 6;
					offset.Z -= 6;
					break;
				case 3:
					offset.Y = 6;
					offset.Z = 6;
					break;
			}

			ShootBullet(1, missileSpot, offset);
			cannon += 1;
		}
	}

	public void GetHit(int dmg){
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

	private void _on_shoottimer_timeout(){
		ShootFromAllCannons();
	}


}
