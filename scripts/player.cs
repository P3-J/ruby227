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
    [Export] Guid guid;
    [Export] Node3D PlayerCamBase;

    Timer shotcooldown;
    Timer shotcooldownLeft;
    RayCast3D MissileTargeter;
    GpuParticles3D RightSteam;
    GpuParticles3D LeftSteam;
    Node2D tsquareController;
    Camera3D playercam;
    Timer EnemyTimer;
    Area3D DetArea;
    AnimationPlayer deathanim;
    GpuParticles3D deathExplosion;
    CharacterBody3D CurrentTarget = null;
    Marker3D MissileLaunchSpot;
    Node3D MissileLauncherSwivel;

    private const float Gravity = -2.8f;
    private const float JumpForce = 45.0f; //55
    private const float MovementSpeed = 30F; //15

    int HP = 4;
    int cHP = 4;
    int cPower = 100;
    int Power = 100;
    bool playDropSound = false;
    bool canSeeEnemy = false;
    bool targetLocked = false;
    bool canMove = true;

    public override void _Ready()
    {
        shotcooldown = GetNode<Timer>("soundsCooldowns/shotcooldown");
        shotcooldownLeft = GetNode<Timer>("soundsCooldowns/shotcooldownleft");

        MissileTargeter = GetNode<RayCast3D>("targeter");
        playercam = GetNode<Camera3D>("camBase/Camera3D");
        EnemyTimer = GetNode<Timer>("detectionarea/enemytimer");
        DetArea = GetNode<Area3D>("detectionarea");
        MissileLaunchSpot = GetNode<Marker3D>("mech/missilelauncherspot");

        deathExplosion = GetNode<GpuParticles3D>("particles/explosion");
        deathanim = GetNode<AnimationPlayer>("guid/deathscreen/anim");

       
        Input.MouseMode = Input.MouseModeEnum.Captured;
        guid.SetupHud(HP);
    }

    public override void _PhysicsProcess(double delta)
    {

        if (!canMove){
        // most likely death.
            booster.Stop();
            steam.Stop();
            return;
        }

        Vector3 fakeVelo = Velocity;

        HandleTurning();
        HandleCameraTurning(); // includes the left launcher rotation currently, should be based on targeter, doesnt have to be tho

        Vector3 direction = new();
        if (Input.IsActionPressed("up"))
            direction -= Transform.Basis.Z; 
        if (Input.IsActionPressed("down"))
            direction += Transform.Basis.Z; 
        direction = direction.Normalized();


        if (CurrentTarget != null && IsInstanceValid(CurrentTarget)) {
            TargeterPosition();
            targetLocked = guid.ReposSquare(CurrentTarget.GlobalTransform.Origin, canSeeEnemy);
        } else {
            guid.ResetTargetingSquare();
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

        fakeVelo.X = direction.X * MovementSpeed;
        fakeVelo.Z = direction.Z * MovementSpeed;

        if (Input.IsActionJustPressed("jump") && IsOnFloor())
        {
            fakeVelo.Y = JumpForce;
            jumpboostsound.Play();
        }

        if (direction == Vector3.Zero){
            booster.Stop();
        } else if (!booster.Playing) {
            booster.Play();
        }

        Velocity = fakeVelo;
        MoveAndSlide();
    }



    public void HandleTurning(){
        float rotationInput = 0f;
        if (Input.IsActionPressed("left")){
            rotationInput += 0.02f;
            PlaySteamAudioIfCan();
        }
        if (Input.IsActionPressed("right")){
            rotationInput -= 0.02f;
            PlaySteamAudioIfCan();
        }

        if (!Input.IsActionPressed("left") && !Input.IsActionPressed("right")){
            steam.Stop();
        }
        RotateY(rotationInput);
    }

    public void GetHit(int dmg){
        cHP -= dmg;
        guid.RefreshHud(cHP);
        // check for megadeth
        if (cHP <= 0){
            Die();
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
            ShootBullet(GlobalTransform);
        }
        if (Input.IsActionJustPressed("shootleft") && shotcooldownLeft.IsStopped()) {
            shotcooldownLeft.Start();
            Vector2 pos2 = guid.tsquareController.GlobalPosition;
            if (targetLocked){
                pos2.Y += 10; // sprite a bit higher then origin point so lower it.
            } else {
                pos2.Y -= 30; // boost even more on a not locked target.
            }
            Vector3 pos3 = playercam.ProjectPosition(pos2, 50);
            ShootBullet(pos3);
        }


        if (@event is InputEventMouseMotion eventy)
        {
            PlayerCamBase.Rotation += new Vector3(-eventy.Relative.Y * 0.01f,-eventy.Relative.X * 0.01f,0);             
        }
     

    }

    public void ShootBullet(Transform3D pos)
    {
        bullet bulletInstance = Bullet.Instantiate() as bullet;
        bulletInstance.Position = rightArm.GlobalPosition;
        bulletInstance.SetDirection(-pos.Basis.Z);
        bulletInstance.SetProps(2, "player");
        GetParent().AddChild(bulletInstance);
        rocket.Play();
        guid.ResetCooldown(true, 1);
    }
    public void ShootBullet(Vector3 targetPosition)
    {
        // meant for left arm
        bullet bulletInstance = Bullet.Instantiate() as bullet;
        bulletInstance.Position = MissileLaunchSpot.GlobalPosition;
        Vector3 direction = (targetPosition - MissileLaunchSpot.GlobalPosition).Normalized();
        bulletInstance.SetDirection(direction);
        bulletInstance.SetProps(1, "player");
        GetParent().AddChild(bulletInstance);
        rocket.Play();
        guid.ResetCooldown(false, 3);
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


    public void ScanForEnemies(){
        if (DetArea.HasOverlappingBodies()){
            Godot.Collections.Array<Node3D> enemies = DetArea.GetOverlappingBodies();

            float Distance = 10000.0f; //cutoff
            foreach (Node3D enemy in enemies)
            {
                if (enemy is CharacterBody3D && enemy != this){
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

    private void Die(){
        // start death explosion, trigger below to scene reset rn
        //GetTree().ReloadCurrentScene();
        if (!canMove){return;} // stops a loop from happening. just a band aid to a bigger problem
        AudioServer.SetBusVolumeDb(0, -40f);
        deathExplosion.Emitting = true;
        canMove = false;
        deathanim.Play("death");
    }

    private void _on_button_pressed(){
        GetTree().ReloadCurrentScene();
    }


}
