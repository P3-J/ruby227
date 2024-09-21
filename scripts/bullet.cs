using System;
using System.Net;
using Godot;
public partial class bullet : CharacterBody3D
{
	[Export] public float BulletSpeed;
	private Vector3 _direction;
	private Vector3 _velocity;

	[Export] MeshInstance3D bulletBody;
	[Export] RayCast3D collisionRay;
	[Export] PackedScene explosion;

	public string owner;
	public int damage = 1;

    public override void _Ready()
    {
	    /* float angleRadians = Mathf.Atan2(_direction.X, _direction.Z);
		float angleDegrees = Mathf.RadToDeg(angleRadians);

		float x = Mathf.RadToDeg(Mathf.Atan2(_direction.Y, _direction.Z)); */
		/* float xDegWithRot = 90 - Mathf.RadToDeg(_direction.X);
		bulletBody.RotationDegrees = new Vector3(xDegWithRot, angleDegrees, 0); */ 

		bulletBody.LookAt(_direction);
		GD.Print(bulletBody.Rotation);
    }

    public override void _PhysicsProcess(double delta)
	{
		if (collisionRay.IsColliding()){
			GodotObject collider = collisionRay.GetCollider();
			string colliderName = (string)collider.Get("name");
			if (colliderName != "entity_0_worldspawn" && colliderName != owner){
				// little hardcoding never killed anyone L
				GD.Print(colliderName, owner);
				collider.Call("GetHit", damage);
			}
			GenerateExplosion(collisionRay.GetCollisionPoint());
			QueueFree();
			
		}

		_velocity = _direction * BulletSpeed;
		Velocity = _velocity;
		MoveAndSlide();
	}

	public void SetOwner(string passedowner){
		owner = passedowner;
	}

	public void SetDamage(int dmg){
		damage = dmg;
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
        GetParent().AddChild(explosionInstance); // mob dies, calls getparent, no parent anymore, so throws error -> maybe top level node as parent
	}

}
