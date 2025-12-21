using Godot;
using Godot.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.Marshalling;

public partial class enemy : CharacterBody3D
{
    public Godot.Collections.Array<Marker3D> patrolPoints;

    List<Vector3> patrolPointsPos = [];
    float PlayerDistance = 999;
    int currentPatrolStep = 0;
    bool hasSeenPlayer = false;

    private void StateMachine(double delta)
    {
        // GD.Print(cState);
        // silent crash when path not found
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
                Speed = 220f;
                break;
            case EnemyTypes.BOMBER:
                Speed = 30f;
                break;
        }

        // collect patrol points
        if (patrolPoints == null) return;


        foreach (Marker3D p in patrolPoints)
        {
            Vector3 point = NavigationServer3D.RegionGetClosestPoint(navregion.GetRid(), p.GlobalPosition);
            patrolPointsPos.Add(point);
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
            hasSeenPlayer = true;
        }
        else
        {
            hasVisionOfTarget = false;
        }


		hasAggro = PlayerDistance < AggroDistance;
    }

    private void _on_retarget_timeout(){
		if (cState == EnemyStates.AFK) return;

        Vector3 targetPos = cState == EnemyStates.PATROL ? patrolPointsPos[currentPatrolStep] : Player.GlobalPosition;
        if (PlayerDistance > 50)
        {
            SetTargetPos(targetPos);
            last = next;
            next = navagent.GetNextPathPosition();
        }
        
        
		GD.Randomize();
		retargetTimer.WaitTime = 0.5f;
		retargetTimer.Start();
	}

    private void OnNavigationAgentTargetReached()
	{
		if (cState == EnemyStates.AFK) return;
        RaisePatrolPointStep();
        
        retargetTimer.Stop();
        _on_retarget_timeout();
        
    }

    private void _shooterLoop(double delta)
    {
        switch (cState)
        {
            case EnemyStates.SHOOTING:
                canMove = false;
                RotateBodyTowards(Player.GlobalPosition, "body", delta);
                if (hasAggro) TryToShoot();
                break;

            case EnemyStates.AFK:
                canMove = false;
                if (patrolPointsPos.Count > 0 && !hasSeenPlayer) {
                    cState = EnemyStates.PATROL; // costly?
                    SetTargetPos(patrolPointsPos[currentPatrolStep]);
                }
                if (hasAggro) cState = EnemyStates.HUNTING;
                break;

            case EnemyStates.HUNTING or EnemyStates.PATROL:
                canMove = true;
                //CheckAggroResetTime((float)delta);        
                MoveTowardsTarget();

                //RotateBodyTowards(Velocity.Normalized(), "legs", delta);

                if (CheckIfCanShoot(PlayerDistance)) cState = EnemyStates.SHOOTING;
                break;
        }
    }
    
    private void _bomberLoop(double delta)
    {
        return;
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
				//RotateBodyTowards(next, "legs");
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

    private void MoveTowardsTarget()
    {
    

        if (last == next)
        {
            return;
        }
            GD.Print(next);
        Vector3 dir = GlobalPosition.DirectionTo(next).Normalized();

        velocity.X = dir.X * Speed;
        velocity.Z = dir.Z * Speed;

      /*   if (Velocity == Vector3.Zero)
        {
            next = NavigationServer3D.RegionGetClosestPoint(navregion.GetRid(), GlobalPosition);
        } */
        
    }

    private void RaisePatrolPointStep()
    {
        if (cState != EnemyStates.PATROL) return;
     
        
        if (patrolPointsPos.Count - 1 <= currentPatrolStep)
        {
            currentPatrolStep = 0;
            GD.Print("raised step", currentPatrolStep);
            return;
        }
        currentPatrolStep += 1;
           GD.Print("raised step", currentPatrolStep);

    }

}
