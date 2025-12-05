using Godot;
using System;

public partial class Selfdestruct : Area3D
{
    
    [Export] float expRadius;
    [Export] CollisionShape3D colShape;
    [Export] GpuParticles3D expl;
    [Export] AudioStreamPlayer3D explosionSound;




    public override void _Ready()
    {
        base._Ready();
        colShape.Shape.Set("radius", expRadius);


        GD.Print(colShape.Shape.Get("radius"));
    }

    public void Explode(int dmg, string callerName)
    {
        expl.Emitting = !expl.Emitting;
        explosionSound.Play();

        if (!HasOverlappingBodies()) return;

        // change based on length please

        Godot.Collections.Array<Node3D> entities = GetOverlappingBodies();

        foreach (Node3D entity in entities)
        {
            if ((entity.IsInGroup("enemy") || entity.IsInGroup("player")) && entity.Name != callerName)
            {
                entity.Call("GetHit", dmg);
            }
        }
    }



}
