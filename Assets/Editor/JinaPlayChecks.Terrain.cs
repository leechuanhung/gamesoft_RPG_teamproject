using UnityEngine;

// Backdrops and ledges, aimed arrows, flying ghosts, and the boss's charge and leap.
public static partial class JinaPlayChecks
{
    static void Terrain()
    {
        foreach (var area in new[]{"stele","camp","village","forest"})
            Check(GameObject.Find("원경 "+area)!=null && GameObject.Find("근경 "+area)!=null,"Backdrop layers for "+area);
        Physics2D.SyncTransforms();
        foreach (var set in new[]{WorldBuilder.CampLedges,WorldBuilder.ForestLedges})
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
        // 겨눈 그림(make_aim.py): 활 15도, 칼 30도마다 네 장씩, 수평 그림과 같은 배율이고 발은 넓힌 만큼 위.
        for (int b=-90;b<=90;b+=Combat.BowAimStep) if (b!=0)
        {
            var f=JinaArt.Frames("03_Player_Attack_Bow/bow_aim_"+SeonbiVisual.AimName(b)+"_"); var level0=JinaArt.Load("03_Player_Attack_Bow/bow_attack_00");
            Check(f.Length==4 && Mathf.Approximately(f[0].pixelsPerUnit,level0.pixelsPerUnit) && Mathf.Approximately(f[0].pivot.y,level0.pivot.y+JinaArt.AimPad) && Mathf.Approximately(f[0].pivot.x,level0.pivot.x),"Aimed bow frames "+b);
        }
        for (int b=-90;b<=90;b+=Combat.SwordAimStep) if (b!=0) foreach (var kind in new[]{"sword_aim_","sword_aim_empty_"})
        {
            var f=JinaArt.Frames("02_Player_Attack_Sword/"+kind+SeonbiVisual.AimName(b)+"_"); var level0=JinaArt.Load("02_Player_Attack_Sword/sword_attack_00");
            Check(f.Length==4 && Mathf.Approximately(f[0].pixelsPerUnit,level0.pixelsPerUnit) && Mathf.Approximately(f[0].pivot.y,level0.pivot.y+JinaArt.AimPad) && Mathf.Approximately(f[0].pivot.x,level0.pivot.x),"Aimed sword frames "+kind+b);
        }
        Check(Combat.AimBucket(7,15)==0 && Combat.AimBucket(8,15)==15 && Combat.AimBucket(-89,15)==-90 && Combat.AimBucket(120,15)==90
            && Combat.AimBucket(44,30)==30 && Combat.AimBucket(46,30)==60,"The nearest aimed picture is chosen");
        var visual=combat.player.GetComponent<SeonbiVisual>(); var body=Field<SpriteRenderer>(visual,"body");
        Bow(combat,true);
        // 몸은 가장 가까운 그림, 화살은 마우스 각도 그대로(왼쪽도), 겨눈 그림의 시위에서 떠난다.
        foreach (var (angle,facing,frame) in new[]{(30f,1,"bow_aim_u30_02"),(-30f,1,"bow_aim_d30_02"),(70f,1,"bow_aim_u75_02"),(-60f,-1,"bow_aim_d60_02"),(89f,-1,"bow_aim_u90_02"),(5f,1,"bow_attack_02")})
        {
            visual.Attack(true,facing,angle); Set(visual,"attackTime",.31f); visual.SendMessage("LateUpdate");
            Check(body.sprite.name.EndsWith(frame),"Aimed shot frame "+angle+" is "+frame);
            Set(combat,"pendingBow",true); Set(combat,"pendingAngle",angle); Set(combat,"attackFacing",facing); Set(combat,"playerImpactAt",Time.time);
            Call(combat);
            var shot=GameObject.Find("Arrow").GetComponent<SpriteRenderer>();
            var flight=(Vector2)(shot.transform.rotation*(facing<0 ? Vector3.left : Vector3.right));
            var want=Combat.AimDirection(angle,facing);
            Check(Vector2.Angle(flight,want)<.1f && shot.flipX==(facing<0),"Arrow points along the mouse at "+angle+" facing "+facing);
            var arrows=(System.Collections.IList)typeof(Combat).GetField("arrows",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(combat);
            var flying=arrows[0]; var dir=(Vector2)flying.GetType().GetField("direction").GetValue(flying);
            Check(Vector2.Angle(dir,want)<.1f,"Arrow flies toward the mouse at "+angle+" facing "+facing);
            var launch=Combat.ArrowLaunch(Combat.AimBucket(angle,Combat.BowAimStep));
            var from=new Vector2(combat.player.transform.position.x+facing*launch.x,combat.player.GetComponent<SpriteRenderer>().bounds.min.y+launch.y);
            Check(Vector2.Distance((Vector2)shot.transform.position-dir*14f*Time.deltaTime,from)<.05f,"Arrow leaves the drawn bow at "+angle);
            if (angle==30f || angle==-30f || angle==89f) Capture("bow-aim-"+angle,new Vector3(combat.player.transform.position.x+facing,1.5f,-10),2.5f);
            shot.gameObject.name="Checked Arrow";
            typeof(Combat).GetMethod("ClearArrows",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(combat,null);
        }
        Set(combat,"pendingAngle",0f);
        Bow(combat,false);
        // 칼: 가장 가까운 30도 그림. 단검·명검(칼 없는 그림에 무기를 쥐여 줌)은 손이 그림을 따라가고 칼이 같이 돈다.
        foreach (var (angle,facing,frame) in new[]{(70f,1,"sword_aim_u60_02"),(-100f,-1,"sword_aim_d90_02"),(10f,1,"sword_attack_02")})
        {
            visual.Attack(false,facing,angle,0); Set(visual,"attackTime",.25f); visual.SendMessage("LateUpdate");
            Check(body.sprite.name.EndsWith(frame),"Aimed cut frame "+angle+" is "+frame);
        }
        Check(SeonbiVisual.SwingPalmAt(2,90,out float up).y>0 && up==90
            && SeonbiVisual.SwingPalmAt(2,90,out _).y-JinaArt.AimPad<SeonbiVisual.SwingPalmAt(2,0,out _).y-40,"An upward cut lifts the held weapon's hand");
        Check(SeonbiVisual.SwingPalmAt(1,30,out float raised).x==SeonbiVisual.SwingPalmAt(1,0,out _).x-20 && raised==0,"The raised frame only leans back");
        visual.ResetPose(); visual.SendMessage("LateUpdate");
        // 칼도 마우스 방향으로: 수평이면 예전 앞쪽 상자, 위·아래·뒤로도 닿는다.
        var level=Combat.SwordBox(Vector2.zero,1,0,1.2f,.9f);
        Check(Mathf.Abs(level.center.x-.88f)<.001f && Mathf.Abs(level.center.y)<.001f && Mathf.Abs(level.size.x-1.2f)<.001f && Mathf.Abs(level.size.y-.9f)<.001f,"A level cut keeps the old reach");
        var flier=new Bounds(new Vector3(.2f,1.3f,0),new Vector3(.8f,.8f,1));
        Check(Combat.SwordBox(Vector2.zero,1,90,1.2f,.9f).Intersects(flier) && !level.Intersects(flier),"An upward cut reaches a flier overhead");
        Check(Combat.SwordBox(Vector2.zero,-1,90,1.2f,.9f).Intersects(flier),"Upward cut works facing left too");
        var below=new Bounds(new Vector3(0,-1.3f,0),new Vector3(.8f,.8f,1));
        Check(Combat.SwordBox(Vector2.zero,1,-90,1.2f,.9f).Intersects(below) && !Combat.SwordBox(Vector2.zero,1,90,1.2f,.9f).Intersects(below),"A downward cut reaches below only");
        var diag=Combat.SwordBox(Vector2.zero,-1,45,1.2f,.9f);
        Check(diag.center.x<0 && diag.center.y>0 && !diag.Intersects(new Bounds(new Vector3(1,0,0),new Vector3(.5f,.5f,1))),"A diagonal cut goes up-left and not behind");
        Check(Combat.ArrowLaunch(30).y>Combat.ArrowLaunch(0).y && Combat.ArrowLaunch(-30).y<Combat.ArrowLaunch(0).y && Combat.ArrowLaunch(90).y>Combat.ArrowLaunch(45).y,"Aimed arrows leave the bow higher or lower");
        // A diagonal arrow meets a target above, and passes up through a ledge.
        var above=new Bounds(new Vector3(4,2.3f,0),new Vector3(1,1.4f,1));
        Check(!float.IsInfinity(Combat.ArrowHitDistance(above,Vector2.zero,Combat.AimDirection(30,1),10)),"Upward arrow hits a flier");
        Check(float.IsInfinity(Combat.ArrowHitDistance(above,Vector2.zero,Combat.AimDirection(0,1),10)),"A level arrow passes under it");
        var ledge=WorldBuilder.CampLedges[0];
        var wall=typeof(Combat).GetMethod("Wall",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static)
            .Invoke(null,new object[]{new Vector2(ledge.x,ledge.y-1),Vector2.up,2f});
        Check(((RaycastHit2D)wall).collider==null,"Arrows fly through ledges");
    }

    static void FlyingGhosts()
    {
        var camp=combat.Camp; var mob=camp.mobs[0]; var player=combat.player.transform;
        Check(mob.hp==Camp.GhostHealth && Camp.GhostHealth>3,"Ghosts are tougher");
        player.position=new Vector3(mob.home-3,WorldBuilder.CampLedges[0].y+.6f,0);
        float startY=mob.art.transform.position.y, startDist=Vector3.Distance(mob.art.transform.position,player.position);
        for(int i=0;i<200;i++) Call(camp);
        // Batch frames are a fraction of a millisecond, so this only checks the direction they move in.
        Check(mob.art.transform.position.y>startY+.005f && Vector3.Distance(mob.art.transform.position,player.position)<startDist,"Ghosts fly up toward the seonbi (y "+startY+"->"+mob.art.transform.position.y+" dt="+Time.deltaTime+" paused="+combat.Paused+")");
        Capture("ghost-fly",new Vector3(mob.home-1.5f,2,-10),3);
        combat.Restart();
        Check(Mathf.Abs(mob.art.transform.position.y-Camp.HoverY)<.01f,"Restart sets them back down");
    }

    static void BossMoves()
    {
        var player=combat.player.transform; var bossVisual=boss.GetComponent<JinaActorVisual>();
        Check(Combat.ChaseSpeed>1.8f,"The boss chases faster");
        // On level ground at mid range: a charge.
        boss.transform.position=new Vector3(46,Combat.BossGround,0);
        player.position=new Vector3(40,player.position.y,0);
        Set(combat,"alerted",true); Set(combat,"nextMoveAt",0f); Set(combat,"enemyReadyAt",0f);
        Call(combat);
        Check(combat.Move==Combat.BossMove.ChargeWindup,"Mid range on the ground: it winds up a charge");
        bossVisual.Refresh();
        Capture("boss-charge-windup",new Vector3(43,1,-10),3.5f);
        Set(combat,"moveUntil",Time.time-.01f); Call(combat);
        Check(combat.Move==Combat.BossMove.Charging,"Then charges");
        float x=boss.transform.position.x; Call(combat);
        Check(boss.transform.position.x<x,"Toward the seonbi");
        Set(combat,"moveUntil",Time.time-.01f); Call(combat);
        Check(combat.Move==Combat.BossMove.None,"The charge ends");
        // Up on a ledge: a leap.
        JinaFx.Clear();
        var ledge=WorldBuilder.ForestLedges[1];
        boss.transform.position=new Vector3(ledge.x+3,Combat.BossGround,0);
        player.position=new Vector3(ledge.x,ledge.y+.6f,0);
        Set(combat,"nextMoveAt",0f); Set(combat,"bossRecoveryUntil",0f); Call(combat);
        Check(combat.Move==Combat.BossMove.LeapWindup,"A seonbi on a ledge is leapt at");
        Check(BossFxCount("ring")==0,"No shockwave during leap windup");
        Set(combat,"moveUntil",Time.time-.01f); Call(combat);
        Check(combat.Move==Combat.BossMove.Leaping,"It leaps");
        Set(combat,"moveUntil",Time.time+Combat.LeapLength/2); Call(combat); bossVisual.Refresh();
        Check(boss.transform.position.y>Combat.BossGround+2 && Picture(bossVisual).sprite.name.EndsWith("boss_attack_01"),"High in the air, club swinging");
        Capture("boss-leap",new Vector3(ledge.x+1.5f,2.5f,-10),3.5f);
        Check(BossFxCount("ring")==0,"No shockwave in the air");
        int takeoffDust=BossFxCount("dust");
        Set(combat,"moveUntil",Time.time-.01f); Call(combat); bossVisual.Refresh();
        Check(combat.Move==Combat.BossMove.Landed && Mathf.Abs(boss.transform.position.y-Combat.BossGround)<.01f && Picture(bossVisual).sprite.name.EndsWith("boss_attack_02"),"It slams down on the ground");
        Call(combat);
        Check(BossFxCount("ring")==1 && BossFxCount("dust")==takeoffDust+8,"Landing emits one ring and eight dust puffs, once");
        combat.Restart();
        Check(combat.Move==Combat.BossMove.None && combat.PlayerHealth==Combat.MaxPlayerHealth,"Restart calms the boss");

        // A normal club strike still hits the ground if the seonbi moves out of range.
        boss.transform.position=new Vector3(46,Combat.BossGround,0);
        player.position=new Vector3(45,Combat.BossGround,0);
        Set(combat,"alerted",true); Set(combat,"enemyReadyAt",0f);
        Call(combat,"UpdateBoss");
        Check(Field<float>(combat,"windupUntil")>Time.time && BossFxCount("ring")==0,"No shockwave during a normal windup");
        player.position=new Vector3(40,Combat.BossGround,0);
        Set(combat,"windupUntil",Time.time-.01f);
        Call(combat,"UpdateBoss"); bossVisual.Refresh();
        Check(Picture(bossVisual).sprite.name.EndsWith("boss_attack_02") && combat.PlayerHealth==Combat.MaxPlayerHealth,"The missed strike reaches its contact frame without dealing damage");
        Call(combat,"UpdateBoss");
        Check(BossFxCount("ring")==1 && BossFxCount("dust")==8,"A normal strike emits one ring and eight dust puffs, once");
        combat.Restart();
    }

    static int BossFxCount(string name)
    {
        int count=0;
        foreach(var art in UnityEngine.Object.FindObjectsOfType<SpriteRenderer>())
            if(art.enabled && art.sprite!=null && art.sprite.name.StartsWith("20_Fx/"+name+"_")) count++;
        return count;
    }
}
