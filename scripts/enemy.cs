using Godot;
using Godot.Collections;
using Microsoft.VisualBasic;
using System;
using System.Threading.Tasks;

public partial class enemy : CharacterBody3D
{


	[Export] private NavigationAgent3D navagent;
	[Export] public PackedScene Bullet;
	[Export] CpuParticles3D shotparticle;
	[Export] Selfdestruct selfD;
	[Export] Node3D legs;
	[Export] Marker3D gunshotspot1;
	[Export] Marker3D gunshotspot2;
	Timer retargetTimer;
	RayCast3D los;
	Timer deathTimer;
	GpuParticles3D deathExplosion;
	Node3D body;
	Vector3 next = Vector3.Zero;
	Vector3 last;
	player Player;
	AudioStreamPlayer3D booster;
	AudioStreamPlayer3D rocket;
	NavigationRegion3D navregion;
	Vector3 velocity;
	public Vector3 spawnLocation;

	enum EnemyStates { AFK = 0, HUNTING = 1, SHOOTING = 2, PATROL = 3, COOLDOWN = 4 }
	private EnemyStates cState = EnemyStates.AFK;
	enum EnemyTypes { SHOOTER = 1, BOMBER = 2 }
	private EnemyTypes cType = EnemyTypes.SHOOTER;

	bool canMove = true;
	public float Speed;
	public const float Gravity = -9.8f;
	public const float jumpstr = 10f;
	public int ShootDistance = 200;
	public int AggroDistance = 400;
	int HP = 5;
	int cHP = 5;
	bool target;
	bool Disabled = true;
	float lastSawPlayerSeconds;
	private bool canShoot = true;
	bool hasVisionOfTarget;
	bool targetinlos;
	bool hasAggro;

	public override void _Ready()
	{

		navagent = GetNode<NavigationAgent3D>("NavigationAgent3D");
		body = GetNode<Node3D>("bodyController/body");
		los = GetNode<RayCast3D>("los");

		retargetTimer = GetNode<Timer>("timers/retarget");
		booster = GetNode<AudioStreamPlayer3D>("booster");
		rocket = GetNode<AudioStreamPlayer3D>("rocket");

		navagent.Connect("target_reached", new Callable(this, nameof(OnNavigationAgentTargetReached)));
		navagent.Connect("velocity_computed", new Callable(this, nameof(OnNavigationAgentVelocityComputed)));
		navagent.Connect("link_reached", new Callable(this, nameof(OnNavigationAgentLinkReached)));

		deathTimer = GetNode<Timer>("death/deathtime");
		deathExplosion = GetNode<GpuParticles3D>("death/explosion");

		GlobalPosition = spawnLocation;
		navregion = GetParent<NavigationRegion3D>();

		GD.Randomize();
		//int randi = GD.RandRange(1, 2);
		//	cType = randi == 1 ? EnemyTypes.SHOOTER : EnemyTypes.BOMBER;


		SceneTreeTimer tr = GetTree().CreateTimer(1.0);
		tr.Timeout += OnTimeOut;


	}
	private void OnTimeOut()
	{
		target = true;
		Node potPlayer = GetTree().CurrentScene.FindChild("player");

		if (potPlayer.IsInGroup("player"))
		{
			Disabled = false;
			Player = (player)potPlayer;
			SetTargetPos(Player.GlobalPosition);
		}
		else
		{
			GD.PushWarning("no player");
		}

		SetupProps();
	}



	public override void _PhysicsProcess(double delta)
	{
		if (Disabled) return;

		/* if (!canMove)
		{
			velocity = velocity.MoveToward(new Vector3(0, velocity.Y, 0), 1f * (float)delta);
		} */
		if (!IsOnFloor() && velocity.Y > -10)
		{
			velocity.Y += -10 * (float)delta;
		}

		navagent.Velocity = velocity;
		MoveAndSlide();
		velocity = Velocity;
		//GD.Print(Velocity, groundcheck.IsColliding(), IsOnFloor(), canMove);
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
		//GD.Print(cState);
		if (Disabled) return;
		LosCollsionChecks();
		StateMachine(delta);

	}

	public Vector3 GetVelo()
	{
		return Velocity;
	}

	private void CheckAggroResetTime(float delta)
	{
		lastSawPlayerSeconds += delta;
		if (lastSawPlayerSeconds >= 20000f)
		{
			GD.Print("We afk");
			cState = EnemyStates.AFK;
		}
	}

	private void EnableHeadIndicator()
	{
		// if spotted , or in aggro range -> light up red dot on head to indicate target to player
		// either 3d model , or 3d sprite facing Player. 3d can emit
	}

	public float GetPlayerDistance()
	{
		Vector3 origin = los.GlobalPosition;
		Vector3 colPoint = los.GetCollisionPoint();
		return origin.DistanceTo(colPoint);
	}

	public void GetHit(int dmg)
	{
		GD.Print("hit");
		cHP -= dmg;
		if (cHP <= 0)
			Die();
	}

	private void Die()
	{
		if (Disabled) return;
		Disabled = true;
		deathTimer.Start();
		selfD.Explode(3, this.Name);
		deathExplosion.Emitting = true;
	}

	private void _on_deathtime_timeout()
	{
		QueueFree(); // do be sad
	}

	public void SetTargetPos(Vector3 pos)
	{
		var map = GetWorld3D().NavigationMap;
		var p = NavigationServer3D.MapGetClosestPoint(map, pos);
		navagent.TargetPosition = p;
	}


	public void RotateBodyTowards(Vector3 lookPos, string part, double delta)
	{
		Node3D cBody = part == "legs" ? legs : body;

		var dir = lookPos - cBody.GlobalPosition;
		dir.Y = 0;
		if (dir.LengthSquared() < 0.0001f) return;

		dir = dir.Normalized();
		float targetYaw = Mathf.Atan2(dir.X, dir.Z);
		float currentYaw = Mathf.DegToRad(cBody.RotationDegrees.Y);
		float newYaw = Mathf.LerpAngle(currentYaw, targetYaw, (float)delta * 5f);

		cBody.RotationDegrees = new Vector3(
			cBody.RotationDegrees.X,
			Mathf.RadToDeg(newYaw),
			cBody.RotationDegrees.Z
		);
	}

	public bool CheckIfCanShoot(float distance)
	{
		if (distance < ShootDistance && canShoot)
		{
			return true;
		}
		return false;
	}

	public void TryToShoot()
	{
		if (!canShoot) return;
		canShoot = false;

		GD.Randomize();
		ShootBullet("left");
		ShootBullet("right");
		SceneTreeTimer tr = GetTree().CreateTimer(2);
		tr.Timeout += stateSwap;

	}

	private void stateSwap()
	{
		canShoot = true;
		cState = EnemyStates.HUNTING;
	}

	public void Jump()
	{
		if (IsOnFloor())
		{
			velocity.Y = 0;
			velocity.Y += jumpstr;
		}
	}

	public void ShootBullet(String hand)
	{
		if (Disabled) return;
		shotparticle.Emitting = true;

		Marker3D handSpot = hand == "left" ? gunshotspot1 : gunshotspot2;

		bullet bulletInstance = Bullet.Instantiate() as bullet;
		// mybe shootbug?


		bulletInstance.SetDirection((Player.GlobalPosition - GlobalTransform.Origin).Normalized() * Speed);
		bulletInstance.SetProps(1, "enemy", Player.Velocity * 4, 35, true, bullet.BulletType.EXPLODING);

		GetParent().AddChild(bulletInstance);
		bulletInstance.GlobalPosition = handSpot.GlobalPosition;
		rocket.Play();

	}

	private void OnNavigationAgentVelocityComputed(Vector3 safevelo)
	{
		Velocity = safevelo;
	}


#pragma warning disable IDE0060
	private void OnNavigationAgentLinkReached(Dictionary data)
	{
		Jump();
	}


}
