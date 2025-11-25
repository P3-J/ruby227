using Godot;
using System;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Threading.Tasks;

public partial class player : CharacterBody3D
{

    [Export] public PackedScene Bullet;
    [Export] AudioStreamPlayer booster;
	[Export] AudioStreamPlayer rocket;
    [Export] AudioStreamPlayer steam;
    [Export] AudioStreamPlayer jumpboostsound;
    [Export] AudioStreamPlayer dropsound;
    [Export] Marker3D leftarm;
    [Export] Marker3D rightarm;
    [Export] Guid guid;
    [Export] Node3D PlayerCamBase;
    [Export] Node3D BodyManager;
    [Export] RayCast3D groundnormal;    
    [Export] Node3D hlevel;
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

    private const float Gravity = -2.8f;
    private const float JumpForce = 45.0f; //55
    private const float MovementSpeed = 15F; //15

    int HP = 400;
    int cHP = 400;
    int cPower = 100;
    int Power = 100;
    bool playDropSound = false;
    bool canSeeEnemy = false;
    bool targetLocked = false;
    bool cameraLocked = false;
    bool canMove = true;

    public override void _Ready()
    {
        shotcooldown = GetNode<Timer>("soundsCooldowns/shotcooldown");
        shotcooldownLeft = GetNode<Timer>("soundsCooldowns/shotcooldownleft");

        MissileTargeter = GetNode<RayCast3D>("targeter");
        playercam = GetNode<Camera3D>("camBase/Camera3D");
        EnemyTimer = GetNode<Timer>("detectionarea/enemytimer");
        DetArea = GetNode<Area3D>("detectionarea");

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
        Vector3 direction = new();
        if (Input.IsActionPressed("up"))
            direction -= Transform.Basis.Z; 
        if (Input.IsActionPressed("down"))
            direction += Transform.Basis.Z; 
        direction = direction.Normalized();
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


    public override void _Process(double delta)
    {
        base._Process(delta);
        PlayerCamBase.GlobalPosition = this.GlobalPosition;

        if (CurrentTarget != null && IsInstanceValid(CurrentTarget)) {
            TargeterPosition();
            targetLocked = guid.ReposSquare(CurrentTarget.GlobalTransform.Origin, canSeeEnemy);
            //cameraLocked = true;
        } else {
            CurrentTarget = null;
            targetLocked = false;
            cameraLocked = false;
            guid.ResetTargetingSquare();
        }

        HandleTurning();
        HandleCameraTurning(); // includes the left launcher rotation currently, should be based on targeter, doesnt have to be tho
        GroundNormalRotate();

    }

    private void GroundNormalRotate()
    {
        bool onground = groundnormal.IsColliding();

        Vector3 normal = onground ? groundnormal.GetCollisionNormal() : Vector3.Up;

        if (onground)
        {
            float angle = Mathf.RadToDeg(Mathf.Acos(normal.Dot(Vector3.Up)));
            if (angle > 25)
            {
                float t = (angle - 25) / angle;
                normal = normal.Slerp(Vector3.Up, t);
            }
        }

        Vector3 forward = hlevel.GlobalTransform.Basis.Z.Slide(normal).Normalized();
        Vector3 r = normal.Cross(forward).Normalized();

        Basis basis = new Basis(r, normal, forward).Orthonormalized();
        Basis current = hlevel.GlobalTransform.Basis.Orthonormalized();
        Basis smooth = current.Slerp(basis, (float)GetProcessDeltaTime() * 10f);
        hlevel.GlobalTransform = new Transform3D(smooth, hlevel.GlobalTransform.Origin);

    }

    public void HandleTurning(){
        float rotationInput = 0f;
        if (Input.IsActionPressed("left")){
            rotationInput += 0.01f;
            PlaySteamAudioIfCan();
        }
        if (Input.IsActionPressed("right")){
            rotationInput -= 0.01f;
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

        if (cameraLocked && CurrentTarget != null)
        {
           PlayerCamBase.LookAt(CurrentTarget.GlobalPosition);
        }

        RotateMechBodyWithCamera();          
    }

    public override void _Input(InputEvent @event)
    {
        if (Input.IsActionJustPressed("shoot") && shotcooldown.IsStopped())
        {
            shotcooldown.Start();
            ShootRightArm();
        }
        if (Input.IsActionJustPressed("shootleft") && shotcooldownLeft.IsStopped()) {
            shotcooldownLeft.Start();
            ShootLeftArm();
        }

        if (Input.IsActionJustPressed("lockon") && CurrentTarget != null)
        {
            cameraLocked = !cameraLocked;
        }


        if (@event is InputEventMouseMotion eventy && !cameraLocked)
        {
            PlayerCamBase.Rotation += new Vector3(-eventy.Relative.Y * 0.01f,-eventy.Relative.X * 0.01f,0); 
            RotateMechBodyWithCamera();            
        }
     

    }

    public void RotateMechBodyWithCamera()
    {
        BodyManager.GlobalRotation = new Vector3(0, PlayerCamBase.GlobalTransform.Basis.GetEuler().Y ,0);
    }

    public async Task ShootRightArm()
    {
        foreach (var _ in Enumerable.Range(0,3))
        {
            genRightArm();

            await ToSignal(GetTree().CreateTimer(0.2f), "timeout");
        }

        guid.ResetCooldown(true, 1);
    }
    public void genRightArm()
    {
        bullet bulletInstance = CreateBullet(true);
        bulletInstance.SetProps(1, "player", Velocity, 100, false, bullet.BulletType.FIVEFIVESIX);
        GetParent().AddChild(bulletInstance);
        rocket.Play();
    }

    public void ShootLeftArm()
    {
        bullet bulletInstance = CreateBullet(false);
        bulletInstance.SetProps(5, "player", Velocity, -25, true, bullet.BulletType.EXPLODING);
        GetParent().AddChild(bulletInstance);
        rocket.Play();
        guid.ResetCooldown(false, 3);
    }

    private bullet CreateBullet(bool rarm)
    {
        Vector2 pos2 = guid.tsquareController.GlobalPosition;
        if (targetLocked){
            pos2.Y += 10; // sprite a bit higher then origin point so lower it.
        }  else {
            pos2.Y += 30; // boost even more on a not locked target.
        } 
        Vector3 targetPosition = playercam.ProjectPosition(pos2, 50);

        bullet bulletInstance = Bullet.Instantiate() as bullet;

        Vector3 armPos = rarm ? rightarm.GlobalPosition : leftarm.GlobalPosition;

        bulletInstance.Position = armPos;
        Vector3 direction = (targetPosition - armPos).Normalized();
        bulletInstance.SetDirection(direction);
        return bulletInstance;
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

                        if (CurrentTarget != enemy)
                        {
                            dropsound.Play();
                        }
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
