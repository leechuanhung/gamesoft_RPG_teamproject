using UnityEngine;

// A felled ghost plays its death motion, stops counting and acting, then comes back on restart.
public static partial class JinaPlayChecks
{
    static void GhostDeath()
    {
        var mob=combat.Camp.mobs[0]; var visual=mob.visual;
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
