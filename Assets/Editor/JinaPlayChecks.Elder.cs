using UnityEngine;

// The elder struck after the alarm: hit frames, the fall, and a corpse that stays put.
public static partial class JinaPlayChecks
{
    static void ElderDeath(JinaActorVisual elderVisual)
    {
        // Struck after the alarm, sword in hand, with the boss on his right.
        Set(combat.Village,"<Alarmed>k__BackingField",true); Set(combat.Village,"<AlarmAt>k__BackingField",Time.time-5);
        boss.transform.position=new Vector3(PrototypeWorld.Elder.x+1.5f,boss.transform.position.y,0);
        combat.Quest.progress.SacrificeElder(); combat.Quest.ShowElderDeath();
        var body=Picture(elderVisual);
        foreach(float t in new[]{.1f,.3f,.6f,.8f,1f})
        {
            Set(combat.Quest,"<ElderStruckAt>k__BackingField",Time.time-t); elderVisual.Refresh();
            Check(t<.45f ? body.sprite.name.Contains("elder_hit_0"+(t<.2f ? 0 : 1))
                : body.sprite.name.EndsWith(t<ElderVisual.FallLength ? "elder_down" : "elder_corpse"),"Elder hit frames, then the fall");
            Check(t<ElderVisual.FallLength || (body.sprite.name.EndsWith("elder_corpse") && Mathf.Abs(body.transform.eulerAngles.z)<.01f && body.color==Color.gray),"Elder ends as the corpse drawing");
            Capture("elder-fall-"+t.ToString("0.0"),new Vector3(PrototypeWorld.Elder.x,1,-10),2.5f);
        }
        // The corpse stays put: neither the boss walking past nor reading his note turns it.
        bool flipped=body.flipX; var lying=body.transform.position;
        boss.transform.position=new Vector3(PrototypeWorld.Elder.x+(flipped ? 3 : -3),boss.transform.position.y,0);
        combat.player.transform.position=new Vector3(PrototypeWorld.Elder.x+(flipped ? 1.2f : -1.2f),combat.player.transform.position.y,0);
        Set(combat.Quest,"<IsDialogOpen>k__BackingField",true); Set(combat.Quest,"shrineDialog",false); elderVisual.Refresh();
        Check(body.flipX==flipped && body.transform.position==lying,"Corpse does not turn when the note is read or the boss passes");
        Capture("elder-corpse",new Vector3(PrototypeWorld.Elder.x,1,-10),2.5f);
        Set(combat.Quest,"<IsDialogOpen>k__BackingField",false);
    }
}
