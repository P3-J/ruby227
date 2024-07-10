using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;

public partial class enemy : CharacterBody3D
{
	public const float Speed = 5f;

	[Export] NavigationAgent3D navagent;
	[Export] CharacterBody3D player; // bad but gets the job done
	[Export] Node3D body;
	[Export] public PackedScene Bullet;
	[Export] Timer timer;

	[Export] AudioStreamPlayer3D booster;
	[Export] AudioStreamPlayer3D rocket;

	Boolean canMove;
	Boolean hasShot;

	public override void _Process(double delta)
	{

		navagent.TargetPosition = player.GlobalPosition;
		Vector3 direction = navagent.GetNextPathPosition();
		Vector3 _velocity = (direction - GlobalTransform.Origin).Normalized() * Speed;

		// can also check if the climb itself to the next point is too sudden?
		//https://docs.godotengine.org/en/stable/tutorials/navigation/navigation_using_navigationlinks.html
		// nav links!!!
		/* if (navagent.TargetPosition.Y - GlobalPosition.Y > 5 && !IsOnFloor())
        {
            _velocity.Y = 400;
        } */

		RotateBody(direction);
		Velocity = _velocity;
		if (!canMove){
			Velocity = Vector3.Zero;
			booster.Stop();
		} else if (!booster.Playing) {
			booster.Play();
		}
		
		MoveAndSlide();
	}

	public void RotateBody(Vector3 _direction){
		/* body.LookAt(_direction); */
	}

	public void ShootBullet()
    {
        CharacterBody3D bulletInstance = Bullet.Instantiate() as CharacterBody3D;
        bulletInstance.Position = GlobalPosition;
        bulletInstance.Call("SetDirection", (player.GlobalPosition - GlobalTransform.Origin).Normalized() * Speed);
        GetParent().AddChild(bulletInstance);
		rocket.Play();
		if (!canMove){
			timer.Start();
		}
    }

	private void _on_scanner_body_entered(Node3D body)
	{
		if (body.Name == "player"){
			canMove = true;
			timer.Start();
		}
	}

	private void _on_scanner_body_exited(Node3D body)
	{
		if (body.Name == "player"){
			canMove = true;
		}
	}

	private void _on_shot_cooldown_timeout(){
		ShootBullet();
	}

	private void _on_navigation_agent_3d_link_reached(Dictionary details){
		GD.Print(details);
	}
}
