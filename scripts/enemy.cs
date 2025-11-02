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

	enum EnemyStates { AFK, HUNTING, SHOOTING }
	private EnemyStates cState = EnemyStates.AFK;

	int HP = 1;
	int cHP = 1;
    bool target;
	bool Disabled = false;
	float lastSawPlayerSeconds;

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

		lastSawPlayerSeconds += (float)delta;
        if (lastSawPlayerSeconds >= 5f)
		{
			GD.Print("We afk");
			cState = EnemyStates.AFK;
        }	

		Node Collider = null;
		if (los.IsColliding()) Collider = (Node)los.GetCollider();
		if (Collider is player)
        {
        	PlayerDistance = GetPlayerDistance();
			lastSawPlayerSeconds = 0;
			cState = EnemyStates.HUNTING;
        }
		hasAggro = PlayerDistance < AggroDistance;
		if (hasAggro) ColliderMovementController(PlayerDistance, (player)Collider); 
			

		velocity = Velocity;

		if (IsOnFloor())
		{
			next = navagent.GetNextPathPosition();
			RotateBody(next);
			if (!canMove)
            {
                velocity.X = 0;
				velocity.Z = 0;
            }
		}
		else
		{
			velocity.Y += Gravity * (float)delta;
		}

		Vector3 dir = GlobalPosition.DirectionTo(next);
		
        if (next != Vector3.Zero && canMove){
			velocity.X = dir.X * Speed;
			velocity.Z = dir.Z * Speed;
		} 

		navagent.Velocity = velocity;
		MoveAndSlide();
		//GD.Print(Velocity, groundcheck.IsColliding(), IsOnFloor(), canMove);
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


	public void RotateBody(Vector3 _direction){

		Vector3 lookDir;

		if (!canMove)
			lookDir = (player.GlobalPosition - body.GlobalPosition).Normalized();
		else
			lookDir = (_direction - body.GlobalPosition).Normalized();
	
		Vector3 targetForward = lookDir;
		float targetYaw = Mathf.Atan2(targetForward.X, targetForward.Z);

		Tween tween = GetTree().CreateTween();
		tween.TweenProperty(body, "rotation:y", targetYaw, 0.25);
	}

	public void ColliderMovementController(float distance, player collider){
		//Raycast look at player, stop if in los, or move if not
	
		if (distance < ShootDistance){
			canMove = false;
			RotateBody(player.GlobalPosition);
			if (timer.IsStopped()) StartShotTimer();
		} 
	}

	public void StartShotTimer(){
		GD.Randomize();
		int randi = GD.RandRange(1, 3);
		timer.WaitTime = randi;
		timer.Start();
	}

	public void Jump()
	{
		if (IsOnFloor())
        {
			velocity.Y = 0;
            velocity.Y +=  jumpstr;
        }
	}

	public void ShootBullet()
    {
        bullet bulletInstance = Bullet.Instantiate() as bullet;
        bulletInstance.Position = GlobalPosition;
		Vector3 playerPos = player.GlobalPosition;

		bulletInstance.SetDirection((playerPos - GlobalTransform.Origin).Normalized() * Speed);
		bulletInstance.SetProps(1, "enemy");

        GetParent().AddChild(bulletInstance);
		rocket.Play();
		if (!canMove){
			StartShotTimer();
		}
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

	private void _on_shot_cooldown_timeout(){
		if (!Disabled){
			ShootBullet();
			canMove = true;
		}
	}
	
	private void _on_retarget_timeout(){
		if (cState == EnemyStates.AFK) return;
		SetTargetPos(player.GlobalPosition);
		GD.Randomize();
		int randi = GD.RandRange(1, 3);
		retargetTimer.WaitTime = randi;
		retargetTimer.Start();
	}
}
