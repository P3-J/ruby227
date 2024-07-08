using Godot;
using System;

public partial class player : CharacterBody3D
{
    private const float Gravity = -4.8f;
    private const float JumpForce = 75.0f;
    private const float MovementSpeed = 20F;

    [Export] public PackedScene Bullet;
    [Export] private Marker3D rightArm;

    public override void _PhysicsProcess(double delta)
    {
        Vector3 fakeVelo = Velocity;

        float rotationInput = 0f;
        if (Input.IsActionPressed("left"))
            rotationInput += 0.05f;
        if (Input.IsActionPressed("right"))
            rotationInput -= 0.05f;
        RotateY(rotationInput);

        Vector3 direction = new Vector3();
        if (Input.IsActionPressed("up"))
            direction -= Transform.Basis.Z; 
        if (Input.IsActionPressed("down"))
            direction += Transform.Basis.Z; 
        direction = direction.Normalized();

        if (!IsOnFloor())
        {
            fakeVelo.Y += Gravity;
        }
        else
        {
            fakeVelo.Y = 0;
        }

        if (Input.IsActionJustPressed("jump") && IsOnFloor())
        {
            fakeVelo.Y = JumpForce;
        }

        fakeVelo.X = direction.X * MovementSpeed;
        fakeVelo.Z = direction.Z * MovementSpeed;

        Velocity = fakeVelo;
        MoveAndSlide();
    }

    public override void _Input(InputEvent @event)
    {
        if (Input.IsActionJustPressed("shoot"))
        {
            ShootBullet();
        }
    }

    public void ShootBullet()
    {
        CharacterBody3D bulletInstance = Bullet.Instantiate() as CharacterBody3D;
        bulletInstance.Position = rightArm.GlobalPosition;
        bulletInstance.Call("SetDirection", -GlobalTransform.Basis.Z);
        GetParent().AddChild(bulletInstance);
    }
}