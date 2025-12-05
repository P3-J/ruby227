using Godot;
using System;

public partial class P1Mine : StaticBody3D
{
    [Export] Area3D detArea;
   // [Export] RayCast3D rayc;
    [Export] bool IsKill = false;
    [Export] Node3D minenode;
    [Export] Selfdestruct selfD;
    [Export] AudioStreamPlayer3D droneAudio;
      
    mineStates cState = mineStates.AFK;
    player Player;
    Timer scanTimer;

    int[] HP = [3];
    int CooldownBetweenShots = 3;      
    float maxDeployHeight = 100;

    float rot = 0;
    bool firstCaseLoop = true;
    enum mineStates
    {
        AFK,
        SCANNING,
        DEPLOYING,
        FIRING,
        EXPLODE,
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
            case mineStates.AFK:
                if (!scanTimer.IsStopped()) break;
                scanTimer.Start();
                break;
            case mineStates.DEPLOYING:
                PlayDroneSound();
                DeployMine();
                break;
            case mineStates.FIRING:
                PlayDroneSound();
                if (Player.GlobalPosition.DistanceTo(GlobalPosition) < 10f)
                {
                    cState = mineStates.EXPLODE;
                    break;
                }

                GlobalPosition = GlobalPosition.MoveToward(Player.GlobalPosition, 50f * (float)delta);
                minenode.LookAt(Player.GlobalPosition + new Vector3(0, 5, 0));

                if (rot < 180)
                {
                    rot += 0.5f;
                } else
                {
                    rot = 0;
                }

                Basis offset = Basis.FromEuler(new Vector3(rot, Mathf.DegToRad(90), Mathf.DegToRad(90)));
                minenode.Basis = minenode.Basis * offset;
                
                break;
            case mineStates.EXPLODE:
                if (!firstCaseLoop) return;
                droneAudio.Stop();
                firstCaseLoop = false;
                selfD.Explode(5, this.Name);
                SceneTreeTimer tr = GetTree().CreateTimer(1.0);
	            tr.Timeout += () => CallDeferred("queue_free");  
                break;
            default:
                break;
        }   

    }

    private void PlayDroneSound()
    {
        if (droneAudio.Playing) return;
        droneAudio.Play();
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


    public void _on_scantimer_timeout()
    {
        ScanForEnemies();
    }

    private void DeployMine()
    {
        if (firstCaseLoop)
        {
            maxDeployHeight += GlobalPosition.Y;
            firstCaseLoop = false;
        }

        Vector3 dir = Vector3.Zero;
        
        if (GlobalPosition.Y < maxDeployHeight)
        {
            dir = new Vector3(0, 0.5f, 0);
        } else
        {
            cState = mineStates.FIRING;
            firstCaseLoop = true;
        }

        Vector3 newPos = GlobalPosition;
        newPos += dir;
        GlobalPosition = newPos;

    }

    private void ScanForEnemies(){
        if (cState == mineStates.DISABLED) return;

        if (Player != null) return;

        if (!detArea.HasOverlappingBodies())
        {
            Player = null;
            return;
        }


        Godot.Collections.Array<Node3D> entities = detArea.GetOverlappingBodies();
        foreach (Node3D entity in entities)
        {
            if (!entity.IsInGroup("player")) continue;
            Player = (player)entity;
            cState = mineStates.DEPLOYING;
            break;
        }
    }
   
}
