using Godot;
using System;

public partial class Boostpadplat : Node3D
{

    [Export] Marker3D boostdir;
    [Export] Marker3D boostdir2;


    private void _on_scanner_body_entered(Node3D body)
    {
        
        if (body.IsInGroup("player") && body is player e)
        {
            
            e.InBoost((boostdir.GlobalPosition - boostdir2.GlobalPosition).Normalized());

        }

    }

}
