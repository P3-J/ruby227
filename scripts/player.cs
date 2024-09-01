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
    Sprite2D TSquare;
    Camera3D playercam;
    Timer EnemyTimer;
    Area3D DetArea;
    private const float Gravity = -2.8f;
    private const float JumpForce = 55.0f; //55
    private const float MovementSpeed = 30F; //15

    int HP = 3;
    int cHP = 3;
    bool playDropSound = false;
    bool canSeeEnemy = false;
    bool targetLocked = false;
    CharacterBody3D CurrentTarget = null;
    Marker3D MissileLaunchSpot;
    MeshInstance3D MissileMesh;


    public override void _Ready()
    {
        shotcooldown = GetNode<Timer>("shotcooldown");
        leftshoulder = GetNode<GpuParticles3D>("mech/leftshoulder");
        rightshoulder = GetNode<GpuParticles3D>("mech/rightshoulder");
        MissileTargeter = GetNode<RayCast3D>("targeter");
        TSquare = GetNode<Sprite2D>("guid/tsquare");
        playercam = GetNode<Camera3D>("camBase/Camera3D");
        EnemyTimer = GetNode<Timer>("detectionarea/enemytimer");
        DetArea = GetNode<Area3D>("detectionarea");
        MissileLaunchSpot = GetNode<Marker3D>("mech/missilelauncher/missilelauncherspot");
        MissileMesh = GetNode<MeshInstance3D>("mech/missilelauncher");
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
        HandleCameraTurning(); // includes the left launcher rotation currently, should be based on targeter, doesnt have to be tho

        Vector3 direction = new();
        if (Input.IsActionPressed("up"))
            direction -= Transform.Basis.Z; 
        if (Input.IsActionPressed("down"))
            direction += Transform.Basis.Z; 
        direction = direction.Normalized();


        if (CurrentTarget != null && IsInstanceValid(CurrentTarget)) {
            TargeterPosition();
            ReposSquare(CurrentTarget.GlobalTransform.Origin);
        } else {
            ResetTargetingSquare();
            targetLocked = false;
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

    public void HandleCameraTurning(){
         if (Input.IsActionPressed("camleft")){
			Vector3 v = PlayerCamBase.RotationDegrees;
			v.Y += 3f;
			PlayerCamBase.RotationDegrees = v;
            v.Z = -90;
            MissileMesh.RotationDegrees = v;
		}

		 if (Input.IsActionPressed("camright")){
			Vector3 v = PlayerCamBase.RotationDegrees;
			v.Y -= 3f;
			PlayerCamBase.RotationDegrees = v;
            v.Z = -90;
            MissileMesh.RotationDegrees = v;
		}
    }

    public override void _Input(InputEvent @event)
    {
        if (Input.IsActionJustPressed("shoot") && shotcooldown.IsStopped())
        {
            shotcooldown.Start();
            ShootBullet(GlobalTransform);
        }
        if (Input.IsActionJustPressed("shootleft")) {
            Vector2 pos2 = TSquare.GlobalPosition;

            if (targetLocked){
               pos2.Y += 10; // sprite a bit higher then origin point so lower it.
            } else {
                pos2.Y -= 30; // boost even more on a not locked target.
            }
            Vector3 pos3 = playercam.ProjectPosition(pos2, 50);
            ShootBullet(pos3);
        }
    }

    public void ShootBullet(Transform3D pos)
    {
        CharacterBody3D bulletInstance = Bullet.Instantiate() as CharacterBody3D;
        bulletInstance.Position = rightArm.GlobalPosition;
        bulletInstance.Call("SetDirection", -pos.Basis.Z);
        bulletInstance.Call("SetOwner", "player");
        GetParent().AddChild(bulletInstance);
        rocket.Play();
    }

    public void ShootBullet(Vector3 targetPosition)
    {
        // meant for left arm
        CharacterBody3D bulletInstance = Bullet.Instantiate() as CharacterBody3D;
        bulletInstance.Position = MissileLaunchSpot.GlobalPosition;
        Vector3 direction = (targetPosition - MissileLaunchSpot.GlobalPosition).Normalized();
        bulletInstance.Call("SetDirection", direction);
        bulletInstance.Call("SetOwner", "player");
        GetParent().AddChild(bulletInstance);
        rocket.Play();
    }

    public void _on_enemytimer_timeout(){
        ScanForEnemies();
        EnemyTimer.Start();
    }

    public void TargeterPosition(){
        MissileTargeter.LookAt(CurrentTarget.GlobalPosition);
        if (MissileTargeter.IsColliding() && MissileTargeter.GetCollider() is CharacterBody3D){
            canSeeEnemy = true;
        } else {
            canSeeEnemy = false;
        }
    }

    public void ReposSquare(Vector3 globaltransform){
        Vector2 screenpos = playercam.UnprojectPosition(globaltransform);
        screenpos.Y -= 15; // OFFSET so sprite is centered.
        /// text next to it indicating distance / else Zero 
        /// do anti of unproject - project position into world to aim

        if (!playercam.IsPositionBehind(globaltransform) && playercam.IsPositionInFrustum(globaltransform) && canSeeEnemy){
            TSquare.Position = TSquare.Position.MoveToward(screenpos, 5f);
            targetLocked = true;
        } else {
            ResetTargetingSquare();
            targetLocked = false;
        }
    }

    public void ResetTargetingSquare(){
        // reset targeting square to center of screen
        TSquare.Position = TSquare.Position.MoveToward(new Vector2(256, 256), 5f);
    }

    public void ScanForEnemies(){
        if (DetArea.HasOverlappingBodies()){
            Godot.Collections.Array<Node3D> enemies = DetArea.GetOverlappingBodies();

            float Distance = 10000.0f; //cutoff
            foreach (Node3D enemy in enemies)
            {
                if (enemy is CharacterBody3D && (string)enemy.Get("name") != "player"){
                    float distanceTo = enemy.GlobalPosition.DistanceTo(GlobalPosition);
                    if (distanceTo < Distance){
                        Distance = distanceTo;
                        CurrentTarget = (CharacterBody3D)enemy;
                    }
                }
            }
        }
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