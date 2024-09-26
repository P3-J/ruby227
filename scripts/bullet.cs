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
	public float extraSpeed = 0f;

    public override void _Ready()
    {
		bcontroller = GetNode<Node3D>("bcontroller");
		bcontroller.LookAt(GlobalTransform.Origin - _direction); // start - end = angle
    }

    public override void _PhysicsProcess(double delta)
	{
		if (collisionRay.IsColliding()){
			GodotObject collider = collisionRay.GetCollider();
			if (collider is CharacterBody3D){
				collider.Call("GetHit", damage);
			}   // faster, alternative can use masks, do not need to set owner
			
			GenerateExplosion(collisionRay.GetCollisionPoint());
			QueueFree();
			
		}

		_velocity = _direction * (BulletSpeed + extraSpeed);
		Velocity = _velocity;
		MoveAndSlide();
	}

	public void SetOwner(string passedowner){
		owner = passedowner;
	}

	public void SetDamage(int dmg){
		damage = dmg;
	}

	public void RemoveRayCastMask(int layer, bool state){
		collisionRay.SetCollisionMaskValue(layer, state);
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
