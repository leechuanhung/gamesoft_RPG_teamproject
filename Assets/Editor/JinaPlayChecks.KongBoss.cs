using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Who = PrototypeKongVillage.Who;

// The Kongjwi demon on the back hill: waiting, the talk and the change, the hill's terrain, what she throws, the raid
// on Kongbat village, the hairpin, and the scenes after (PrototypeKongBoss, PrototypeKongAftermath).
public static partial class JinaPlayChecks
{
    static void KongBoss()
    {
        combat.Restart();
        var boss=combat.KongBoss; var kong=combat.Kong; var after=combat.Aftermath; var q=combat.Quest.progress;
        var player=combat.player.transform; var bag=JinaItems.Inventory;
        foreach(var pose in new[]{"idle_00","idle_01","crouch","hop","open","tongue","cast","crack","death_00","death_01","death_02"})
            Check(JinaArt.Load("21_KongBoss/kong_boss_"+pose)!=null,"Demon frame "+pose);
        foreach(var p in new[]{"kongjwi_kneel","kongjwi_bow","kongjwi_sad","patjwi_down","anak_down","namu_down","hunjang_down","yeot_down","halmi_down"})
            Check(JinaArt.Load("16_Kong/"+p)!=null,"Villager frame "+p);
        foreach(var name in new[]{"훈장","엿장수","할멈"})
        {
            var v=GameObject.Find(name).GetComponent<JinaActorVisual>(); v.Refresh();
            Check(Picture(v).sprite!=null && Picture(v).sprite.name.StartsWith("16_Kong/"),name+" is drawn");
        }
        Check(PrototypeKongVillage.TownLines(Who.Halmi,false,false)[1].Contains("두꺼비"),"The grandmother remembers the toad of the old tale");

        // The developer's F9: the dokkaebi purified as if played through, and the seonbi at the ferry.
        Check(combat.Cheats.SkipToFerry() && q.Outcome==EncounterOutcome.Purified && q.Honor==30 && combat.Camp.Remaining==0 && combat.BossPurified
            && combat.Maps.Current==PrototypeMaps.Index(PrototypeWorld.Ferryman.x) && Mathf.Abs(player.position.x-PrototypeCheats.FerrySpot.x)<.01f,"F9 finishes the purification route and moves him to the ferry");
        Check(combat.Cheats.SkipToFerry() && q.Honor==30,"Pressed again it only moves him");
        combat.Restart();

        // The back hill: a map of its own past the pass, ground at several heights, ledges stacked high, crocks for cover.
        Check(PrototypeMaps.Index(PrototypeWorld.KongLair)==7 && PrototypeWorld.GoalX>PrototypeWorld.HillStart && PrototypeWorld.Top(PrototypeWorld.GoalX)==-.5f,"The back hill is the last map, the road at its end");
        float highest=0; foreach(var l in PrototypeWorld.HillLedges) highest=Mathf.Max(highest,l.top);
        Check(PrototypeWorld.HillLedges.Length>=10 && highest>=4.4f && PrototypeWorld.Top(223)>.9f,"More ledges than the forest, stacked higher, and rises in the ground");
        Physics2D.SyncTransforms();
        Check(Physics2D.Raycast(new Vector2(213.5f,6),Vector2.down,2,1<<8).collider!=null,"The top tier can be stood on");
        Capture("kong-hill",new Vector3(216,2,-10),5.5f);

        // She waits on the hill; in the village she is only there while he cannot see the hill.
        Check(boss.Now==PrototypeKongBoss.Phase.Waiting && Mathf.Abs(boss.Feet.x-PrototypeWorld.KongLair)<.01f,"Kongjwi waits on the back hill");
        var kongjwi=GameObject.Find("콩쥐").GetComponent<SpriteRenderer>();
        player.position=new Vector3(PrototypeWorld.Kongjwi.x-3,.3f,0); Call(kong,"LateUpdate");
        Check(kongjwi.enabled,"Kongjwi is at home while he is in the village");
        player.position=new Vector3(PrototypeWorld.PassStart+5,.3f,0); Call(kong,"LateUpdate");
        Check(!kongjwi.enabled,"Once he is on the pass she has gone ahead to the hill");

        // The talk, her last line, the change.
        var lines=PrototypeKongBoss.MeetingLines(true,false,true);
        Check(lines[0].Contains("오셨네요") && lines[lines.Length-1]==PrototypeKongBoss.LastLine && System.Array.Exists(lines,l=>l.Contains("강물")),"She greets him, smells the river on him, and ends with her threat");
        Check(PrototypeKongBoss.LastLine=="젓갈로 담가 주마!" && PrototypeKongBoss.MeetingPortrait(0,false)=="kongjwi" && PrototypeKongBoss.MeetingPortrait(1,false)=="kongjwi_true"
            && PrototypeKongBoss.MeetingPortrait(0,true)=="kongjwi_true","Her sweet face first, then the true one");
        player.position=new Vector3(PrototypeWorld.KongLair-8,.3f,0); Physics2D.SyncTransforms(); combat.Maps.Sync();
        Call(boss);
        Check(boss.Now==PrototypeKongBoss.Phase.Meeting && combat.Paused && !combat.player.CanMove,"She speaks when he comes near on the hill, and the game holds");
        for(int i=0;i<8 && boss.Now==PrototypeKongBoss.Phase.Meeting;i++) boss.NextLine();
        Check(boss.Now==PrototypeKongBoss.Phase.Changing && combat.Paused && combat.Hud.Hidden,"Her last line changes her");
        foreach(float t in new[]{.5f,1.3f,2.2f})
        {
            Set(boss,"<ChangedAt>k__BackingField",Time.time-t); Call(boss,"LateUpdate");
            Capture("kong-change-"+t.ToString("0.0"),new Vector3(boss.Feet.x,2,-10),3.5f);
        }
        Set(boss,"<ChangedAt>k__BackingField",Time.time-PrototypeKongBoss.ChangeLength-.01f); Call(boss);
        Check(boss.Fighting && !combat.Paused && combat.player.CanMove,"Then the fight begins and he can move");
        Set(combat,"<EnemyHealth>k__BackingField",0);   // the dokkaebi is always dealt with by now
        Check(combat.Hud.Goal().Contains("콩쥐 요괴"),"The goal speaks of her");
        KongDraw();
        Capture("kong-boss",new Vector3(boss.Feet.x-3,2,-10),5.5f);

        // Blows, the hurt box, the cover, what she throws.
        int hp=boss.Health;
        Check(boss.HurtBox.size.x>2 && boss.Damage(PrototypeCombat.SwordDamage,true) && boss.Health==hp-PrototypeCombat.SwordDamage,"A wide target that takes the sword");
        int standing=boss.CrocksStanding;
        boss.StrikeCrock(0,PrototypeKongBoss.CrockShots-1); Check(boss.CrocksStanding==standing && boss.HoneyCount==0,"A crock takes a couple of her shots");
        boss.StrikeCrock(0,1); Check(boss.CrocksStanding==standing-1 && boss.HoneyCount==1,"The third breaks it, and honey spills out");
        // His blows: two break a crock (sword or arrow), and the honey heals him when he touches it.
        float cx=PrototypeWorld.HillCrocks[1];
        var cut=new Bounds(new Vector3(cx,PrototypeWorld.Top(cx)+.6f,0),new Vector3(1.4f,.9f,1));
        Check(boss.BlowCrocks(cut) && boss.CrocksStanding==standing-1 && boss.HoneyCount==1,"One cut only cracks it");
        Check(boss.BlowCrocks(cut) && boss.CrocksStanding==standing-2 && boss.HoneyCount==2,"The second breaks it and spills honey");
        Check(!boss.BlowCrocks(cut),"A broken crock takes no more blows");
        Call(boss);   // the honey hops out and settles
        var dish=boss.HoneyAt(1);
        player.position=new Vector3(dish.x,dish.y+.95f,0); Physics2D.SyncTransforms();
        Call(boss);
        Check(boss.HoneyCount==2,"At full health he leaves the honey where it is");
        Set(combat,"<PlayerHealth>k__BackingField",50); Call(boss);
        Check(combat.PlayerHealth==50+PrototypeKongBoss.HoneyHeal && boss.HoneyCount==1,"Touching it heals him at once");
        Set(combat,"<PlayerHealth>k__BackingField",PrototypeCombat.MaxPlayerHealth);
        int crockAt; Check(boss.CrockArrowDistance(new Vector2(PrototypeWorld.HillCrocks[2]-3,PrototypeWorld.Top(PrototypeWorld.HillCrocks[2])+.6f),Vector2.right,5,out crockAt)<5 && crockAt==2,"Arrows strike the crocks too");
        player.position=new Vector3(PrototypeWorld.KongLair-6,.3f,0);

        // Out of a hollow: against the mound's wall (1.6 high) she leaps high and lands on top.
        var rb=boss.GetComponent<Rigidbody2D>(); var mode=Physics2D.simulationMode;
        Physics2D.simulationMode=SimulationMode2D.Script;
        try
        {
            // The mound (221) from the hollow on its left; the step (204~210) from the hollow on its right.
            foreach(var (wall,top,dir) in new[]{(221f,1.1f,1),(210f,.7f,-1)})
            {
                rb.position=new Vector2(wall-dir*1.1f,-.5f); rb.velocity=Vector2.zero; boss.transform.position=rb.position;
                player.position=new Vector3(wall+dir*2.5f,top+.95f,0); Physics2D.SyncTransforms();
                boss.Hop(wall+dir*2.5f);
                Check(boss.HighHop,"A wall ahead or him higher up: a high hop");
                Set(boss,"crouchUntil",Time.time);
                bool onTop=false;
                for(int i=0;i<90 && !onTop;i++)
                {
                    Call(boss,"FixedUpdate"); Physics2D.Simulate(.02f);
                    onTop=rb.position.y>top-.05f && Mathf.Abs(rb.velocity.y)<.5f && (rb.position.x-wall)*dir>.3f;
                }
                Check(onTop,"She hops out of the hollow onto the ground at "+wall+": "+rb.position);
            }
        }
        finally { Physics2D.simulationMode=mode; }
        rb.velocity=Vector2.zero; Set(boss,"airborne",false); Set(boss,"crouchUntil",0f);
        boss.transform.position=new Vector3(PrototypeWorld.KongLair,PrototypeWorld.Top(PrototypeWorld.KongLair),0); rb.position=boss.transform.position;
        var release=boss.GetType().GetMethod("Release",BindingFlags.NonPublic|BindingFlags.Instance);
        release.Invoke(boss,new object[]{PrototypeKongBoss.Attack.Beans});
        Check(boss.ShotCount==5,"Five beans in a fan");
        for(int i=0;i<200 && boss.ShotCount>0;i++) boss.StepShots(.03f);
        Check(boss.ShotCount==0,"The beans end on him, a wall or their range");
        Set(combat,"invulnerableUntil",0f);
        int before=combat.PlayerHealth;
        player.position=new Vector3(PrototypeWorld.KongLair-6,-.5f+.95f,0); Physics2D.SyncTransforms();
        release.Invoke(boss,new object[]{PrototypeKongBoss.Attack.Spit});
        Check(boss.ShotCount==1,"A gob of salted fish is lobbed");
        for(int i=0;i<200 && boss.ShotCount>0;i++) boss.StepShots(.02f);
        Check(boss.ShotCount==0 && (combat.PlayerHealth<before || boss.PuddleCount>0),"It comes down on him or leaves brine");
        Set(combat,"invulnerableUntil",0f);
        // Boulders roll through her.
        int health=boss.Health;
        boss.transform.position=new Vector3(PrototypeWorld.PassStart+8,PrototypeWorld.Top(PrototypeWorld.PassStart+8),0);
        var rock=combat.Pass.Spawn(PrototypeWorld.PassStart+8); combat.Pass.Step(0);
        Check(boss.Health==health,"Boulders do not hurt her");
        // She cannot cross the river: past it she heads back for the village.
        player.position=new Vector3(PrototypeWorld.RiverLeft-4,.3f,0);
        Check(PrototypeKongBoss.MinX>PrototypeWorld.RiverRight && boss.Goal(kong)==PrototypeWorld.KongEnd-4,"Across the river he is out of her reach; she goes for the village");
        Check(PrototypeFerry.Lines(q,true,false,true,true)[0].Contains("비명") && string.Join("",PrototypeFerry.Lines(q,true,true,true,true)).Contains("비녀"),"The ferryman sees he has fled her");
        Check(PrototypeQuest.ElderPages(new QuestProgress(),true,true).Length==4 && PrototypeQuest.ElderPages(new QuestProgress(),true,true)[2].Contains("비녀")
            && PrototypeQuest.ElderPages(new QuestProgress(),true,false)[1].Contains("두꺼비"),"The elder speaks of the toad and the hairpin when he comes back");

        // The hairpin: only with half her health gone, close by.
        player.position=new Vector3(PrototypeWorld.KongLair-8,.3f,0);
        boss.transform.position=new Vector3(PrototypeWorld.KongLair,PrototypeWorld.Top(PrototypeWorld.KongLair),0);
        string why;
        Check(!boss.TryPurify(true,out why) && boss.Fighting,"No hairpin, no purification");
        bag.TryAdd(JinaItems.Keepsake);
        player.position=new Vector3(boss.Feet.x-1.5f,.3f,0);
        Check(!boss.TryPurify(true,out why) && why.Contains("절반") && boss.Fighting,"Not until half her health is gone");
        Set(boss,"<Health>k__BackingField",PrototypeKongBoss.MaxHealth/2);
        int honor=q.Honor;
        Check(boss.TryPurify(true,out why) && boss.Now==PrototypeKongBoss.Phase.Purified && bag.GetQuantity(JinaItems.Keepsake)==0
            && q.Honor==honor+50 && q.KongOutcome==EncounterOutcome.Purified,"The hairpin purifies her: +50, and it is spent");
        foreach(float t in new[]{.3f,1.1f,1.9f,2.8f})
        {
            Set(boss,"<PurifiedAt>k__BackingField",Time.time-t); Call(boss,"LateUpdate");
            Capture("kong-purify-"+t.ToString("0.0"),new Vector3(boss.Feet.x,2,-10),3.5f);
        }
        // Out on the hill she wakes and he tells her; once he has gone she is home, and her sister comes to her.
        Set(boss,"<PurifiedAt>k__BackingField",Time.time-PrototypeKongBoss.PurifyLength-.01f); Call(after);
        Check(after.Playing && after.Scene=="field" && after.CurrentLine.Contains("선비님"),"Kongjwi wakes on the hill");
        while(after.Playing) after.Advance();
        player.position=new Vector3(PrototypeWorld.Kongjwi.x-3,.3f,0); combat.Maps.Sync(); Call(boss);
        Check(boss.HandedBack && boss.AtHome && Mathf.Abs(kong.PersonX(Who.Kongjwi)-PrototypeWorld.Kongjwi.x)<.01f,"She goes home once he has left the hill");
        Call(after);
        Check(after.Playing && after.Scene=="village" && after.CurrentSpeaker=="팥쥐" && !kong.Raided,"Nobody saw the demon: her sister comes to her");
        while(after.Playing) after.Advance();
        kong.Open(Who.Kongjwi);
        Check(kong.IsOpen && kong.Portrait=="kongjwi" && Field<string[]>(kong,"lines")[0].Contains("기억"),"Herself again, she remembers everything, in her portrait window");
        kong.Next(); kong.Next();

        // The raid: in the village she binds everyone, Patjwi runs to the far left, and they die from the right.
        combat.Restart();
        KongFight();
        boss.transform.position=new Vector3(PrototypeWorld.KongEnd-3,-.5f,0); boss.GetComponent<Rigidbody2D>().position=boss.transform.position;
        player.position=new Vector3(PrototypeWorld.Kongjwi.x,.3f,0); combat.Maps.Sync();
        Call(boss);
        Check(kong.Raided && kong.FateOf(Who.Anak)==PrototypeKongVillage.Fate.Bound && kong.FateOf(Who.Patjwi)==PrototypeKongVillage.Fate.Bound,"Her curse binds everyone in the village");
        Check(Field<float[]>(kong,"goals")[(int)Who.Patjwi]==PrototypeWorld.PatjwiRaidX,"Patjwi runs to the far left");
        kong.Place(Who.Patjwi,PrototypeWorld.PatjwiRaidX);
        Check(kong.NextVictim()==Who.Hunjang,"The rightmost first");
        // Two cuts while she winds up a kill make her spare them; left alone, the lash kills.
        Set(boss,"castUntil",0f); Set(boss,"nextKillAt",0f);
        boss.transform.position=new Vector3(kong.PersonX(Who.Hunjang)+PrototypeKongBoss.KillReach,-.5f,0);
        Call(boss);
        Check(boss.KillAt>=0 && kong.Threatened==Who.Hunjang && kong.ThreatAt>=0,"She squeezes her victim, who cries out");
        Call(kong,"LateUpdate"); var hunjang=GameObject.Find("훈장").GetComponent<VillagerVisual>(); hunjang.Refresh();
        Check(hunjang.look==VillagerVisual.Look.Bound && hunjang.trembling,"Bound and trembling");
        KongDraw();
        Capture("kong-raid",new Vector3(boss.Feet.x-2,2,-10),4);
        boss.Damage(PrototypeCombat.SwordDamage,true); boss.Damage(PrototypeCombat.SwordDamage,true);
        Check(boss.KillAt<0 && kong.Alive(Who.Hunjang),"Two sword cuts make her let go");
        Set(boss,"stunnedUntil",0f); Set(boss,"nextKillAt",0f); Call(boss);
        Set(boss,"<KillAt>k__BackingField",Time.time-PrototypeKongBoss.KillWindup-.01f); Call(boss);
        Check(!kong.Alive(Who.Hunjang) && q.Honor==-10 && q.VillagersLost==1 && kong.LostAt>=0,"Each death costs ten");
        var order=new List<Who>();
        while(kong.NextVictim().HasValue) { var w=kong.NextVictim().Value; order.Add(w); kong.Kill(w); }
        Check(string.Join(",",order)=="Yeot,Namu,Halmi,Anak,Patjwi" && q.Honor==-60 && !kong.AnyoneToTake,"From the right, Patjwi last: "+string.Join(",",order));
        Call(kong,"LateUpdate"); var patjwi=GameObject.Find("팥쥐").GetComponent<JinaActorVisual>(); patjwi.Refresh();
        Check(Picture(patjwi).sprite.name.EndsWith("_down"),"The dead lie where they fell");
        KongDraw();
        Capture("kong-raid-after",new Vector3(120,2,-10),5.5f);
        player.position=new Vector3(80,.3f,0);
        Check(boss.Goal(kong)==PrototypeKongBoss.VillageCentre,"With nobody left she waits in the middle of the village");
        // Slain: +10.
        Set(boss,"<Health>k__BackingField",PrototypeCombat.SwordDamage);
        boss.Damage(PrototypeCombat.SwordDamage,false);
        Check(boss.Now==PrototypeKongBoss.Phase.Slain && q.KongOutcome==EncounterOutcome.Slain && q.Honor==-50,"Slain: ten back");

        // Purified in the village with some dead: she weeps, the survivors turn on her, Patjwi and he stop them.
        combat.Restart();
        KongFight();
        boss.transform.position=new Vector3(PrototypeWorld.KongEnd-3,-.5f,0);
        player.position=new Vector3(boss.Feet.x-1.5f,.3f,0); combat.Maps.Sync();
        Call(boss);
        kong.Place(Who.Patjwi,PrototypeWorld.PatjwiRaidX);
        kong.Kill(Who.Hunjang); kong.Kill(Who.Yeot);
        bag.TryAdd(JinaItems.Keepsake); Set(boss,"<Health>k__BackingField",PrototypeKongBoss.MaxHealth/2);
        Check(boss.TryPurify(true,out why),"Purified in the village");
        Check(kong.FateOf(Who.Anak)==PrototypeKongVillage.Fate.Free,"The curse lets the living go");
        Set(boss,"<PurifiedAt>k__BackingField",Time.time-PrototypeKongBoss.PurifyLength-.01f); Call(boss);
        Check(boss.HandedBack,"In the village she is handed over at once");
        Call(after);
        var said=new List<string>();
        while(after.Playing) { said.Add(after.CurrentSpeaker+":"+after.CurrentLine); after.Advance(); }
        string all=string.Join("|",said);
        Check(after.Scene=="village" && all.Contains("콩쥐:용서해") && all.Contains("요물") && all.Contains("팥쥐:그만해요") && all.Contains("선비:물러서시오"),"She weeps; they come at her; Patjwi and he stop them: "+all);

        // Patjwi dead: Kongjwi runs past the others to her body and wails.
        combat.Restart();
        KongFight();
        boss.transform.position=new Vector3(PrototypeWorld.KongEnd-3,-.5f,0);
        player.position=new Vector3(boss.Feet.x-1.5f,.3f,0); combat.Maps.Sync();
        Call(boss);
        kong.Place(Who.Patjwi,PrototypeWorld.PatjwiRaidX);
        while(kong.NextVictim().HasValue) kong.Kill(kong.NextVictim().Value);
        bag.TryAdd(JinaItems.Keepsake); Set(boss,"<Health>k__BackingField",PrototypeKongBoss.MaxHealth/2);
        boss.TryPurify(true,out why);
        Set(boss,"<PurifiedAt>k__BackingField",Time.time-PrototypeKongBoss.PurifyLength-.01f); Call(boss); Call(after);
        Check(after.Scene=="wail","Patjwi lies dead: the wailing scene");
        while(after.Playing && !after.CurrentLine.StartsWith("팥쥐야!!")) after.Advance();
        Check(Mathf.Abs(Field<float[]>(kong,"goals")[0]-(PrototypeWorld.PatjwiRaidX+.9f))<.01f,"She runs to Patjwi's body, past the others");
        kong.Place(Who.Kongjwi,PrototypeWorld.PatjwiRaidX+.9f); after.Advance();
        Call(kong,"LateUpdate"); var kv=GameObject.Find("콩쥐").GetComponent<VillagerVisual>(); kv.Refresh();
        Check(kv.look==VillagerVisual.Look.Bow && kv.tears && Picture(kv).sprite.name.EndsWith("_bow"),"Bent over her sister, weeping");
        KongDraw();
        Capture("kong-wail",new Vector3(PrototypeWorld.PatjwiRaidX+2,1.5f,-10),3);
        while(after.Playing && !after.CurrentLine.Contains("아아")) after.Advance();
        Check(after.Playing,"She wails her sister's name");
        // The first village hears what he really did: the dokkaebi slain stays slain, however high his honour climbs.
        var slainThenPurified=new QuestProgress(); slainThenPurified.Resolve(false); slainThenPurified.ResolveKong(true);
        Check(slainThenPurified.Honor>=30 && !PrototypeTavern.Greeting(slainThenPurified).Contains("저주를 풀어") && PrototypeTavern.Greeting(slainThenPurified).Contains("콩밭")
            && !PrototypePeddler.Greeting(slainThenPurified).Contains("도깨비를 달래") && PrototypePeddler.Greeting(slainThenPurified).Contains("깎아"),"Kongjwi's honour does not turn the slain dokkaebi into a purified one");
        var slainOnly=new QuestProgress(); slainOnly.Resolve(false);
        var purifiedOnly=WithCharm(new QuestProgress()); purifiedOnly.Resolve(true);
        Check(PrototypeTavern.Greeting(slainOnly).Contains("물리치셨") && PrototypeTavern.Greeting(purifiedOnly).Contains("저주를 풀어")
            && PrototypePeddler.Greeting(slainOnly).Contains("숲길") && PrototypePeddler.Greeting(purifiedOnly).Contains("달래어"),"Before Kongbat they speak of the dokkaebi as it went");

        // Struck down after she has spoken: R starts him at the foot of the hill, the story kept, the fight undone.
        combat.Restart();
        Check(!combat.CanRetryAtHill,"No checkpoint before he reaches her");
        kong.WakeUp(); combat.AddArrows(-3); int arrowsAt=combat.Arrows, honorAt=q.Honor;
        KongFight();
        Check(combat.CanRetryAtHill && combat.Checkpoint.Value.arrows==arrowsAt && combat.Checkpoint.Value.honor==honorAt,"Her talk saves the foot of the hill");
        boss.transform.position=new Vector3(PrototypeWorld.KongEnd-3,-.5f,0);
        player.position=new Vector3(PrototypeWorld.Kongjwi.x,.3f,0); combat.Maps.Sync(); Call(boss);
        kong.Kill(Who.Hunjang); combat.AddArrows(-5); Set(boss,"<Health>k__BackingField",100);
        Set(combat,"invulnerableUntil",0f); combat.HurtPlayer(1000);
        Check(combat.PlayerHealth==0 && combat.CanRetryAtHill && kong.Raided,"Struck down in the middle of her raid");
        combat.RestartAtHill();
        Check(combat.PlayerHealth==PrototypeCombat.MaxPlayerHealth && !combat.player.Downed && combat.player.CanMove && !combat.GameOver.Showing
            && Mathf.Abs(player.position.x-PrototypeCombat.HillEntrance.x)<.01f && combat.Maps.Current==PrototypeMaps.Index(PrototypeWorld.HillStart+1) && !combat.Maps.Travelling,"He rises at the foot of the hill");
        Check(boss.Now==PrototypeKongBoss.Phase.Waiting && boss.Health==PrototypeKongBoss.MaxHealth && boss.Retries==1 && q.Honor==honorAt && q.VillagersLost==0
            && kong.Alive(Who.Hunjang) && !kong.Raided && combat.Arrows==arrowsAt,"Her fight is undone: everyone alive, honour and arrows as they were");
        Check(kong.Stayed && combat.CanRetryAtHill,"What he has learned is kept, and so is the checkpoint");
        player.position=new Vector3(PrototypeWorld.KongLair-8,.3f,0); Physics2D.SyncTransforms(); combat.Maps.Sync(); Call(boss);
        Check(boss.Now==PrototypeKongBoss.Phase.Meeting && Field<string[]>(boss,"lines").Length==2 && boss.Line.StartsWith("또 오셨네요"),"Coming back, she only says a word before changing");
        combat.Restart();
        Check(!combat.CanRetryAtHill && boss.Retries==0,"Starting over clears the checkpoint");
        Check(PrototypeEnding.KongCoda(EncounterOutcome.Purified,false,6)[0].Contains("팥쥐") && PrototypeEnding.KongCoda(EncounterOutcome.Purified,true,0)[1].Contains("비녀")
            && PrototypeEnding.KongCoda(EncounterOutcome.Slain,true,0)[1].Contains("독 속"),"The ending remembers how Kongbat ended");
        combat.Restart();
        Check(boss.Now==PrototypeKongBoss.Phase.Waiting && boss.Health==PrototypeKongBoss.MaxHealth && !kong.Raided && kong.Alive(Who.Patjwi) && !after.Playing && q.KongOutcome==EncounterOutcome.Unresolved,"Restart puts her back on the hill and everyone back home");
    }

    // Draws her and the villagers as they stand now, before a capture (the checks run within one frame).
    static void KongDraw()
    {
        Call(combat.Kong,"LateUpdate");
        foreach(var v in Object.FindObjectsOfType<VillagerVisual>()) v.Refresh();
        Call(combat.KongBoss,"LateUpdate");
    }
    // Walks him up to her, through her talk and the change, to the start of the fight.
    static void KongFight()
    {
        var boss=combat.KongBoss; var player=combat.player.transform;
        player.position=new Vector3(PrototypeWorld.KongLair-8,.3f,0); Physics2D.SyncTransforms(); combat.Maps.Sync();
        Call(boss);
        for(int i=0;i<8 && boss.Now==PrototypeKongBoss.Phase.Meeting;i++) boss.NextLine();
        Set(boss,"<ChangedAt>k__BackingField",Time.time-PrototypeKongBoss.ChangeLength-.01f); Call(boss);
        Check(boss.Fighting,"The fight begins");
    }
}
