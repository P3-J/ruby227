using Godot;
using Godot.Collections;
using System;

public partial class enemy : CharacterBody3D
{
	public const float Speed = 10f;

	[Export] private NavigationAgent3D navagent;
	[Export] CharacterBody3D player; // bad but gets the job done
	Node3D body;
	[Export] public PackedScene Bullet;
	Timer timer;
	Timer retargetTimer;
	RayCast3D los;
	bool targetinlos;

	AudioStreamPlayer3D booster;
	AudioStreamPlayer3D rocket;

	Boolean canMove = true;
	Vector3 velocity;
	public const float Gravity = -9.8f;
	Vector3 next = Vector3.Zero;


	int HP = 2;
	int cHP = 2;
    bool target;
    public override void _Ready()
    {

		navagent = GetNode<NavigationAgent3D>("NavigationAgent3D");
		body = GetNode<Node3D>("bodyController");
		timer = GetNode<Timer>("shotCooldown");
		los = GetNode<RayCast3D>("los");

		retargetTimer = GetNode<Timer>("retarget");
		booster = GetNode<AudioStreamPlayer3D>("booster");
		rocket  =GetNode<AudioStreamPlayer3D>("rocket");

		// export not good all the time whaat... especially currently for signals???
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
		var collider = los.GetCollider();
		if (collider is CharacterBody3D){
			GD.Print(collider.GetType(), collider.Get("name"));
			if ((string)collider.Get("name") == "player"){
				
				canMove = false;
				Velocity = Vector3.Zero;
				if (timer.IsStopped())
					timer.Start();
			}
		} else {
			canMove = true;
			SetTargetPos(player.GlobalPosition);
		}


		velocity = Velocity;
		if (IsOnFloor())
			next = navagent.GetNextPathPosition();
			if (canMove)
				RotateBody(navagent.TargetPosition);
			else
				RotateBody(player.GlobalPosition);


		if (!IsOnFloor())
        {
            velocity.Y += Gravity * (float)delta;
        }

		Vector3 dir = GlobalPosition.DirectionTo(next);
        if (dir != Vector3.Zero && canMove){
			velocity.X = dir.X * Speed;
			velocity.Z = dir.Z * Speed;
		} 

		navagent.Velocity = velocity;
		MoveAndSlide();
    }

	public void GetHit(){
        cHP -= 1;
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
		body.LookAt(_direction, Vector3.Up);
	}

	public void Jump()
	{
		if (IsOnFloor())
        {
            velocity.Y += 15;
        }
	}

	public void ShootBullet()
    {
        CharacterBody3D bulletInstance = Bullet.Instantiate() as CharacterBody3D;
        bulletInstance.Position = GlobalPosition;
        bulletInstance.Call("SetDirection", (player.GlobalPosition - GlobalTransform.Origin).Normalized() * Speed);
		bulletInstance.Call("Setowner", "enemy");
        GetParent().AddChild(bulletInstance);
		rocket.Play();
		if (!canMove){
			timer.Start();
		}
    }

	private void OnNavigationAgentVelocityComputed(Vector3 safevelo)
    {
        Velocity = safevelo;
    }

    private void OnNavigationAgentTargetReached()
    {
        SetTargetPos(player.GlobalPosition);
        GD.Print("Target reached");
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
		retargetTimer.Start();
	}
}
