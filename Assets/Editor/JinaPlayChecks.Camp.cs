using UnityEngine;

// A felled ghost plays its death motion, stops counting and acting, then comes back on restart.
public static partial class JinaPlayChecks
{
    static void GhostDeath()
    {
        var mob=combat.Camp.mobs[0]; var visual=mob.visual;
        // Its attack, shared by the water ghosts: reared back in the windup, then the swipe and the follow-through,
        // every frame on the idle's canvas at the idle's scale.
        visual.Winding=true; visual.AimX=mob.art.transform.position.x+5; visual.Refresh();
        Check(Picture(visual).sprite.name.EndsWith("ghost_attack_00"),"Ghost rears back in the windup");
        Check(!Picture(visual).flipX,"It faces where it aims");
        float idleScale=JinaArt.Load(JinaArt.Ghost).pixelsPerUnit;
        foreach(var part in new[]{"00","01","02"}) Check(Mathf.Abs(JinaArt.Load("07_Ghost/ghost_attack_"+part).pixelsPerUnit-idleScale)<.01f,"Attack frame keeps the ghost's size");
        Capture("ghost-attack-0",new Vector3(mob.art.transform.position.x,1,-10),2.5f);
        visual.Strike(); visual.Refresh();
        Check(Picture(visual).sprite.name.EndsWith("ghost_attack_01"),"Then it swipes");
        Capture("ghost-attack-1",new Vector3(mob.art.transform.position.x,1,-10),2.5f);
        visual.StruckAt=Time.time-GhostVisual.SwipeLength-.01f; visual.Refresh();
        Check(Picture(visual).sprite.name.EndsWith("ghost_attack_02"),"And follows through");
        Capture("ghost-attack-2",new Vector3(mob.art.transform.position.x,1,-10),2.5f);
        visual.StruckAt=Time.time-GhostVisual.StrikeLength-.01f; visual.Refresh();
        Check(Picture(visual).sprite.name==JinaArt.Ghost,"Then it floats as before");
        var waterGhost=combat.River.mobs[0].visual; waterGhost.Winding=true; waterGhost.AimX=0; waterGhost.Refresh();
        Check(Picture(waterGhost).sprite.name.EndsWith("ghost_attack_00") && waterGhost.tint!=Color.white,"The water ghost uses the same motion in its own colour");
        Capture("water-ghost-attack",new Vector3(combat.River.mobs[0].art.transform.position.x,1,-10),2.5f);
        waterGhost.Winding=false; waterGhost.Refresh();
        combat.Camp.Hit(mob,PrototypeCamp.GhostHealth);
        Check(mob.hp==0 && combat.Camp.Remaining==2 && mob.art.enabled && visual.DownAt>=0,"Felled ghost starts its death motion");
        float[] times={.05f,.3f,.6f};
        for(int i=0;i<3;i++)
        {
            Set(visual,"<DownAt>k__BackingField",Time.time-times[i]); visual.Refresh();
            Check(Picture(visual).sprite.name.EndsWith("ghost_death_0"+i),"Ghost death frame "+i);
            Capture("ghost-death-"+i,new Vector3(mob.art.transform.position.x,1,-10),2.5f);
        }
        Set(visual,"<DownAt>k__BackingField",Time.time-GhostVisual.DeathLength-.01f); visual.Refresh();
        Check(!Picture(visual).enabled,"Ghost vanishes after its death motion");
        combat.Camp.ResetCamp(); visual.Refresh();
        Check(mob.hp==PrototypeCamp.GhostHealth && visual.DownAt<0 && Picture(visual).enabled && Picture(visual).sprite.name==JinaArt.Ghost,"Restart brings the ghost back");
    }
}
