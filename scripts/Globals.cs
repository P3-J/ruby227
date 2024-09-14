using Godot;
using System;

public partial class Globals : Node
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

    public override void _Input(InputEvent @event)
    {
		// all mighty QOL
        if (Input.IsActionJustReleased("fullscreen")){
			if (DisplayServer.WindowGetMode() == DisplayServer.WindowMode.Windowed){
				DisplayServer.WindowSetMode(DisplayServer.WindowMode.ExclusiveFullscreen);
				return;
			}
			DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
		}
    }
}
