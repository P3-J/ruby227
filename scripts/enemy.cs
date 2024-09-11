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
	bool targetinlos;
	bool hasAggro;

	AudioStreamPlayer3D booster;
	AudioStreamPlayer3D rocket;

	Boolean canMove = true;
	Vector3 velocity;
	public const float Gravity = -9.8f;
	public const float jumpstr = 10f;
	Vector3 next = Vector3.Zero;
	RayCast3D groundcheck;


	int HP = 4;
	int cHP = 4;
    bool target;

	/// <summary>
	///  TO FIX
	///  los can target other enemies, this should not be a factor - ? i think fake news
	///  look at, is looking at the final destination. not good - done
	///  randomize speed, instead of latency to introduce some randomness? 
	///  plus minus bullet angle, so that it has the ability to be a tracing shot \\ would miss standing targets -- quite ok rn with lag
	///  but can create quite a lot of randomness ? 
	///  death anim
	///  container ship/
	///  invisible barriers just for bots
	/// </summary>
    public override void _Ready()
    {

		navagent = GetNode<NavigationAgent3D>("NavigationAgent3D");
		body = GetNode<Node3D>("bodyController");
		timer = GetNode<Timer>("shotCooldown");
		los = GetNode<RayCast3D>("los");

		retargetTimer = GetNode<Timer>("retarget");
		booster = GetNode<AudioStreamPlayer3D>("booster");
		rocket  =GetNode<AudioStreamPlayer3D>("rocket");
		groundcheck = GetNode<RayCast3D>("groundcheck");

        navagent.Connect("target_reached", new Callable(this, nameof(OnNavigationAgentTargetReached)));
        navagent.Connect("velocity_computed", new Callable(this, nameof(OnNavigationAgentVelocityComputed)));
        navagent.Connect("link_reached", new Callable(this, nameof(OnNavigationAgentLinkReached))); 

		SceneTreeTimer tr = GetTree().CreateTimer(1.0);
		tr.Timeout += OnTimeOut;

       
    }

	private void OnTimeOut()
	{
		target = true;
		SetTargetPos(player.GlobalPosition);
	}

	

    public override void _PhysicsProcess(double delta)
    {
		if (!target){
			return;
		}
		los.LookAt(player.GlobalPosition);

		float distance = 999;
		if (los.GetCollider() == player){
			Vector3 origin = los.GlobalPosition;
			Vector3 colPoint = los.GetCollisionPoint();
			distance = origin.DistanceTo(colPoint);
			GD.Print(distance);
		}
		ColliderMovementController(distance);

		if (!hasAggro){
			AggroCheck(distance);
			return;
		}

		velocity = Velocity;
		if (IsOnFloor() && groundcheck.IsColliding()) {
			next = navagent.GetNextPathPosition();
			RotateBody(next);
		}
			
		if (!groundcheck.IsColliding())
        {
            velocity.Y += Gravity * (float)delta;
        }

		Vector3	dir = GlobalPosition.DirectionTo(next);
        if (next != Vector3.Zero && canMove && groundcheck.IsColliding()){
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

	public void AggroCheck(float distance){
		if (distance > AggroDistance){
			return;
		} 
		hasAggro = true;
	}

	public void GetHit(int dmg){
		GD.Print("hit");
        cHP -= dmg;
		if (cHP <= 0)
			QueueFree();
    }

	public void SetTargetPos(Vector3 pos)
	{
		var map = GetWorld3D().NavigationMap;
		var p = NavigationServer3D.MapGetClosestPoint(map, pos);
		navagent.TargetPosition = p;
	}


	public void RotateBody(Vector3 _direction){	
		if (!canMove) {
			body.LookAt(player.GlobalPosition, Vector3.Up);
		} else {
			body.LookAt(_direction, Vector3.Up);
		}
		Vector3 rot = body.Rotation;
		rot.X = 0;
		rot.Z = 0;
		body.Rotation = rot;
	}

	public void ColliderMovementController(float distance){
		//Raycast look at player, stop if in los, or move if not
		var collider = los.GetCollider();
		
		if (collider is CharacterBody3D && distance < ShootDistance){
			if ((string)collider.Get("name") == "player"){
				canMove = false;
				RotateBody(player.GlobalPosition);
				if (IsOnFloor()){
					velocity.X = 0;
					velocity.Z = 0;
				}
				if (timer.IsStopped()){
					StartShotTimer();
				}
			}
		} else {
			canMove = true;
			//SetTargetPos(player.GlobalPosition); this causes constant recalculations
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
        CharacterBody3D bulletInstance = Bullet.Instantiate() as CharacterBody3D;
        bulletInstance.Position = GlobalPosition;
        bulletInstance.Call("SetDirection", (player.GlobalPosition - GlobalTransform.Origin).Normalized() * Speed);
		bulletInstance.Call("SetOwner", "enemy");
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
        SetTargetPos(player.GlobalPosition);
        // wait some time to randomize the movement 
    }

    private void OnNavigationAgentLinkReached(Dictionary data)
    {
        Jump();
    } 

	private void _on_shot_cooldown_timeout(){
		ShootBullet();
	}
	
	private void _on_retarget_timeout(){
		SetTargetPos(player.GlobalPosition);
		GD.Randomize();
		int randi = GD.RandRange(1, 3);
		retargetTimer.WaitTime = randi;
		retargetTimer.Start();
	}
}
