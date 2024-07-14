using Godot;
using Godot.Collections;
using System;

public partial class enemy : CharacterBody3D
{
	public const float Speed = 10f;

	[Export] private NavigationAgent3D navagent;
	[Export] CharacterBody3D player; // bad but gets the job done
	[Export] Node3D body;
	[Export] public PackedScene Bullet;
	[Export] Timer timer;
	[Export] Timer retargetTimer;

	[Export] AudioStreamPlayer3D booster;
	[Export] AudioStreamPlayer3D rocket;

	Boolean canMove = true;
	Vector3 velocity;
	public const float Gravity = -9.8f;
	Vector3 next = Vector3.Zero;


	int HP = 3;
	int cHP = 3;
    bool target;
    public override void _Ready()
    {
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

		velocity = Velocity;
		if (IsOnFloor())
			next = navagent.GetNextPathPosition();

		if (!IsOnFloor())
        {
            velocity.Y += Gravity * (float)delta;
        }

		RotateBody(player.GlobalPosition);

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
    }

	public void SetTargetPos(Vector3 pos)
	{
		var map = GetWorld3D().NavigationMap;
		GD.Randomize();
		var p = NavigationServer3D.MapGetClosestPoint(map, pos);
		navagent.TargetPosition = p;
	}

	private void _on_navigation_agent_3d_target_reached(){
		
		SetTargetPos(player.GlobalPosition);
		GD.Print("reached");
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

	private void _on_scanner_body_entered(Node3D body)
	{
		/* if (body.Name == "player"){
			canMove = false;
			Velocity = Vector3.Zero;
			timer.Start();
		} */
	}
	private void _on_scanner_body_exited(Node3D body)
	{
		/* if (body.Name == "player"){
			canMove = true;
			SetTargetPos(player.GlobalPosition);
		} */
	}

	private void _on_navigation_agent_3d_velocity_computed(Vector3 safevelo){
		Velocity = safevelo;
	}
	private void _on_shot_cooldown_timeout(){
		ShootBullet();
	}
	private void _on_navigation_agent_3d_link_reached(Dictionary details){
		Jump();
	}
	private void _on_retarget_timeout(){
		SetTargetPos(player.GlobalPosition);
		retargetTimer.Start();
	}
}
