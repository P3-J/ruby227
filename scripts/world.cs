using Godot;
using System;

public partial class world : Node3D
{

	[Export] Node3D PlayerCamBase;
    public override void _Input(InputEvent @event)
    {
    }

    public override void _PhysicsProcess(double delta)
    {
         if (Input.IsActionPressed("camleft")){
			Vector3 v = PlayerCamBase.RotationDegrees;
			v.Y += 3f;
			PlayerCamBase.RotationDegrees = v;
		}

		 if (Input.IsActionPressed("camright")){
			Vector3 v = PlayerCamBase.RotationDegrees;
			v.Y -= 3f;
			PlayerCamBase.RotationDegrees = v;
		}
    }
}
