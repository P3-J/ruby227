using Godot;
using Godot.Collections;
using System.Collections.Generic;
using System.Linq;

public partial class enemy : CharacterBody3D
{
    public Godot.Collections.Array<Marker3D> patrolPoints;

    List<Vector3> patrolPointsPos = [];
    float PlayerDistance = 999;
    int currentPatrolStep = 0;
    bool hasSeenPlayer = false;

    // ── Wander / movement config ────────────────────────────────────────────
    [Export] public float WanderRadius = 180f;
    [Export] public float WanderMinDist = 50f;
    [Export] public float StopDistance = 40f;
    [Export] public float Accel = 4f;

    private Vector3 _wanderTarget;
    private readonly RandomNumberGenerator _rng = new();

    // ── State machine ────────────────────────────────────────────────────────
    private void StateMachine(double delta)
    {
        switch (cType)
        {
            case EnemyTypes.SHOOTER: _shooterLoop(delta); break;
            case EnemyTypes.BOMBER: _bomberLoop(delta); break;
        }
    }

    // ── Setup ────────────────────────────────────────────────────────────────
    private void SetupProps()
    {
        _rng.Randomize(); // only once, here at setup

        switch (cType)
        {
            case EnemyTypes.SHOOTER: Speed = 30f; break;
            case EnemyTypes.BOMBER: Speed = 30f; break;
        }

        if (patrolPoints == null) return;

        foreach (Marker3D p in patrolPoints)
        {
            Vector3 point = NavigationServer3D.RegionGetClosestPoint(
                navregion.GetRid(), p.GlobalPosition);
            patrolPointsPos.Add(point);
        }
    }

    // ── Line-of-sight ────────────────────────────────────────────────────────
    private void LosCollsionChecks()
    {
        if (target && GlobalPosition.DistanceTo(Player.GlobalPosition) > 0.01f)
            los.LookAt(Player.GlobalPosition, Vector3.Up);

        PlayerDistance = 999f;
        Node collider = null;

        if (los.IsColliding())
            collider = (Node)los.GetCollider();

        if (collider is player)
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

    // ── Retarget timer ───────────────────────────────────────────────────────
    private void _on_retarget_timeout()
    {
        if (cState == EnemyStates.AFK) return;

        if (cState == EnemyStates.PATROL)
        {
            // Navigate to the current patrol waypoint
            SetTargetPos(patrolPointsPos[currentPatrolStep]);
        }
        else if (cState == EnemyStates.HUNTING)
        {
            // Pick a random wander point near the player and navigate there
            PickNewWanderPoint();
            SetTargetPos(_wanderTarget);
        }

        // Advance the nav agent one step
        if (PlayerDistance > 50f)
        {
            last = next;
            next = navagent.GetNextPathPosition();
        }

        retargetTimer.WaitTime = _rng.RandfRange(0.4f, 0.8f); // slight variance keeps groups desync'd
        retargetTimer.Start();
    }

    private void OnNavigationAgentTargetReached()
    {
        if (cState == EnemyStates.AFK) return;

        RaisePatrolPointStep();

        retargetTimer.Stop();
        _on_retarget_timeout();
    }

    // ── Shooter loop ─────────────────────────────────────────────────────────
    private void _shooterLoop(double delta)
    {
        switch (cState)
        {
            case EnemyStates.SHOOTING:
                canMove = false;
                RotateBodyTowards(Player.GlobalPosition, "body", delta);
                RotateBodyTowards(next, "legs", delta);
                if (hasAggro) TryToShoot();

                // Return to hunting if player moves out of shoot range
                if (!CheckIfCanShoot(PlayerDistance))
                    cState = EnemyStates.HUNTING;
                break;

            case EnemyStates.AFK:
                canMove = false;
                if (patrolPointsPos.Count > 0 && !hasSeenPlayer)
                {
                    cState = EnemyStates.PATROL;
                    SetTargetPos(patrolPointsPos[currentPatrolStep]);
                }
                if (hasAggro) cState = EnemyStates.HUNTING;
                break;

            case EnemyStates.HUNTING:
            case EnemyStates.PATROL:
                canMove = true;
                MoveTowardsTarget(delta);

                RotateBodyTowards(Player.GlobalPosition, "body", delta);  // upper always watches player
                RotateBodyTowards(next, "legs", delta);

                if (CheckIfCanShoot(PlayerDistance))
                    cState = EnemyStates.SHOOTING;
                break;
        }
    }

    // ── Bomber loop ──────────────────────────────────────────────────────────
    private void _bomberLoop(double delta)
    {
        switch (cState)
        {
            case EnemyStates.AFK:
                if (hasAggro) cState = EnemyStates.HUNTING;
                break;

            case EnemyStates.HUNTING:
                canMove = true;
                CheckAggroResetTime((float)delta);
                MoveTowardsTarget(delta);

                if (PlayerDistance < 5f)
                    cState = EnemyStates.SHOOTING;
                break;

            case EnemyStates.SHOOTING:
                canMove = false;
                Die();
                break;
        }
    }

    // ── Wander ───────────────────────────────────────────────────────────────
    private void PickNewWanderPoint()
    {
        if (Player == null) return;

        float angle = _rng.RandfRange(0f, Mathf.Tau);
        float radius = _rng.RandfRange(WanderMinDist, WanderRadius);

        Vector3 offset = new(
            Mathf.Cos(angle) * radius,
            0f,
            Mathf.Sin(angle) * radius
        );

        // Snap to navmesh so the agent never targets inside a wall
        Vector3 raw = Player.GlobalPosition + offset;
        _wanderTarget = NavigationServer3D.RegionGetClosestPoint(navregion.GetRid(), raw);
    }

    // ── Movement ─────────────────────────────────────────────────────────────
    private void MoveTowardsTarget(double delta)
    {
        // No waypoint yet — request one
        if (next == Vector3.Zero || last == next)
        {
            _on_retarget_timeout();
            return;
        }

        float dist = GlobalPosition.DistanceTo(next);

        // Arrived at waypoint
        if (dist < StopDistance)
        {
            //GlobalPosition = new Vector3(next.X, GlobalPosition.Y, next.Z);
            velocity.X = 0f;
            velocity.Z = 0f;
            last = next;
            return;
        }

        // Steer toward waypoint with acceleration smoothing
        Vector3 dir = (next - GlobalPosition).Normalized();
        Vector3 targetVel = new(dir.X * Speed, velocity.Y, dir.Z * Speed);
        velocity = velocity.Lerp(targetVel, Accel * (float)delta);
    }

    // ── Patrol step ──────────────────────────────────────────────────────────
    private void RaisePatrolPointStep()
    {
        if (cState != EnemyStates.PATROL || patrolPoints == null) return;

        currentPatrolStep = (currentPatrolStep >= patrolPointsPos.Count - 1)
            ? 0
            : currentPatrolStep + 1;

        GD.Print("patrol step → ", currentPatrolStep);
    }
}
