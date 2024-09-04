using Godot;
using System;
using System.Diagnostics.Tracing;

public partial class player : CharacterBody3D
{

   
    ProgressBar leftbar;
    ProgressBar rightbar;



   public void ResetCooldown(ProgressBar bar, int cdr){
        bar.Value = 0;
        Tween tween = GetTree().CreateTween();
        tween.TweenProperty(bar, "value", 100, cdr);
   }


}




