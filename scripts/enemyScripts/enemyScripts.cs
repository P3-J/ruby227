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
		}

		hasAggro = PlayerDistance < AggroDistance;
    }


    private void _shooterLoop(double delta)
    {
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
                GD.Print("hunting");
                canMove = true;
                CheckAggroResetTime((float)delta);
                next = navagent.GetNextPathPosition();
                RotateBodyTowardsPlayer(false, next);
                Vector3 dir = GlobalPosition.DirectionTo(next);
                if (CheckIfCanShoot(PlayerDistance)) cState = EnemyStates.SHOOTING;
                if (next != Vector3.Zero)
                {
                    velocity.X = dir.X * Speed;
                    velocity.Z = dir.Z * Speed;
                }
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
				next = navagent.GetNextPathPosition();
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
