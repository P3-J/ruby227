using Godot;
using System;

public partial class player : CharacterBody3D
{
    private const float Gravity = -4.8f;
    private const float JumpForce = 75.0f;
    private const float MovementSpeed = 15F;

    int HP = 3;
    int cHP = 3;

    [Export] public PackedScene Bullet;
    [Export] private Marker3D rightArm;
    [Export] AudioStreamPlayer booster;
	[Export] AudioStreamPlayer rocket;
    [Export] AudioStreamPlayer steam;
    [Export] RichTextLabel hudText;

    public override void _PhysicsProcess(double delta)
    {
        Vector3 fakeVelo = Velocity;

        float rotationInput = 0f;
        if (Input.IsActionPressed("left"))
            rotationInput += 0.02f;
            PlaySteamAudioIfCan();
        if (Input.IsActionPressed("right"))
            rotationInput -= 0.02f;
            PlaySteamAudioIfCan();

        if (!Input.IsActionPressed("left") && !Input.IsActionPressed("right")){
            steam.Stop();
        }
        
        RotateY(rotationInput);

        Vector3 direction = new();
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

        if (direction == Vector3.Zero){
            booster.Stop();
        } else if (!booster.Playing) {
            booster.Play();
        }

        Velocity = fakeVelo;
        MoveAndSlide();
    }

    public void RefreshHud(){
        hudText.Text = cHP.ToString() + "/" + HP.ToString() + " ❤️";
    }

    public void GetHit(){
        cHP -= 1;
        RefreshHud();
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
        bulletInstance.Call("Setowner", "player");
        GetParent().AddChild(bulletInstance);
        rocket.Play();
    }

    public void PlaySteamAudioIfCan(){
        if (!steam.Playing)
        {
            steam.Play();
        }
    }
}