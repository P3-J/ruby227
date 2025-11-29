using Godot;
using System;

public partial class CamBase : Node3D
{
    
    [Export] Camera3D playercam;
    [Export] RayCast3D pCameraRay;

    Tween fovTween;

    int ogLen;

    public override void _Ready()
    {
        base._Ready();
        ogLen = 75;
    }

    public override void _Process(double delta)
    {

        pCameraRay.TargetPosition = playercam.Position - new Vector3(0, 2, 0);

        if (!pCameraRay.IsColliding()) { 
            TweenFov(75);
            return ;
            
        }
        
        TweenFov(30);

    }


    private async void TweenFov(float targetFov, float duration = 0.3f)
    {

        if (fovTween != null && fovTween.IsRunning())
            fovTween.Kill();

        fovTween = GetTree().CreateTween();
        fovTween.TweenProperty(playercam, "fov", targetFov, duration)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.Out);
    }



}   
