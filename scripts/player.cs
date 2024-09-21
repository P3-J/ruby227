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
    Timer shotcooldownLeft;
    GpuParticles3D leftshoulder;
    GpuParticles3D rightshoulder;
    RayCast3D MissileTargeter;
    GpuParticles3D RightSteam;
    GpuParticles3D LeftSteam;
    Node2D tsquareController;
    Camera3D playercam;
    Timer EnemyTimer;
    Area3D DetArea;
    AnimationPlayer deathanim;

    GpuParticles3D deathExplosion;
    private const float Gravity = -2.8f;
    private const float JumpForce = 55.0f; //55
    private const float MovementSpeed = 20F; //15

    int HP = 4;
    int cHP = 4;
    bool playDropSound = false;
    bool canSeeEnemy = false;
    bool targetLocked = false;
    bool canMove = true;
    CharacterBody3D CurrentTarget = null;
    Marker3D MissileLaunchSpot;
    Node3D MissileLauncherSwivel;


    public override void _Ready()
    {
        shotcooldown = GetNode<Timer>("shotcooldown");
        shotcooldownLeft = GetNode<Timer>("shotcooldownleft");

        leftshoulder = GetNode<GpuParticles3D>("mech/leftshoulder");
        rightshoulder = GetNode<GpuParticles3D>("mech/rightshoulder");
        MissileTargeter = GetNode<RayCast3D>("targeter");
        tsquareController = GetNode<Node2D>("guid/tsquarecontroller");

        leftbar = GetNode<ProgressBar>("guid/tsquarecontroller/shooterleft/shooterbar");
        rightbar = GetNode<ProgressBar>("guid/tsquarecontroller/shooterright/shooterbar");

        playercam = GetNode<Camera3D>("camBase/Camera3D");
        EnemyTimer = GetNode<Timer>("detectionarea/enemytimer");
        DetArea = GetNode<Area3D>("detectionarea");
        MissileLaunchSpot = GetNode<Marker3D>("mech/missilelauncherswivel/missilelauncher/missilelauncherspot");
        MissileLauncherSwivel = GetNode<Node3D>("mech/missilelauncherswivel");

        RightSteam = GetNode<GpuParticles3D>("mech/rightgas");
        LeftSteam = GetNode<GpuParticles3D>("mech/leftgas");

        deathExplosion = GetNode<GpuParticles3D>("particles/explosion");
        deathanim = GetNode<AnimationPlayer>("guid/deathscreen/anim");

        SetupHud();
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

        fakeVelo.X = direction.X * MovementSpeed;
        fakeVelo.Z = direction.Z * MovementSpeed;

        if (Input.IsActionJustPressed("jump") && IsOnFloor())
        {
            fakeVelo.Y = JumpForce;
            jumpboostsound.Play();
        }

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

    public void HandleTurning(){
        float rotationInput = 0f;
        if (Input.IsActionPressed("left")){
            rotationInput += 0.02f;
            RightSteam.Emitting = true;
            PlaySteamAudioIfCan();
        }
        if (Input.IsActionPressed("right")){
            rotationInput -= 0.02f;
            LeftSteam.Emitting = true;
            PlaySteamAudioIfCan();
        }
        if (Input.IsActionJustReleased("right")) {LeftSteam.Emitting = false;}
        if (Input.IsActionJustReleased("left")) {RightSteam.Emitting = false;} 

        if (!Input.IsActionPressed("left") && !Input.IsActionPressed("right")){
            steam.Stop();
        }
        RotateY(rotationInput);
    }

    public void GetHit(int dmg){
        cHP -= dmg;
        RefreshHud();
        // check for megadeth
        if (cHP <= 0){
            Die();
        }
    }

    public void HandleCameraTurning(){
         if (Input.IsActionPressed("camleft")){
			Vector3 v = PlayerCamBase.RotationDegrees;
			v.Y += 4f;
			PlayerCamBase.RotationDegrees = v;
            MissileLauncherSwivel.RotationDegrees = v;
		}

		 if (Input.IsActionPressed("camright")){
			Vector3 v = PlayerCamBase.RotationDegrees;
			v.Y -= 4f;
			PlayerCamBase.RotationDegrees = v;
            MissileLauncherSwivel.RotationDegrees = v;
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
            Vector2 pos2 = tsquareController.GlobalPosition;

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
        bulletInstance.Call("SetDamage", 2);
        GetParent().AddChild(bulletInstance);
        rocket.Play();
        ResetCooldown(rightbar, 1);
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
        ResetCooldown(leftbar, 2);
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
            tsquareController.Position = tsquareController.Position.MoveToward(screenpos, 5f);
            targetLocked = true;
        } else {
            ResetTargetingSquare();
            targetLocked = false;
        }
    }

    public void ResetTargetingSquare(){
        // reset targeting square to center of screen
        tsquareController.Position = tsquareController.Position.MoveToward(new Vector2(256, 256), 5f);
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

    public void ShoulderParticleController(bool state){
        leftshoulder.Emitting  = state;
        rightshoulder.Emitting = state;
    }

}