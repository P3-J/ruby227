using Godot;
using System;
using System.Diagnostics.Tracing;

public partial class player : CharacterBody3D
{

    [Export] public PackedScene Bullet;
    [Export] private Marker3D rightArm;
    [Export] AudioStreamPlayer booster;
	[Export] AudioStreamPlayer rocket;
    [Export] AudioStreamPlayer steam;
    [Export] AudioStreamPlayer jumpboostsound;
    [Export] AudioStreamPlayer dropsound;
    [Export] ProgressBar hpBar;
    [Export] Node3D PlayerCamBase;

    Timer shotcooldown;
    GpuParticles3D leftshoulder;
    GpuParticles3D rightshoulder;
    RayCast3D MissileTargeter;
    ColorRect TSquare;
    Camera3D playercam;
    private const float Gravity = -2.8f;
    private const float JumpForce = 55.0f;
    private const float MovementSpeed = 15F;

    int HP = 3;
    int cHP = 3;
    bool playDropSound = false;
    CharacterBody3D CurrentTarget = null;


    public override void _Ready()
    {
        shotcooldown = GetNode<Timer>("shotcooldown");
        leftshoulder = GetNode<GpuParticles3D>("mech/leftshoulder");
        rightshoulder = GetNode<GpuParticles3D>("mech/rightshoulder");
        MissileTargeter = GetNode<RayCast3D>("targeter");
        TSquare = GetNode<ColorRect>("guid/tsquare");
        playercam = GetNode<Camera3D>("camBase/Camera3D");
        SetupHud();
    }

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
        HandleCameraTurning();
        TargeterPosition();

        Vector3 direction = new();
        if (Input.IsActionPressed("up"))
            direction -= Transform.Basis.Z; 
        if (Input.IsActionPressed("down"))
            direction += Transform.Basis.Z; 
        direction = direction.Normalized();

        if (CurrentTarget != null){
            ReposSquare(CurrentTarget.GlobalTransform.Origin);
        }


        if (!IsOnFloor())
        {
            fakeVelo.Y += Gravity;
            playDropSound = true;
        }
        else
        {
            if (playDropSound && !dropsound.Playing){
                dropsound.Play();
            }
            playDropSound = false;
            fakeVelo.Y = 0;
        }

        if (Input.IsActionJustPressed("jump") && IsOnFloor())
        {
            fakeVelo.Y = JumpForce;
            jumpboostsound.Play();
        }

        fakeVelo.X = direction.X * MovementSpeed;
        fakeVelo.Z = direction.Z * MovementSpeed;

        if (direction == Vector3.Zero){
            booster.Stop();
            ShoulderParticleController(false);
        } else if (!booster.Playing) {
            booster.Play();
            ShoulderParticleController(true);
        }

        Velocity = fakeVelo;
        MoveAndSlide();
    }

    public void RefreshHud(){
        Tween tween = GetTree().CreateTween();
        tween.TweenProperty(hpBar, "value", cHP, 0.5);
    }
    public void SetupHud(){
        hpBar.MaxValue = HP;
    }

    public void GetHit(){
        cHP -= 1;
        RefreshHud();
    }

    public void TargeterPosition(){
        if (MissileTargeter.IsColliding()){
            var collider = MissileTargeter.GetCollider();
            if (collider is CharacterBody3D){
                CharacterBody3D enemy = (CharacterBody3D)collider;
                CurrentTarget = enemy;
            }
        }
    }

    public void ReposSquare(Vector3 globaltransform){
        Vector2 screenpos = playercam.UnprojectPosition(globaltransform);

        if (!playercam.IsPositionBehind(globaltransform) && playercam.IsPositionInFrustum(globaltransform)){
            TSquare.Position = TSquare.Position.MoveToward(screenpos, 5f);
        } else {
            TSquare.Position = TSquare.Position.MoveToward(new Vector2(236, 236), 5f);
        }

    }

    public void HandleCameraTurning(){
         if (Input.IsActionPressed("camleft")){
			Vector3 v = PlayerCamBase.RotationDegrees;
			v.Y += 3f;
			PlayerCamBase.RotationDegrees = v;
		}

		 if (Input.IsActionPressed("camright")){
			Vector3 v = PlayerCamBase.RotationDegrees;
			v.Y -= 3f;
			PlayerCamBase.RotationDegrees = v;
		}
    }

    public override void _Input(InputEvent @event)
    {
        if (Input.IsActionJustPressed("shoot") && shotcooldown.IsStopped())
        {
            shotcooldown.Start();
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

    public void ShoulderParticleController(bool state){
        leftshoulder.Emitting  = state;
        rightshoulder.Emitting = state;
    }

}