using Godot;
using System;

public partial class explosion : GpuParticles3D
{
	public void _on_finished(){
	   SceneTreeTimer tr = GetTree().CreateTimer(1.0);
	   tr.Timeout += QueueFree;
	}
	
}
