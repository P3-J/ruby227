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
	Node3D bcontroller;

	public string owner;
	public int damage = 1;

    public override void _Ready()
    {
	    /* float angleRadians = Mathf.Atan2(_direction.X, _direction.Z);
		float angleDegrees = Mathf.RadToDeg(angleRadians);
		bulletBody.RotationDegrees = new Vector3(90, angleDegrees, 0); */ 

		bcontroller = GetNode<Node3D>("bcontroller");
		bcontroller.LookAt(GlobalTransform.Origin - _direction); // start - end = angle
    }

    public override void _PhysicsProcess(double delta)
	{
		if (collisionRay.IsColliding()){
			GodotObject collider = collisionRay.GetCollider();
			string colliderName = (string)collider.Get("name");
			if (colliderName != "entity_0_worldspawn" && colliderName != owner){
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
