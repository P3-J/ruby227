using Godot;
using Godot.Collections;
using Microsoft.VisualBasic;
using System;

public partial class enemy : CharacterBody3D
{
    float PlayerDistance = 999;

    private void StateMachine(double delta)
    {
        switch (cType)
        {
            case EnemyTypes.SHOOTER:
                _shooterLoop(delta);
                break;
            case EnemyTypes.BOMBER:
                _bomberLoop(delta);
                break;
        }
    }

    private void SetupProps()
    {
        switch (cType)
        {
            case EnemyTypes.SHOOTER:
                Speed = 120f;
                break;
            case EnemyTypes.BOMBER:
                Speed = 30f;
                break;
        }
    }


    private void LosCollsionChecks()
    {
        if (target) los.LookAt(Player.GlobalPosition, Vector3.Up);
		PlayerDistance = 999; // default = out of range
	
		Node Collider = null;
        if (los.IsColliding()) Collider = (Node)los.GetCollider();

        if (Collider is player)
        {
            PlayerDistance = GetPlayerDistance();
            lastSawPlayerSeconds = 0;
            hasVisionOfTarget = true;
        }
        else
        {
            hasVisionOfTarget = false;
        }


		hasAggro = PlayerDistance < AggroDistance;
    }

    private void _on_retarget_timeout(){
		if (cState == EnemyStates.AFK) return;
		SetTargetPos(Player.GlobalPosition);

        if (PlayerDistance > 10)
        {
		    next = navagent.GetNextPathPosition();
        }
		GD.Randomize();
		int randi = GD.RandRange(1, 2);
		retargetTimer.WaitTime = randi;
		retargetTimer.Start();
	}


    private void _shooterLoop(double delta)
    {
        GD.Print(cState);
        switch (cState)
        {
            case EnemyStates.SHOOTING:
                canMove = false;
                RotateBodyTowardsPlayer(true, Vector3.Zero);
                if (hasAggro) TryToShoot(PlayerDistance);
                break;
            case EnemyStates.AFK:
                canMove = false;
                if (hasAggro) cState = EnemyStates.HUNTING;
                break;
            case EnemyStates.HUNTING:
                canMove = true;
                CheckAggroResetTime((float)delta);
                Vector3 dir = GlobalPosition.DirectionTo(next);

                if (CheckIfCanShoot(PlayerDistance))
                {
                    cState = EnemyStates.SHOOTING;
                }


                if (next != Vector3.Zero)
                {
                    velocity.X = dir.X * Speed;
                    velocity.Z = dir.Z * Speed;
                }
                else
                {
                    // get nearest point, go there if out of region
                    next = NavigationServer3D.RegionGetClosestPoint(navregion.GetRid(), GlobalPosition);
                }
                RotateBodyTowardsPlayer(false, next);
                break;
        }
    }
    
    private void _bomberLoop(double delta)
    {
        switch (cState)
        {
            case EnemyStates.SHOOTING:
				canMove = true;
                Die();
				break;
			case EnemyStates.AFK:
				if (hasAggro) cState = EnemyStates.HUNTING;
				break;
			case EnemyStates.HUNTING:
				CheckAggroResetTime((float)delta);
				RotateBodyTowardsPlayer(false, next);
                Vector3 dir = GlobalPosition.DirectionTo(next);
                if (PlayerDistance < 5) cState = EnemyStates.SHOOTING;
				if (next != Vector3.Zero)
				{
					velocity.X = dir.X * Speed;
					velocity.Z = dir.Z * Speed;
				} 
				break;
        }
    }

}
