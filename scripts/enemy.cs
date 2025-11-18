using Godot;
using Godot.Collections;
using Microsoft.VisualBasic;
using System;

public partial class enemy : CharacterBody3D
{


	[Export] private NavigationAgent3D navagent;
	[Export] public PackedScene Bullet;
	[Export] public int ShootDistance = 60;
	[Export] public int AggroDistance = 300;
	Timer timer;
	Timer retargetTimer;
	RayCast3D los;
	Timer deathTimer;
	GpuParticles3D deathExplosion;
	bool targetinlos;
	bool hasAggro;
	Node3D body;
	Vector3 next = Vector3.Zero;
	player Player;
	AudioStreamPlayer3D booster;
	AudioStreamPlayer3D rocket;
	NavigationRegion3D navregion;

	bool canMove = true;
	Vector3 velocity;
	public  float Speed;
	public const float Gravity = -9.8f;
	public const float jumpstr = 10f;


	enum EnemyStates { AFK = 0, HUNTING = 1, SHOOTING = 2 }
	private EnemyStates cState = EnemyStates.AFK;
	enum EnemyTypes { SHOOTER = 1, BOMBER = 2 }
	private EnemyTypes cType = EnemyTypes.SHOOTER;

	int HP = 3;
	int cHP = 3;
    bool target;
	bool Disabled = true;
	float lastSawPlayerSeconds;
	private bool canShoot = true;
	bool hasVisionOfTarget;
	public Vector3 spawnLocation;

    /// <summary>
    ///  TO FIX
    ///  los can target other enemies, this should not be a factor - ? i think fake news
    ///  look at, is looking at the final destination. not good +
    ///  randomize speed, instead of latency to introduce some randomness? 
    ///  plus minus bullet angle, so that it has the ability to be a tracing shot \\ would miss standing targets -- quite ok
    ///  death anim +
    ///  invisible barriers just for bots +
	/// 
	/// dodge bullets? jump 
	/// target lock on unlock
	/// wings//wol
    /// </summary>
    public override void _Ready()
    {

		navagent = GetNode<NavigationAgent3D>("NavigationAgent3D");
		body = GetNode<Node3D>("bodyController");
		timer = GetNode<Timer>("shotCooldown");
		los = GetNode<RayCast3D>("los");

		retargetTimer = GetNode<Timer>("retarget");
		booster = GetNode<AudioStreamPlayer3D>("booster");
		rocket  = GetNode<AudioStreamPlayer3D>("rocket");

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

		SetupProps();

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
        } else
        {
			GD.PushWarning("no player"); 
        }
	}



	public override void _PhysicsProcess(double delta)
	{
		if (Disabled) return;
		

		
		if (!canMove)
		{
			velocity = velocity.MoveToward(new Vector3(0, velocity.Y, 0), 10f * (float)delta);
		}
		if (!IsOnFloor())
		{
			velocity.Y += -1 * (float)delta;
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

    private void CheckAggroResetTime(float delta)
    {
        lastSawPlayerSeconds += delta;
        if (lastSawPlayerSeconds >= 20f)
		{
			GD.Print("We afk");
			cState = EnemyStates.AFK;
        }	
    }

    private void EnableHeadIndicator(){
		// if spotted , or in aggro range -> light up red dot on head to indicate target to player
		// either 3d model , or 3d sprite facing Player. 3d can emit
	}

	public float GetPlayerDistance(){
		Vector3 origin = los.GlobalPosition;
		Vector3 colPoint = los.GetCollisionPoint();
		return origin.DistanceTo(colPoint);
	}

	public void GetHit(int dmg){
		GD.Print("hit");
        cHP -= dmg;
		if (cHP <= 0)
			Die();
    }

	private void Die(){	
		Disabled = true;
		deathTimer.Start();
		deathExplosion.Emitting = true;
		
	}

	private void _on_deathtime_timeout(){
		QueueFree(); // do be sad
	}

	public void SetTargetPos(Vector3 pos)
	{
		var map = GetWorld3D().NavigationMap;
		var p = NavigationServer3D.MapGetClosestPoint(map, pos);
		navagent.TargetPosition = p;				
	}


	public void RotateBodyTowardsPlayer(bool lookAtPlayer, Vector3 lookPos){

		Vector3 lookDir;

		if (lookAtPlayer)
			lookDir = (Player.GlobalPosition - body.GlobalPosition).Normalized();
		else
			lookDir = (lookPos - body.GlobalPosition).Normalized();
	
		float currentYaw = body.RotationDegrees.Y;
        float targetYaw = Mathf.RadToDeg(Mathf.Atan2(lookDir.X, lookDir.Z));

        float delta = Mathf.Wrap(targetYaw - currentYaw, -180f, 180f);
        float finalYaw = currentYaw + delta;


        Tween rotationTween = GetTree().CreateTween();
        rotationTween.TweenProperty(body, "rotation_degrees:y", finalYaw, 0.1)
                     .SetTrans(Tween.TransitionType.Sine)
                     .SetEase(Tween.EaseType.InOut);
	}

	public bool CheckIfCanShoot(float distance)
	{
		if (distance < ShootDistance && canShoot)
		{
			return true;
		}
		return false;
	}
	
	public void TryToShoot(float PlayerDistance)
    {
		if (!canShoot) return;
		canShoot = false;

		SceneTreeTimer tr = GetTree().CreateTimer(1.5);
		tr.Timeout += () => ShootBullet();
    }	

	public void Jump()
	{
		if (IsOnFloor())
        {
			velocity.Y = 0;
            velocity.Y += jumpstr;
        }
	}

	public void ShootBullet()
	{
		if (Disabled) return;
		canShoot = true;
        bullet bulletInstance = Bullet.Instantiate() as bullet;
        bulletInstance.Position = GlobalPosition;

		bulletInstance.SetDirection((Player.GlobalPosition - GlobalTransform.Origin).Normalized() * Speed);
		bulletInstance.SetProps(1, "enemy");

        GetParent().AddChild(bulletInstance);
		rocket.Play();
		cState = EnemyStates.HUNTING;
		SetTargetPos(Player.GlobalPosition);


		GD.Randomize();
		//int randi = GD.RandRange(-15, 15);
		//int randi2 = GD.RandRange(-15, 15);

		//velocity.X += randi;
		//velocity.Z += randi2;

    }

	private void OnNavigationAgentVelocityComputed(Vector3 safevelo)
    {
        Velocity = safevelo;
    }

    private void OnNavigationAgentTargetReached()
	{
		if (cState == EnemyStates.AFK) return;
        SetTargetPos(Player.GlobalPosition);
		next = navagent.GetNextPathPosition();
    }
	#pragma warning disable IDE0060
    private void OnNavigationAgentLinkReached(Dictionary data)
    {
        Jump();
    } 


}
