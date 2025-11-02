using Godot;
using Godot.Collections;
using System;

public partial class enemy : CharacterBody3D
{
	public const float Speed = 15f;

	[Export] private NavigationAgent3D navagent;
	[Export] CharacterBody3D player; // bad but gets the job done
	Node3D body;
	[Export] public PackedScene Bullet;
	[Export] float ShootDistance = 60f;
	[Export] float AggroDistance = 90f;
	Timer timer;
	Timer retargetTimer;
	RayCast3D los;
	Timer deathTimer;
	GpuParticles3D deathExplosion;
	bool targetinlos;
	bool hasAggro;

	AudioStreamPlayer3D booster;
	AudioStreamPlayer3D rocket;

	Boolean canMove = true;
	Vector3 velocity;
	public const float Gravity = -9.8f;
	public const float jumpstr = 10f;
	Vector3 next = Vector3.Zero;

	enum EnemyStates { AFK = 0, HUNTING = 1, SHOOTING = 2 }
	private EnemyStates cState = EnemyStates.AFK;

	int HP = 1;
	int cHP = 1;
    bool target;
	bool Disabled = false;
	float lastSawPlayerSeconds;
    private bool canShoot = true;

    /// <summary>
    ///  TO FIX
    ///  los can target other enemies, this should not be a factor - ? i think fake news
    ///  look at, is looking at the final destination. not good +
    ///  randomize speed, instead of latency to introduce some randomness? 
    ///  plus minus bullet angle, so that it has the ability to be a tracing shot \\ would miss standing targets -- quite ok
    ///  death anim +
    ///  invisible barriers just for bots +
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
		

		SceneTreeTimer tr = GetTree().CreateTimer(1.0);
		tr.Timeout += OnTimeOut;

       
    }

	private void OnTimeOut()
	{
		target = true;
		if (player != null)
        {
			SetTargetPos(player.GlobalPosition);
        } else
        {
			GD.PushWarning("no player"); 
        }
	}

	

    public override void _PhysicsProcess(double delta)
	{
		if (Disabled) return;

		if (target) los.LookAt(player.GlobalPosition, Vector3.Up);
		float PlayerDistance = 999; // default = out of range
	
		Node Collider = null;
		if (los.IsColliding()) Collider = (Node)los.GetCollider();
		if (Collider is player)
		{
			PlayerDistance = GetPlayerDistance();
			lastSawPlayerSeconds = 0;
		}

		hasAggro = PlayerDistance < AggroDistance;
		if (hasAggro && cState != EnemyStates.SHOOTING) cState = EnemyStates.HUNTING;
		velocity = Velocity;

		bool ShouldStop = false;

		switch (cState)
        {
            case EnemyStates.SHOOTING:
				ShouldStop = true;
				RotateBodyTowardsPlayer(true, Vector3.Zero);
				if (hasAggro) TryToShoot();	
				break;
			case EnemyStates.AFK:
				break;
			case EnemyStates.HUNTING:
				CheckAggroResetTime((float)delta);
				next = navagent.GetNextPathPosition();
				RotateBodyTowardsPlayer(false, next);
				Vector3 dir = GlobalPosition.DirectionTo(next);
				CheckIfCanShoot(PlayerDistance);
				if (next != Vector3.Zero)
				{
					velocity.X = dir.X * Speed;
					velocity.Z = dir.Z * Speed;
				} 
				
				GD.Randomize();
				int randi = GD.RandRange(1, 20);
				//if (randi == 10) Jump();
				break;
        }

		if (!IsOnFloor())
		{
			velocity.Y += Gravity * (float)delta;
		}

		if (ShouldStop)
        {
			velocity.X = 0;
			velocity.Z = 0;
        }
		
		navagent.Velocity = velocity;
		MoveAndSlide();
		//GD.Print(Velocity, groundcheck.IsColliding(), IsOnFloor(), canMove);
    }

    private void CheckAggroResetTime(float delta)
    {
        lastSawPlayerSeconds += delta;
        if (lastSawPlayerSeconds >= 5f)
		{
			GD.Print("We afk");
			cState = EnemyStates.AFK;
        }	
    }

    private void EnableHeadIndicator(){
		// if spotted , or in aggro range -> light up red dot on head to indicate target to player
		// either 3d model , or 3d sprite facing player. 3d can emit
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
			lookDir = (player.GlobalPosition - body.GlobalPosition).Normalized();
		else
			lookDir = (lookPos - body.GlobalPosition).Normalized();
	
		Vector3 targetForward = lookDir;
		float targetYaw = Mathf.Atan2(targetForward.X, targetForward.Z);

		Tween tween = GetTree().CreateTween();
		tween.TweenProperty(body, "rotation:y", targetYaw, 0.5);
	}

	public void CheckIfCanShoot(float distance)
	{
		//Raycast look at player, stop if in los, or move if not
		if (distance < ShootDistance)
		{
			cState = EnemyStates.SHOOTING;
		}
	}
	
	public void TryToShoot()
    {
		if (!canShoot) return;
		canShoot = false;

		SceneTreeTimer tr = GetTree().CreateTimer(2.0);
		tr.Timeout += ShootBullet;
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
		canShoot = true;
        bullet bulletInstance = Bullet.Instantiate() as bullet;
        bulletInstance.Position = GlobalPosition;

		bulletInstance.SetDirection((player.GlobalPosition - GlobalTransform.Origin).Normalized() * Speed);
		bulletInstance.SetProps(1, "enemy");

        GetParent().AddChild(bulletInstance);
		rocket.Play();
		cState = EnemyStates.HUNTING;
    }

	private void OnNavigationAgentVelocityComputed(Vector3 safevelo)
    {
        Velocity = safevelo;
    }

    private void OnNavigationAgentTargetReached()
	{
		if (cState == EnemyStates.AFK) return;
        SetTargetPos(player.GlobalPosition);
    }
	#pragma warning disable IDE0060
    private void OnNavigationAgentLinkReached(Dictionary data)
    {
        Jump();
    } 

	private void _on_retarget_timeout(){
		if (cState == EnemyStates.AFK) return;
		SetTargetPos(player.GlobalPosition);
		GD.Randomize();
		int randi = GD.RandRange(1, 5);
		retargetTimer.WaitTime = randi;
		retargetTimer.Start();
	}
}
