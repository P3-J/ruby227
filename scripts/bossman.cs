
using Godot;
public partial class bossman : CharacterBody3D
{
	[Export] CharacterBody3D player;
	[Export] public PackedScene Bullet;
	[Export] public PackedScene Enemy;

	[Signal]
	public delegate void BossHitEventHandler();

	[Signal]
	public delegate void OpenGatesEventHandler();

	[Signal]
	public delegate void LazorPlayEventHandler();


	bool canMove = true;
	bool canShoot = true;
	bool disabled = false;
	const int MAXHP = 10;
	int HP = 10;
	int cStage = 0;

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
	}

	public override void _PhysicsProcess(double delta){
		if (disabled){return;}

		LookAtPos(player.GlobalPosition);
	}

	public void StartCombat(){
		shottimer.Start();
	}

	private void LookAtPos(Vector3 pos){

		Vector3 dire = GlobalPosition - pos;

		float angleRadians = Mathf.Atan2(dire.X, dire.Z);
		float angleDegrees = Mathf.RadToDeg(angleRadians);
		RotationDegrees = new Vector3(0, angleDegrees, 0);

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
		
		if (HP <= 8 && cStage == 0){
			cStage += 1;
			Stage1();
			return;
		}
		if (HP <= 5 && cStage == 1){
			cStage += 1;
			Stage2();
			return;
		}
		if (HP <= 3 && cStage == 2){
			cStage += 1;
			Stage3();
			return;
		}
	}

	private void Stage1(){
		EmitSignal(nameof(LazorPlay));
		SceneTreeTimer tr = GetTree().CreateTimer(25.0);
		tr.Timeout += Stage1;
	}

	private void Stage2(){
		// spawns enemies
		EmitSignal(nameof(OpenGates));
	}

	private void Stage3(){
		
	}

	private void _on_shoottimer_timeout(){
		ShootFromAllCannons();
	}


}
