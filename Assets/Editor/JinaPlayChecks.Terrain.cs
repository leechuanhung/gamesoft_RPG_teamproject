using UnityEngine;

// Backdrops and ledges, aimed arrows, flying ghosts, and the boss's charge and leap.
public static partial class JinaPlayChecks
{
    static void Terrain()
    {
        foreach (var area in new[]{"stele","camp","village","forest"})
            Check(GameObject.Find("원경 "+area)!=null && GameObject.Find("근경 "+area)!=null,"Backdrop layers for "+area);
        Physics2D.SyncTransforms();
        foreach (var set in new[]{PrototypeWorld.CampLedges,PrototypeWorld.ForestLedges})
        {
            foreach (var ledge in set)
            {
                var hit=Physics2D.Raycast(new Vector2(ledge.x,ledge.y+1),Vector2.down,2,1<<8);
                Check(hit.collider!=null && hit.collider.usedByEffector && Mathf.Abs(hit.point.y-ledge.y)<.05f,"Ledge top at "+ledge);
                // from the ground (y -0.5) or from a lower ledge
                Check(ledge.y+.5f<1.9f || System.Array.Exists(set,o=>o.y<ledge.y && ledge.y-o.y<1.9f),"Each ledge can be reached by a jump");
            }
        }
        Capture("terrain-camp",new Vector3(-24,2,-10),5.5f);
        Capture("terrain-forest",new Vector3(44,2,-10),5.5f);
        Capture("backdrop-village",new Vector3(6,2,-10),5.5f);
        Capture("backdrop-stele",new Vector3(-55,2,-10),5.5f);
    }

    static void AimedArrows()
    {
        foreach (string prefix in new[]{"03_Player_Attack_Bow/bow_up_","03_Player_Attack_Bow/bow_down_"}) Check(JinaArt.Frames(prefix).Length==4,"Aimed bow frames");
        var visual=combat.player.GetComponent<SeonbiVisual>(); var body=Field<SpriteRenderer>(visual,"body");
        Set(combat,"<UsingBow>k__BackingField",true);
        foreach (int aim in new[]{1,-1})
        {
            visual.Attack(true,1,aim); Set(visual,"attackTime",.31f); visual.SendMessage("LateUpdate");
            Check(body.sprite.name.Contains(aim>0 ? "bow_up_" : "bow_down_"),"Aimed shot frame");
            Set(combat,"pendingBow",true); Set(combat,"pendingAim",aim); Set(combat,"attackFacing",1); Set(combat,"playerImpactAt",Time.time);
            Call(combat);
            var shot=GameObject.Find("Arrow").GetComponent<SpriteRenderer>();
            var dir=PrototypeCombat.ArrowDirection(aim,1);
            Check(Mathf.Abs(shot.transform.eulerAngles.z-(aim>0 ? 30 : 330))<.1f && dir.y*aim>0,"Arrow turns and flies "+(aim>0 ? "up" : "down"));
            Capture("bow-aim-"+(aim>0 ? "up" : "down"),new Vector3(combat.player.transform.position.x+1,1.5f,-10),2.5f);
            shot.gameObject.name="Checked Arrow";
            typeof(PrototypeCombat).GetMethod("ClearArrows",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(combat,null);
        }
        Set(combat,"<UsingBow>k__BackingField",false);
        Check(PrototypeCombat.ArrowLaunch(1).y>PrototypeCombat.ArrowLaunch(0).y && PrototypeCombat.ArrowLaunch(-1).y<PrototypeCombat.ArrowLaunch(0).y,"Aimed arrows leave the bow higher or lower");
        // A diagonal arrow meets a target above, and passes up through a ledge.
        var above=new Bounds(new Vector3(4,2.3f,0),new Vector3(1,1.4f,1));
        Check(!float.IsInfinity(PrototypeCombat.ArrowHitDistance(above,Vector2.zero,PrototypeCombat.ArrowDirection(1,1),10)),"Upward arrow hits a flier");
        Check(float.IsInfinity(PrototypeCombat.ArrowHitDistance(above,Vector2.zero,PrototypeCombat.ArrowDirection(0,1),10)),"A level arrow passes under it");
        var ledge=PrototypeWorld.CampLedges[0];
        var wall=typeof(PrototypeCombat).GetMethod("Wall",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static)
            .Invoke(null,new object[]{new Vector2(ledge.x,ledge.y-1),Vector2.up,2f});
        Check(((RaycastHit2D)wall).collider==null,"Arrows fly through ledges");
    }

    static void FlyingGhosts()
    {
        var camp=combat.Camp; var mob=camp.mobs[0]; var player=combat.player.transform;
        Check(mob.hp==PrototypeCamp.GhostHealth && PrototypeCamp.GhostHealth>3,"Ghosts are tougher");
        player.position=new Vector3(mob.home-3,PrototypeWorld.CampLedges[0].y+.6f,0);
        float startY=mob.art.transform.position.y, startDist=Vector3.Distance(mob.art.transform.position,player.position);
        for(int i=0;i<200;i++) Call(camp);
        // Batch frames are a fraction of a millisecond, so this only checks the direction they move in.
        Check(mob.art.transform.position.y>startY+.005f && Vector3.Distance(mob.art.transform.position,player.position)<startDist,"Ghosts fly up toward the seonbi (y "+startY+"->"+mob.art.transform.position.y+" dt="+Time.deltaTime+" paused="+combat.Paused+")");
        Capture("ghost-fly",new Vector3(mob.home-1.5f,2,-10),3);
        combat.Restart();
        Check(Mathf.Abs(mob.art.transform.position.y-PrototypeCamp.HoverY)<.01f,"Restart sets them back down");
    }

    static void BossMoves()
    {
        var player=combat.player.transform; var bossVisual=boss.GetComponent<JinaActorVisual>();
        Check(PrototypeCombat.ChaseSpeed>1.8f,"The boss chases faster");
        // On level ground at mid range: a charge.
        boss.transform.position=new Vector3(46,PrototypeCombat.BossGround,0);
        player.position=new Vector3(40,player.position.y,0);
        Set(combat,"alerted",true); Set(combat,"nextMoveAt",0f); Set(combat,"enemyReadyAt",0f);
        Call(combat);
        Check(combat.Move==PrototypeCombat.BossMove.ChargeWindup,"Mid range on the ground: it winds up a charge");
        bossVisual.Refresh();
        Capture("boss-charge-windup",new Vector3(43,1,-10),3.5f);
        Set(combat,"moveUntil",Time.time-.01f); Call(combat);
        Check(combat.Move==PrototypeCombat.BossMove.Charging,"Then charges");
        float x=boss.transform.position.x; Call(combat);
        Check(boss.transform.position.x<x,"Toward the seonbi");
        Set(combat,"moveUntil",Time.time-.01f); Call(combat);
        Check(combat.Move==PrototypeCombat.BossMove.None,"The charge ends");
        // Up on a ledge: a leap.
        var ledge=PrototypeWorld.ForestLedges[1];
        boss.transform.position=new Vector3(ledge.x+3,PrototypeCombat.BossGround,0);
        player.position=new Vector3(ledge.x,ledge.y+.6f,0);
        Set(combat,"nextMoveAt",0f); Set(combat,"bossRecoveryUntil",0f); Call(combat);
        Check(combat.Move==PrototypeCombat.BossMove.LeapWindup,"A seonbi on a ledge is leapt at");
        Set(combat,"moveUntil",Time.time-.01f); Call(combat);
        Check(combat.Move==PrototypeCombat.BossMove.Leaping,"It leaps");
        Set(combat,"moveUntil",Time.time+PrototypeCombat.LeapLength/2); Call(combat); bossVisual.Refresh();
        Check(boss.transform.position.y>PrototypeCombat.BossGround+2 && Picture(bossVisual).sprite.name.EndsWith("boss_attack_01"),"High in the air, club swinging");
        Capture("boss-leap",new Vector3(ledge.x+1.5f,2.5f,-10),3.5f);
        Set(combat,"moveUntil",Time.time-.01f); Call(combat); bossVisual.Refresh();
        Check(combat.Move==PrototypeCombat.BossMove.Landed && Mathf.Abs(boss.transform.position.y-PrototypeCombat.BossGround)<.01f && Picture(bossVisual).sprite.name.EndsWith("boss_attack_02"),"It slams down on the ground");
        combat.Restart();
        Check(combat.Move==PrototypeCombat.BossMove.None && combat.PlayerHealth==PrototypeCombat.MaxPlayerHealth,"Restart calms the boss");
    }
}
