using Godot;
using System;

public partial class bullet : CharacterBody3D
{
	[Export] public float BulletSpeed;
	private Vector3 _direction;
	private Vector3 _velocity;

	[Export] MeshInstance3D bulletBody;
	[Export] RayCast3D collisionRay;

    public override void _Ready()
    {
		float angleRadians = Mathf.Atan2(_direction.X, _direction.Z);
		float angleDegrees = Mathf.RadToDeg(angleRadians);
		bulletBody.RotationDegrees = new Vector3(90, angleDegrees, 0);
    }

    public override void _PhysicsProcess(double delta)
	{
		
		if (collisionRay.IsColliding()){
			GodotObject collider = collisionRay.GetCollider();
			if (collider is not CharacterBody3D)
			{
				QueueFree();
			}
		}

		_velocity = _direction * BulletSpeed;
		Velocity = _velocity;
		MoveAndSlide();
	}

	public void SetDirection(Vector3 newDire)
	{
		_direction = newDire.Normalized();
	}

}
