using System;
using System.Net;
using Godot;
public partial class bullet : CharacterBody3D
{
	[Export] public float BulletSpeed;
	[Export] MeshInstance3D bulletBody;
	[Export] RayCast3D collisionRay;
	[Export] PackedScene explosion;
	[Export] GpuParticles3D trail;
	[Export] MeshInstance3D body;

	public enum BulletType
    {
        EXPLODING,
		FIVEFIVESIX,
    }

	BulletType cBulletType = BulletType.FIVEFIVESIX;
	
	int damage = 1;
	float extraSpeed = 0f;
	string ownerGroup;
	bool applyVeloExtra;

	Node3D bcontroller;
	Vector3 _direction;
	Vector3 _velocity;
	Vector3 Velo;

    public override void _Ready()
    {
		bcontroller = GetNode<Node3D>("bcontroller");
		bcontroller.LookAt(GlobalTransform.Origin - _direction); // start - end = angle

		switch (cBulletType) {
            case BulletType.EXPLODING:
				trail.Emitting = true;
				body.Scale = new Vector3(1f, 1f, 1f);
                break;
            case BulletType.FIVEFIVESIX:
				trail.Emitting = false;
				body.Scale = new Vector3(0.3f, 0.3f, 0.3f);
				break;
        }
    }

    public override void _Process(double delta)
	{
		if (Velocity.Length() > 0.1) {
            
    		bcontroller.LookAt(bcontroller.GlobalTransform.Origin + Velocity.Normalized());
        }
		checkForCollision();
	}

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

		_velocity = _direction * (BulletSpeed + extraSpeed);
		Vector3 extraVelo = applyVeloExtra ? Velo : Vector3.Zero;
		Velocity = _velocity + extraVelo;

		MoveAndSlide();
    }

	public void SetProps(int dmg, string ownergroup, Vector3 velo, float extraspeed = 0, bool abx = false, BulletType btype = BulletType.FIVEFIVESIX)
	{
		damage = dmg;
		ownerGroup = ownergroup;
		Velo = velo * 0.2f;
		extraSpeed = extraspeed;
		applyVeloExtra = abx;
		cBulletType = btype;
	}
	
	private void checkForCollision()
    {
        if (!collisionRay.IsColliding()) { return; }
		Node collider = (Node)collisionRay.GetCollider();


		if (collider.IsInGroup(ownerGroup)) { return; }
		
		if (collider.GetGroups().Count != 0 && IsInstanceValid(collider))
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
