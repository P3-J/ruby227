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
    [Export] AnimationPlayer rgunshoot;
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
    Node3D CurrentTarget = null;

    private const float Gravity = -6.8f;
    private const float JumpForce = 5.0f; //55
    private float MovementSpeed = 35F; //15
    private const float BaseMovementSpeed = 15F;

    float MaxScanDistance = 300.0f; //cutoff

    int HP = 400;
    int cHP = 400;
    int cPower = 100;
    int Power = 100;
    bool playDropSound = false;
    bool canSeeEnemy = false;
    bool targetLocked = false;
    bool cameraLocked = false;
    bool enemyinview = false;
    bool canMove = true;
    bool jumping = false;
    float jumpAirTime = 0.3f;
    Vector3 BoostDir = Vector3.Zero;


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
        PlayerCamBase.GlobalPosition = this.GlobalPosition;
        Vector3 fakeVelo = Velocity;
        Vector3 direction = new();
        direction = GetInput(direction);

      

        bool onFloor = IsOnFloor();
        fakeVelo.X = direction.X * MovementSpeed;
        fakeVelo.Z = direction.Z * MovementSpeed;

        if (direction == Vector3.Zero && BoostDir == Vector3.Zero)
        {   
            if (!onFloor)
            {
               fakeVelo = Velocity.MoveToward(Vector3.Zero, 0.5f); 
            } else
            {
                fakeVelo = Velocity.MoveToward(Vector3.Zero, 1f);
            }
        }
        
        if (jumping) fakeVelo.Y += JumpForce; 
        
        if (!IsOnFloor())
        {
            fakeVelo.Y += Gravity;
        } else
        {
            fakeVelo.Y = 0;
        }

        if (BoostDir != Vector3.Zero)
        {
            fakeVelo = BoostDir * 200;
        }


        Velocity = fakeVelo;
        MoveAndSlide();

    }


    public override void _Process(double delta)
    {
        base._Process(delta);

        HandleCameraTurning();
        GroundNormalRotate();

        bool canSee = false;

        if (CurrentTarget != null && IsInstanceValid(CurrentTarget)) {
            canSee = guid.ReposSquare(CurrentTarget.GlobalTransform.Origin, true);
            TargeterPosition();
        } 

        if (!canSee){
            CurrentTarget = null;
            targetLocked = false;
            cameraLocked = false;
            guid.ResetTargetingSquare();
                   
        }

    }

    private Vector3 GetInput(Vector3 dir)
    {
        if (Input.IsActionPressed("up"))
        {
            dir -= Transform.Basis.Z; 
            if (MovementSpeed < 45 ) MovementSpeed += 0.5f; 
        }
        if (Input.IsActionPressed("down"))
        {
            dir += Transform.Basis.Z; 
        }

        if (Input.IsActionJustPressed("jump") && IsOnFloor() && !jumping)
        {
            jumping = true;
            jumpboostsound.Play();
            SceneTreeTimer tr = GetTree().CreateTimer(jumpAirTime);
		    tr.Timeout += DisableJump;
        }

        if (Input.IsActionPressed("left")){
            RotateY(0.04f);
            PlaySteamAudioIfCan();
        }
        if (Input.IsActionPressed("right")){
            RotateY(-0.04f);
            PlaySteamAudioIfCan();
        }

        if (dir != Vector3.Zero){
            if (!booster.Playing) booster.Play();
            if (!IsOnFloor()) dir = Vector3.Zero;
        } else {
            MovementSpeed = 10f;
            booster.Stop();
        }
 
        return dir.Normalized();

    }

    private void DisableJump()
    {
        jumping = false;
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
                float t = (angle - 35) / angle;
                normal = normal.Slerp(Vector3.Up, t);
            }
        }

        Vector3 forward = this.GlobalTransform.Basis.Z.Slide(normal).Normalized();
        Vector3 r = normal.Cross(forward).Normalized();

        Basis basis = new Basis(r, normal, forward).Orthonormalized();
        Basis current = hlevel.GlobalTransform.Basis.Orthonormalized();
        Basis smooth = current.Slerp(basis, (float)GetProcessDeltaTime() * 10f);
        hlevel.GlobalTransform = new Transform3D(smooth, hlevel.GlobalTransform.Origin);

    }

 
    public void InBoost(Vector3 dir)
    {
        if (BoostDir != Vector3.Zero) return;
        BoostDir = dir;
		GetTree().CreateTimer(1.5).Timeout += BoostDirReset;
    }

    private void BoostDirReset()
    {
        BoostDir = Vector3.Zero;
    }

    public void GetHit(int dmg){
        cHP -= dmg;
        guid.RefreshHud(cHP);
        GD.Print("took dmg :", dmg);
        // check for megadeth
        if (cHP <= 0){
            Die();
        }
    }

    public void HandleCameraTurning(){

        if (cameraLocked && CurrentTarget != null && IsInstanceValid(CurrentTarget))
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
            _ = ShootRightArm();
        }
        if (Input.IsActionJustPressed("shootleft") && shotcooldownLeft.IsStopped()) {
            shotcooldownLeft.Start();
            ShootLeftArm();
        }

        if (Input.IsActionJustPressed("lockon") && CurrentTarget != null)
        {
            GD.Print(CurrentTarget);
            //cameraLocked = !cameraLocked;
        }


        if (!Input.IsActionPressed("left") && !Input.IsActionPressed("right")){
            steam.Stop();
        }
        


        if (@event is InputEventMouseMotion eventy && !cameraLocked)
        {
            PlayerCamBase.Rotation += new Vector3(-eventy.Relative.Y * 0.01f,-eventy.Relative.X * 0.01f,0); 
            
            float pitch = PlayerCamBase.Rotation.X;
            pitch = Mathf.Clamp(pitch, Mathf.DegToRad(-80f), Mathf.DegToRad(80f));
            PlayerCamBase.Rotation = new Vector3(
                pitch,
                PlayerCamBase.Rotation.Y,
                0
            );

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

            await ToSignal(GetTree().CreateTimer(0.4f), "timeout");
        }

        guid.ResetCooldown(true, 1);
    }
    public void genRightArm()
    {
        bullet bulletInstance = CreateBullet(true);

        Vector3 bonusVelo = Vector3.Zero;
        if (CurrentTarget != null) bonusVelo = GetTargetVelo();

        bulletInstance.SetProps(1, "player", bonusVelo * 0.7f, 50, true, bullet.BulletType.FIVEFIVESIX);
        GetParent().AddChild(bulletInstance);
        rocket.Play();
        rgunshoot.Play("firegun");
    }

    public void ShootLeftArm()
    {
        bullet bulletInstance = CreateBullet(false);
        bulletInstance.SetProps(5, "player", Velocity, -25, true, bullet.BulletType.EXPLODING);
        GetParent().AddChild(bulletInstance);
        rocket.Play();
        guid.ResetCooldown(false, 3);
    }


    private Vector3 GetTargetVelo()
    {
        Vector3 velo = (Vector3)CurrentTarget.Get("GetVelo");
        return velo;
    }

    private bullet CreateBullet(bool rarm)
    {
        Vector2 pos2 = guid.tsquareController.GlobalPosition;

        Vector3 targetPosition = playercam.ProjectPosition(pos2, 100);

        if (CurrentTarget != null && canSeeEnemy)
        {
            targetPosition = CurrentTarget.GlobalPosition;
        }

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
        //if (!IsInstanceValid(CurrentTarget)) return;

        MissileTargeter.LookAt(CurrentTarget.GlobalPosition);
        if (!MissileTargeter.IsColliding()){
            canSeeEnemy = false;
            cameraLocked = false;
            
            return;
        } 

        
        
        Node3D collider = (Node3D)MissileTargeter.GetCollider();
        //GD.Print(collider.Name);
        if (collider.IsInGroup("enemy"))
        {
            canSeeEnemy = true;
        }
    }


    public void ScanForEnemies(){
        if (!DetArea.HasOverlappingBodies()) return;

        // change based on length please

        Godot.Collections.Array<Node3D> enemies = DetArea.GetOverlappingBodies();
        if (cameraLocked) return;

        foreach (Node3D enemy in enemies)
        {
            if (!enemy.IsInGroup("enemy") || enemy == this) continue;

            float targetDistance = enemy.GlobalPosition.DistanceTo(GlobalPosition);


            if (enemy == CurrentTarget && targetDistance > MaxScanDistance)
            {
                CurrentTarget = null;
                continue;
            }

            if (targetDistance > MaxScanDistance) continue;

            bool canSee = guid.TargetInView(enemy.GlobalPosition);
            if (!canSee) continue;

            if (CurrentTarget != null)
            {
                float cDis = CurrentTarget.GlobalPosition.DistanceTo(GlobalPosition);
                if (cDis > targetDistance) CurrentTarget = enemy;
                continue;
            }

            CurrentTarget = enemy;
            break;
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
        booster.Stop();
        steam.Stop();

        AudioServer.SetBusVolumeDb(0, -40f);
        deathExplosion.Emitting = true;
        canMove = false;
        deathanim.Play("death");
    }

    private void _on_button_pressed(){
        GetTree().ReloadCurrentScene();
    }


}
