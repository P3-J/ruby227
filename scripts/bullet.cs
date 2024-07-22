using Godot;
using Godot.Collections;
using System;
using System.Linq;

public partial class bullet : CharacterBody3D
{
	[Export] public float BulletSpeed;
	private Vector3 _direction;
	private Vector3 _velocity;

	[Export] MeshInstance3D bulletBody;
	[Export] RayCast3D collisionRay;
	[Export] PackedScene explosion;

	public string owner;

    public override void _Ready()
    {
		float angleRadians = Mathf.Atan2(_direction.X, _direction.Z);
		float angleDegrees = Mathf.RadToDeg(angleRadians);
		bulletBody.RotationDegrees = new Vector3(90, angleDegrees, 0);
    }

    public override void _PhysicsProcess(double delta)
	{
		string[] names = new string[] {"enemy", "player", "bossman"};
		if (collisionRay.IsColliding()){
			GodotObject collider = collisionRay.GetCollider();
			if (names.Contains((string)collider.Get("name"))){
				collider.Call("GetHit");
			}
			GenerateExplosion(collisionRay.GetCollisionPoint());
			QueueFree();
		}

		_velocity = _direction * BulletSpeed;
		Velocity = _velocity;
		MoveAndSlide();
	}

	public void Setowner(string passedowner){
		owner = passedowner;
	}

	public void SetDirection(Vector3 newDire)
	{
		_direction = newDire.Normalized();
	}

	public void _on_queuefree_timeout(){
		QueueFree();
	}

	public void GenerateExplosion(Vector3 pos)
	{
		GpuParticles3D explosionInstance = explosion.Instantiate() as GpuParticles3D;
        explosionInstance.Position = pos;
		explosionInstance.Emitting = true;
        GetParent().AddChild(explosionInstance);
	}

}
