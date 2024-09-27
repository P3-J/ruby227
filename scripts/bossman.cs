
using Godot;
public partial class bossman : CharacterBody3D
{
	[Export] CharacterBody3D player;
	[Export] public PackedScene Bullet;
	[Export] public PackedScene Enemy;

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

		LookAtPos(player.GlobalPosition);
	}

	private void StartCombat(){
		shottimer.Start();
	}

	private void LookAtPos(Vector3 pos){

		Vector3 dire = GlobalPosition - pos;

		float angleRadians = Mathf.Atan2(dire.X, dire.Z);
		float angleDegrees = Mathf.RadToDeg(angleRadians);
		RotationDegrees = new Vector3(0, angleDegrees, 0);
		GD.Print(RotationDegrees, pos);

	}

	private void ShootBullet(int dmg, Marker3D spot, Vector3 offset){
		bullet bulletInstance = Bullet.Instantiate() as bullet;
		bulletInstance.Position = spot.GlobalPosition;
		Vector3 playerPos = player.GlobalPosition;
		playerPos += offset;
		
		bulletInstance.RemoveRayCastMask(2, false);
		bulletInstance.SetDirection((playerPos - spot.GlobalPosition).Normalized());
		bulletInstance.SetOwner("enemy");
		bulletInstance.SetDamage(dmg);
		bulletInstance.extraSpeed = 10;

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
					offset.X = 0;
					break;
				case 1:
					offset.X = -5;
					break;
				case 2:
					offset.X = -10;
					break;
				case 3:
					offset.X = 10;
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
		// repeat between like 20 sek
		SceneTreeTimer tr = GetTree().CreateTimer(20.0);
		tr.Timeout += Stage1;
		// lasers 
	}

	private void Stage2(){
		// spawn enemies 
		Vector3 spawnPoint1 = Vector3.Zero; 
		Vector3 spawnPoint2 = Vector3.Zero;

		Vector3[] spawns = new Vector3[]{spawnPoint1, spawnPoint2};

		
		for (int i = 0; i < 2; i++)
		{
			enemy enemyInstance = Enemy.Instantiate() as enemy;
			enemyInstance.GlobalPosition = spawns[i];
			
		}


	}

	private void Stage3(){
		
	}

	private void _on_shoottimer_timeout(){
		ShootFromAllCannons();
	}


}
