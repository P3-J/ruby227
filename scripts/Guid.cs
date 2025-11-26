using Godot;
using System;
using System.Threading;

public partial class Guid : Control
{
    [Export] public Node2D tsquareController;
    [Export] Camera3D playerCam;
    [Export] ProgressBar hpBar;
    [Export] ProgressBar leftbar;
    [Export] ProgressBar rightbar;
    [Export] RichTextLabel powerleveltext;
    [Export] Sprite2D tsSquare2;
     

    Vector2 aimspotStartSpot;
    public override void _Ready()
    {
        base._Ready();
        aimspotStartSpot = tsquareController.Position;

    }

    public void ReposSquare(Vector3 globaltransform, bool canseeenemy){
        Vector2 screenpos = playerCam.UnprojectPosition(globaltransform);   
        screenpos.Y += 15; // OFFSET so sprite is centered.
        /// text next to it indicating distance / else Zero 
        /// do anti of unproject - project position into world to aim

        if (!playerCam.IsPositionBehind(globaltransform) && playerCam.IsPositionInFrustum(globaltransform) && canseeenemy){
            tsquareController.Position = tsquareController.Position.MoveToward(screenpos, 5f);
            tsSquare2.Visible = tsquareController.Position == screenpos;
        } else {
            ResetTargetingSquare();
        }
    }

    public void ResetTargetingSquare()
    {
        // reset targeting square to center of screen
        tsquareController.Position = tsquareController.Position.MoveToward(aimspotStartSpot, 5f);
    }

    public bool TargetInView(Vector3 targetPos)
    {
        return !playerCam.IsPositionBehind(targetPos) && playerCam.IsPositionInFrustum(targetPos);
    }

    public void RefreshHud(int cHP)
    {
        Tween tween = GetTree().CreateTween();
        tween.TweenProperty(hpBar, "value", cHP, 0.5);
    }

    public void SetupHud(int HP)
    {
        hpBar.MaxValue = HP;
    }
    
   public void ResetCooldown(bool right, int cdr){
        ProgressBar bar = right ? rightbar : leftbar;
        bar.Value = 0;
        Tween tween = GetTree().CreateTween();
        tween.TweenProperty(bar, "value", 100, cdr);
   }




}
