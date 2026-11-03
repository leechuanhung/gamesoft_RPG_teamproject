using UnityEngine;
// The peddler (drawn facing left): runs when fleeing, walks slowly home, otherwise faces his customer or stands as drawn.
public class PeddlerVisual : JinaActorVisual
{
    private Sprite[] runs;

    protected override void Build()
    {
        native=facing=-1;
        runs=JinaArt.Frames("12_Peddler/peddler_run_");
    }
    protected override void Pose(ref Frame f,float dx)
    {
        if(combat==null) return;
        bool moving=Moving(dx);
        float speed=Time.deltaTime>0 ? Mathf.Abs(dx)/Time.deltaTime : 0;
        if(!combat.Paused) walkTime=moving ? walkTime+Time.deltaTime : 0;
        if(moving) f.sprite=runs[(int)(walkTime/(speed>2 ? .09f : .2f))%4];
        else facing=combat.Peddler!=null && combat.Peddler.IsOpen ? Toward(combat.player.transform.position.x) : native;
        // The village fades him in and out at the house door through the placeholder's alpha.
        f.tint.a=source.color.a;
    }
}
