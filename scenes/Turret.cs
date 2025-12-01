using Godot;
using System;
using System.Security.AccessControl;

public partial class Turret : StaticBody3D
{
    [Export] Area3D detArea;
    [Export] RayCast3D rayc;
    [Export] Node3D rayCManager;
    [Export] MeshInstance3D lazor;
    [Export] MeshInstance3D lazor2;
    [Export] AudioStreamPlayer3D warningAudio;
    [Export] AudioStreamPlayer3D laserAudio;
    [Export] bool IsKill = false;
      
    TurretStates cState = TurretStates.AFK;
    player Player;

    Timer scanTimer;     

    int[] HP = [500];
    int CooldownBetweenShots = 3;      
    enum TurretStates
    {
        AFK,
        SCANNING,
        LOCKEDON,
        STARTFIRE,
        FIRING,
        COOLDOWN ,
        DISABLED,
    }

    public override void _Ready()
    {
        base._Ready();
        scanTimer = new Timer();
        scanTimer.WaitTime = 2f;
        scanTimer.OneShot = true;
        scanTimer.Connect("timeout", new Callable(this,nameof(_on_scantimer_timeout)));
        AddChild(scanTimer);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (IsKill) return;
        
        switch (cState)
        {
            case TurretStates.AFK:
                if (!scanTimer.IsStopped()) break;
                scanTimer.Start();
                break;
            case TurretStates.SCANNING:
                ScanForEnemies();
                TryToLockOn();
                break;
            case TurretStates.LOCKEDON or TurretStates.STARTFIRE:
                StartFiringMylazoor();
                break;
            case TurretStates.FIRING:
                Fire();
                break;
            case TurretStates.COOLDOWN:
                StartCooldown();
                break;
            default:
                break;
        }

    }

    public void GetHit(int dmg){
        HP[0] -= dmg;
        if (HP[0] <= 0){
            Die();
        }
    }

    public void Die()
    {
        CallDeferred("queue_free");
    }


    private void ScanForEnemies(){
        if (cState == TurretStates.DISABLED) return;
        player Playerperm = null;

        if (!detArea.HasOverlappingBodies())
        {
            Player = null;
            return;
        }

        Godot.Collections.Array<Node3D> entities = detArea.GetOverlappingBodies();
        foreach (Node3D entity in entities)
        {
            if (!entity.IsInGroup("player")) continue;
            Playerperm = (player)entity;
            break;
        }

        // bad fix like in p1mine
        if (Playerperm == null) {
            Player = null;
        } else
        {  
            Player ??= Playerperm;
            cState = TurretStates.SCANNING;
        }
    }
    private void TryToLockOn()
    {
        if (Player == null) return;

        rayc.LookAt(Player.GlobalTransform.Origin);

        if (!rayc.IsColliding()) return;

        Node3D collider = (Node3D)rayc.GetCollider();

        if (collider.IsInGroup("player"))
        {
            cState = TurretStates.LOCKEDON;
        }
    }

    public void _on_scantimer_timeout()
    {
        ScanForEnemies();
    }

    private async void StartFiringMylazoor()
    {   

        AdjustLazor();

        if (!warningAudio.Playing) warningAudio.Play();

        if (cState != TurretStates.STARTFIRE)
        {
            cState = TurretStates.STARTFIRE;
            SceneTreeTimer tr = GetTree().CreateTimer(CooldownBetweenShots);     
		    tr.Timeout += () => cState = TurretStates.FIRING;
        }    
    }


    private void Fire()
    {
        cState = TurretStates.DISABLED;
        laserAudio.Play();

        SceneTreeTimer tr = GetTree().CreateTimer(0.5);     
		tr.Timeout += ShootLazor; 
    }

    private void ShootLazor()
    {
        float len = (Player.GlobalTransform.Origin - GlobalTransform.Origin).Length();
        lazor2.Scale = new Vector3(20, len , 20);
        cState = TurretStates.COOLDOWN;
    }

    private void StartCooldown()
    {
        if (cState == TurretStates.DISABLED) return;

        cState = TurretStates.DISABLED;
	    SceneTreeTimer tr = GetTree().CreateTimer(1);
		tr.Timeout += () => cState = TurretStates.AFK;
    }

    private void AdjustLazor()
    {
        
        Vector3 start = GlobalTransform.Origin;
        Vector3 end = Player.GlobalTransform.Origin;

        Vector3 dir = end - start;
        float len = dir.Length();
        Vector3 mid = start + dir * 0.5f;


        Transform3D t = lazor.GlobalTransform;
        t.Origin = mid;

        t = t.LookingAt(end);

        lazor.GlobalTransform = t;
        lazor2.GlobalTransform = t;
     
        lazor.RotateObjectLocal(Vector3.Right, Mathf.DegToRad(90));
        lazor.Scale = new Vector3(0.5f, len , 0.5f);

        lazor2.RotateObjectLocal(Vector3.Right, Mathf.DegToRad(90));
        lazor2.Scale = new Vector3(0.5f, len , 0.5f);
    }

}
