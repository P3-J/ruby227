using System;
using System.Net;
using Godot;
public partial class bullet : CharacterBody3D
{
	[Export] public float BulletSpeed;
	[Export] MeshInstance3D bulletBody;
	[Export] RayCast3D collisionRay;
	[Export] PackedScene explosion;
	Node3D bcontroller;
	private Vector3 _direction;
	private Vector3 _velocity;
	public int damage = 1;
	public float extraSpeed = 0f;
	public string ownerGroup;

    public override void _Ready()
    {
		bcontroller = GetNode<Node3D>("bcontroller");
		bcontroller.LookAt(GlobalTransform.Origin - _direction); // start - end = angle
    }

    public override void _Process(double delta)
	{

		checkForCollision();
		
		_velocity = _direction * (BulletSpeed + extraSpeed);
		Velocity = _velocity;
		MoveAndSlide();
	}

	public void SetProps(int dmg, string ownergroup)
	{
		damage = dmg;
		ownerGroup = ownergroup;
	}
	
	private void checkForCollision()
    {
        if (!collisionRay.IsColliding()) { return; }
		Node collider = (Node)collisionRay.GetCollider();
		if (collider.IsInGroup(ownerGroup)) { return; }
		
		if (collider.GetGroups().Count != 0)
		{
			collider.Call("GetHit", damage);
		} 
		GenerateExplosion(collisionRay.GetCollisionPoint());
		QueueFree();
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
