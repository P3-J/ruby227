using Godot;
using System;

public partial class Turret : StaticBody3D
{
    
    int[] HP = [5]; // for max hp storage later             


     public void GetHit(int dmg){
        HP[0] -= dmg;
        if (HP[0] <= 0){
            Die();
        }
        GD.Print("wow");
    }

    public void Die()
    {
        CallDeferred("queue_free");
    }



}
