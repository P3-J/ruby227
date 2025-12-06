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

    Vector2 screenSize;
     

    Vector2 aimspotStartSpot;
    public override void _Ready()
    {
        base._Ready();
        aimspotStartSpot = tsquareController.Position;
        screenSize = GetViewport().GetVisibleRect().Size;

    }

    public bool ReposSquare(Vector3 globaltransform, bool canseeenemy){
        Vector2 screenpos = playerCam.UnprojectPosition(globaltransform);   
        screenpos.Y += 0; //15; // OFFSET so sprite is centered.
        /// text next to it indicating distance / else Zero 
        /// do anti of unproject - project position into world to aim
        /// 
        /// 
        float minX = screenSize.X * 0.32f;
        float maxX = screenSize.X * 0.68f;
        float minY = screenSize.Y * 0.32f;
        float maxY = screenSize.Y * 0.68f;

        bool insideBox =
            screenpos.X >= minX &&
            screenpos.X <= maxX &&
            screenpos.Y >= minY &&
            screenpos.Y <= maxY;

        if (!playerCam.IsPositionBehind(globaltransform) 
            && playerCam.IsPositionInFrustum(globaltransform) 
            && insideBox){

            tsquareController.Position =
                tsquareController.Position.MoveToward(screenpos, 5f);

            tsSquare2.Visible = true;
            return true;
        } else {
            ResetTargetingSquare();
            tsSquare2.Visible = false;
            return false;
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
        tween.Finished += () => tween.Dispose();
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
        tween.Finished += () => tween.Dispose();
   }




}
