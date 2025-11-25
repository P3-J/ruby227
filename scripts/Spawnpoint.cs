using Godot;
using Godot.Collections;
using System;

public partial class Spawnpoint : Marker3D
{
    [Export] PackedScene enemyInstance;
    [Export] Array<Marker3D> patrolPoints;

    public override void _Ready()
    {
        base._Ready();
        SpawnEnemy();
    }

    private void RollSpawn()
    {
        return;
    }

    private void SpawnEnemy()
    {
        enemy Enemy = enemyInstance.Instantiate<enemy>();
        SetupEnemy(Enemy);
        AttachToNavLink(Enemy);
    }

    private void SetupEnemy(enemy Enemy)
    {
        GD.Randomize();
		int randi = GD.RandRange(0, 30);
        Enemy.ShootDistance = GD.RandRange(Enemy.ShootDistance, Enemy.ShootDistance + randi);

        Enemy.spawnLocation = new Vector3(GlobalPosition.X, GlobalPosition.Y + 5, GlobalPosition.Z);

        Enemy.patrolPoints = patrolPoints;
    }
    

    private void AttachToNavLink(Node toSpawn)
    {
        //. not a fan of this one
        Node findnavlink = GetTree().CurrentScene.FindChild("NavigationRegion3D");

        if (findnavlink != null)
        {
            findnavlink.CallDeferred("add_child",toSpawn);
        }
    }
}
