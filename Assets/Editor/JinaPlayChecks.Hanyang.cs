using System.Collections;
using UnityEngine;

// The road to Hanyang (기획서.md 1-2, 1-3): the six new maps, the government soldiers by honour, the new common monsters,
// the beacon fortress, Hanyang's people, the councillor's banquet, change and fight, the checkpoint and the endings.
public static partial class JinaPlayChecks
{
    static void Hanyang()
    {
        combat.Restart();
        var player=combat.player.transform; var maps=combat.Maps; var q=combat.Quest.progress;

        // Six maps past the back hill, the councillor's house last, the road to the examination at its end.
        Check(Maps.All.Length==14 && Maps.Index(WorldBuilder.FortStart+1)==8 && Maps.Index(300)==9 && Maps.Index(360)==10 && Maps.Index(400)==11
            && Maps.Index(450)==12 && Maps.Index(WorldBuilder.OfficialX)==13 && Maps.Index(WorldBuilder.GoalX)==13,"Six maps on the road to Hanyang");
        Check(WorldBuilder.Top(256)==.8f && WorldBuilder.Top(262)==2f && WorldBuilder.Top(280)==-.5f && WorldBuilder.Top(367)==1.1f && WorldBuilder.Top(WorldBuilder.GoalX)==-.5f,"The fortress and the grave road rise and fall");
        Physics2D.SyncTransforms();
        Check(Physics2D.Raycast(new Vector2(WorldBuilder.FortPeak.x,6),Vector2.down,2.5f,1<<8).collider!=null,"The fortress's highest ledge can be stood on");
        foreach(var prop in new[]{"봉수 군령판","산성 소나무","무너진 성가퀴","산성 우물","버려진 역참","부서진 수레","무덤","한양 가게","청사초롱","홍살문","과거 방","좌의정 댁 솟을대문","석등","좌의정 댁 누각"})
            Check(GameObject.Find(prop)!=null && GameObject.Find(prop).GetComponent<SpriteRenderer>().sprite!=null,"Prop placed: "+prop);
        foreach(var kind in new[]{"blade","bow"}) for(int r=1;r<=3;r++)
            foreach(var pose in new[]{"idle","walk_00","walk_01","walk_02","walk_03","ready","strike","hurt","down"}) Check(JinaArt.Load("22_Soldier/"+kind+"_"+r+"_"+pose)!=null,"Soldier frame "+kind+r+pose);
        foreach(var p in new[]{"23_Mobs/wisp_idle_00","23_Mobs/wisp_dash","23_Mobs/wisp_down","23_Mobs/brute_idle","23_Mobs/brute_ready","23_Mobs/brute_slam","23_Mobs/brute_down",
            "25_Final/official_idle","25_Final/official_cup","25_Final/official_blank","25_Final/final_idle_00","25_Final/final_open","25_Final/final_reach","25_Final/final_raise","25_Final/final_cast","25_Final/final_death_02",
            "24_Hanyang/sangin","24_Hanyang/yusaeng","24_Hanyang/cheongji","24_Hanyang/ai","24_Hanyang/mudang"}) Check(JinaArt.Load(p)!=null,"Load "+p);
        // Soldiers stand the seonbi's height, the councillor towers over him.
        float seonbi=JinaArt.Load(JinaArt.Player).bounds.size.y;
        Check(Mathf.Abs(JinaArt.Load("22_Soldier/blade_1_idle").bounds.size.y-2.2f)<.4f && JinaArt.Load("25_Final/final_idle_00").bounds.size.y>seonbi*2,"Soldiers on his scale, the councillor towering");

        // The back hill stays shut until Kongjwi is dealt with: fleeing her fight into the fortress would skip her.
        var kongBoss=combat.KongBoss;
        Check(kongBoss.Sealed,"Miasma shuts the end of the back hill while she waits");
        Physics2D.SyncTransforms();
        var wall=Physics2D.Raycast(new Vector2(WorldBuilder.HillEnd-3,.3f),Vector2.right,4,1<<8);
        Check(wall.collider!=null && !wall.collider.usedByEffector && wall.point.x<WorldBuilder.HillEnd,"…a wall he cannot walk through");
        Capture("hill-miasma",new Vector3(WorldBuilder.HillEnd-4,1.6f,-10),3.5f);
        Set(kongBoss,"<Now>k__BackingField",KongBoss.Phase.Slain); Call(kongBoss);
        Check(!kongBoss.Sealed,"Dealt with, the miasma lifts");
        combat.Restart(); Call(kongBoss);
        Check(kongBoss.Sealed,"Restart brings it back");

        // ── Soldiers by honour ──
        Check(RoadMobs.Rank(0)==0 && RoadMobs.Rank(30)==0 && RoadMobs.Rank(-1)==1 && RoadMobs.Rank(-29)==1 && RoadMobs.Rank(-30)==2 && RoadMobs.Rank(-59)==2 && RoadMobs.Rank(-60)==3 && RoadMobs.Rank(-200)==3,"Honour picks the soldiers' rank");
        int last=0;
        for(int r=1;r<=3;r++)
        {
            var (b,w)=RoadMobs.Squad(r);
            Check(b+w>last && RoadMobs.SoldierHealth[r]>RoadMobs.SoldierHealth[r-1] && RoadMobs.SoldierAttack[r]>RoadMobs.SoldierAttack[r-1] && b>0 && w>0,"Higher ranks: more soldiers, better armed, both kinds");
            last=b+w;
        }
        var mobs=combat.Mobs;
        Check(mobs.Alive(9)==5 && mobs.Alive(10)==5 && combat.StationGhosts.Remaining==3 && combat.GraveGhosts.Remaining==3,"Wisps, dueoksini and ghosts mixed on the two monster maps");
        Goto(310); Call(mobs); Call(mobs);
        Check(mobs.SoldiersAlive(9)==0,"No soldiers while honour is not negative");
        combat.Restart();
        q.SacrificeElder();   // −40: the second rank
        Goto(310); Call(mobs);
        foreach(var m in mobs.mobs) if(!m.soldier) m.pos=new Vector3(m.right,0,0);   // the monsters out of the way
        var (blades,bows)=RoadMobs.Squad(2);
        Check(mobs.SoldiersAlive(9)==blades+bows && mobs.ShoutAt>=0,"Negative honour calls a squad, which shouts as it comes");
        Capture("road-station-soldiers",new Vector3(320,1.6f,-10),4.5f);
        RoadMobs.Mob blade=null, bow=null;
        foreach(var m in mobs.mobs) { if(m.soldier && m.kind==RoadMobs.Kind.Blade && blade==null) blade=m; if(m.soldier && m.kind==RoadMobs.Kind.Bow) bow=m; }
        Check(blade.rank==2 && blade.defense==2 && bow!=null,"The squad is of the second rank");
        // A blade soldier draws back, then lunges.
        blade.pos=new Vector3(player.position.x+1.1f,WorldBuilder.Top(player.position.x+1.1f),0); blade.ready=0; blade.windup=-1; blade.strikeUntil=-1;
        bow.pos=new Vector3(player.position.x-20,WorldBuilder.Top(player.position.x-20),0);
        Call(mobs);
        Check(blade.windup>=0 && RoadMobs.Pose(blade)=="ready" && blade.facing==-1,"The blade soldier draws his sword back, facing the seonbi");
        Set(combat,"invulnerableUntil",0f); int before=combat.PlayerHealth;
        blade.windup=Time.time-.01f; Call(mobs);
        Check(RoadMobs.Pose(blade)=="strike","He lunges");
        Call(mobs);
        Check(combat.PlayerHealth==before-RoadMobs.SoldierAttack[2],"The lunge hurts by the rank's attack");
        // Struck by the seonbi: the armour takes some of it.
        int hp=blade.hp; mobs.Slash(blade.Body);
        Check(blade.hp==hp-(Combat.SwordDamage-RoadMobs.SoldierDefense[2]),"Armour takes some of the sword");
        // An archer keeps his distance, draws, and looses at the seonbi.
        bow.pos=new Vector3(player.position.x-6,WorldBuilder.Top(player.position.x-6),0); bow.ready=0; bow.windup=-1; bow.strikeUntil=-1;
        blade.pos=new Vector3(player.position.x+15,0,0);
        Call(mobs);
        Check(bow.windup>=0 && RoadMobs.Pose(bow)=="ready" && bow.facing==1,"The archer draws, facing him");
        bow.windup=Time.time-.01f; Call(mobs);
        Check(Field<IList>(mobs,"arrows").Count==1 && RoadMobs.Pose(bow)=="strike","He looses an arrow");
        // Enough blows put a soldier down; he fades and is gone.
        while(blade.Alive) mobs.Hit(blade,Combat.SwordDamage);
        Check(RoadMobs.Pose(blade)=="down" && mobs.SoldiersAlive(9)==blades+bows-1,"A soldier falls");
        Capture("road-soldier-down",new Vector3(player.position.x,1.4f,-10),3);
        // A wisp gathers its light and swoops; a dueoksini raises its fists and smashes down.
        combat.Restart(); Goto(310);
        RoadMobs.Mob wisp=null, brute=null;
        foreach(var m in mobs.mobs) { if(m.kind==RoadMobs.Kind.Wisp && m.map==9 && wisp==null) wisp=m; if(m.kind==RoadMobs.Kind.Brute && m.map==9 && brute==null) brute=m; }
        foreach(var m in mobs.mobs) if(m!=wisp && m!=brute) m.pos=new Vector3(m.home+60,0,0);
        wisp.pos=player.position+new Vector3(2,1.2f,0); wisp.ready=0;
        brute.pos=new Vector3(player.position.x-1.2f,WorldBuilder.Top(player.position.x-1.2f),0); brute.ready=0;
        Call(mobs);
        Check(wisp.windup>=0 && brute.windup>=0 && RoadMobs.Pose(brute)=="ready","The wisp gathers, the dueoksini raises its fists");
        Set(combat,"invulnerableUntil",0f); before=combat.PlayerHealth;
        brute.windup=Time.time-.01f; wisp.windup=Time.time-.01f; Call(mobs);
        Check(RoadMobs.Pose(brute)=="slam" && RoadMobs.Pose(wisp)=="dash" && combat.PlayerHealth==before-RoadMobs.BruteAttack,"The smash lands; the wisp streaks at him");
        Capture("road-station-monsters",new Vector3(player.position.x,1.6f,-10),3.5f);

        // ── The beacon fortress ──
        combat.Restart();
        var fort=combat.Fort; var gate=Field<GameObject>(fort,"gate");
        Check(!fort.Open && gate.activeSelf && BeaconFort.Order.Length==BeaconFort.All.Length,"The fortress gate is barred");
        Goto(BeaconFort.BoardX); Check(fort.NearBoard && fort.HasSomethingHere,"The orders board can be read");
        Check(!fort.Light(BeaconFort.Order[1]) && fort.Step==0 && fort.WrongAt>=0,"A beacon out of turn puts them all out");
        Check(fort.Light(BeaconFort.Order[0]) && fort.Light(BeaconFort.Order[1]) && fort.Step==2 && fort.IsLit(BeaconFort.Order[0]),"Beacons in turn stay lit");
        Check(!fort.Light(BeaconFort.Order[3]) && fort.Step==0 && !fort.IsLit(BeaconFort.Order[0]),"A wrong one later still puts them all out");
        foreach(int i in BeaconFort.Order) fort.Light(i);
        Check(fort.Open && !gate.activeSelf && fort.OpenedAt>=0,"All five in turn open the gate");
        Capture("road-fort",new Vector3(268,2.2f,-10),5.5f);
        foreach(var b in BeaconFort.All) { Goto(b.x,b.top+.8f); Check(fort.Near()>=0,"Each beacon can be reached at "+b.place); }
        // The towers and Hanyang's people stand in the world, not under the camera that carries the combat object.
        var cam=Camera.main.transform; var was=cam.position; cam.position+=Vector3.right*40;
        Check(Mathf.Abs(GameObject.Find("봉수대 마른 우물 곁").transform.position.x-BeaconFort.All[3].x)<.01f
            && Mathf.Abs(GameObject.Find("무당").transform.position.x-HanyangTown.People[System.Array.FindIndex(HanyangTown.People,p=>p.who==HanyangTown.Who.Mudang)].x)<.01f,"Beacons and townsfolk stay put when the camera moves");
        cam.position=was;

        // ── Hanyang's people ──
        combat.Restart();
        var town=combat.Town;
        foreach(HanyangTown.Who who in System.Enum.GetValues(typeof(HanyangTown.Who)))
            Check(HanyangTown.Lines(who,false,false).Length>0 && HanyangTown.Lines(who,true,false).Length>0,"Everyone has something to say: "+who);
        Check(string.Join("",HanyangTown.Lines(HanyangTown.Who.Mudang,false,false)).Contains("업 많은 선비가 네 목을 치리라"),"The shaman tells the prophecy");
        int steward=System.Array.FindIndex(HanyangTown.People,p=>p.who==HanyangTown.Who.Cheongji);
        Goto(HanyangTown.People[steward].x-.5f);
        Check(town.Near()==steward,"The steward can be spoken to");
        town.Talk(steward);
        Check(town.IsOpen && combat.Paused && !combat.player.CanMove,"Talking holds the game");
        Capture("road-city",new Vector3(410,1.6f,-10),5f);
        while(town.IsOpen) town.Next();
        Check(town.Invited && combat.player.CanMove,"The steward invites him to the councillor's house");
        Capture("road-hall",new Vector3(448,1.6f,-10),5f);

        // ── The councillor ──
        var boss=combat.FinalBoss;
        Check(boss.Now==FinalBoss.Phase.Waiting,"The councillor waits in his pavilion");
        var good=FinalBoss.BanquetLines(false,true,true); var bad=FinalBoss.BanquetLines(true,false,false);
        var goodThought=System.Array.Find(good,l=>l.thought && l.who==FinalBoss.Official); var badThought=System.Array.Find(bad,l=>l.thought && l.who==FinalBoss.Official);
        Check(good[good.Length-1].said==FinalBoss.LastLine && bad[bad.Length-1].said==FinalBoss.LastLine && goodThought.said.Contains("선업") && badThought.said.Contains("밥그릇"),"He fears his good karma, or his greed and his own rice bowl");
        Check(System.Array.Exists(good,l=>l.said.Contains("열두 살")) && System.Array.Exists(good,l=>l.said.Contains("망령들에게")) && System.Array.Exists(good,l=>l.said.Contains("도깨비")),"He speaks of the boy king, of the face he sold to the old kingdom's ghosts, and of the curses on the road");
        bool sensed=false; foreach(var l in good) if(l.who==FinalBoss.Seonbi && l.said.Contains("눈코입")) sensed=true;
        Check(sensed,"The seonbi sees the faceless reflection in his cup");
        Goto(WorldBuilder.OfficialX-5); Call(boss);
        Check(boss.Now==FinalBoss.Phase.Waiting,"Not yet: the banquet opens at his table");
        Goto(WorldBuilder.OfficialX-3); Call(boss);
        Check(boss.Now==FinalBoss.Phase.Banquet && combat.Paused && boss.Cutscene && combat.CanRetryOnRoad,"Coming to his table opens the banquet, and saves the road");
        Capture("road-manor-banquet",new Vector3(WorldBuilder.OfficialX-2.5f,1.8f,-10),3.5f);
        while(boss.Now==FinalBoss.Phase.Banquet) boss.NextLine();
        Check(boss.Now==FinalBoss.Phase.Changing,"The last line starts the change");
        foreach(var (t,name) in new[]{(.3f,"official_idle"),(.9f,"official_blank")})
        {
            Set(boss,"<ChangedAt>k__BackingField",Time.time-t); Call(boss,"LateUpdate");
            Check(Field<SpriteRenderer>(boss,"human").enabled && Field<SpriteRenderer>(boss,"human").sprite.name.EndsWith(name),"Change frame "+name);
        }
        Set(boss,"<ChangedAt>k__BackingField",Time.time-FinalBoss.ChangeLength-.01f); Call(boss);
        Check(boss.Fighting && !combat.Paused && combat.player.CanMove,"Then he fights");
        Check(mobs.SoldiersAlive(13)==0,"Honour not negative: the soldiers do not join him");
        int witnesses=0; foreach(var m in mobs.mobs) if(m.witness && m.map==13) witnesses++;
        Check(witnesses==2 && mobs.Shout==FinalBoss.SoldiersStay,"…they come, say no such seonbi could be a traitor, and stand aside");
        Call(boss,"LateUpdate");
        Check(Field<SpriteRenderer>(boss,"picture").enabled && Field<SpriteRenderer>(boss,"picture").sprite.name.StartsWith("25_Final/final_"),"The eodugsini councillor is drawn");
        Capture("road-final-fight",new Vector3(WorldBuilder.OfficialX-2.5f,2.4f,-10),4.5f);
        // His moves land on the seonbi.
        Set(boss,"nextMoveAt",0f); Set(combat,"invulnerableUntil",0f);
        Goto(boss.Feet.x-2.5f); Call(boss);
        Check(boss.Doing!=FinalBoss.Move.None && boss.FightPose()!="","He picks a move");
        Set(boss,"<Doing>k__BackingField",FinalBoss.Move.Sweep); Set(boss,"<MoveAt>k__BackingField",Time.time-FinalBoss.Windup(FinalBoss.Move.Sweep)-.01f); Set(boss,"struck",false);
        before=combat.PlayerHealth; Call(boss);
        Check(combat.PlayerHealth==before-FinalBoss.SweepAttack && boss.FightPose()=="reach","The sweep along the ground hits a grounded seonbi");
        Call(boss,"LateUpdate"); Capture("road-final-sweep",new Vector3(boss.Feet.x-2,2,-10),4f);
        // Half his health: darkness, and he grows.
        Check(boss.Damage(FinalBoss.MaxHealth/2) && boss.Dark && boss.Size>1,"At half health the dark deepens and he grows");
        int honour=q.Honor;
        Check(boss.Damage(FinalBoss.MaxHealth) && boss.Resolved && q.FinalOutcome==EncounterOutcome.Slain && q.Honor==honour+10,"Struck down: +10");
        Set(boss,"<DownAt>k__BackingField",Time.time-1.3f); Call(boss,"LateUpdate");
        Check(Field<SpriteRenderer>(boss,"picture").sprite.name.EndsWith("final_death_01"),"He crumbles into the dark");
        Set(boss,"<DownAt>k__BackingField",Time.time-FinalBoss.DeathLength-.01f); Call(boss,"LateUpdate");
        Check(!Field<SpriteRenderer>(boss,"picture").enabled && Field<SpriteRenderer>(boss,"human").enabled && Field<SpriteRenderer>(boss,"human").sprite.name.EndsWith("official_down"),"…and the faceless man is left lying");
        Capture("road-final-corpse",new Vector3(boss.Feet.x,1.4f,-10),3f);
        Check(!combat.CanRetryOnRoad,"No road checkpoint once he is gone");

        // Negative honour: his change calls the soldiers in.
        combat.Restart(); q.SacrificeElder();
        Goto(WorldBuilder.OfficialX-3); Call(boss);
        while(boss.Now==FinalBoss.Phase.Banquet) boss.NextLine();
        Set(boss,"<ChangedAt>k__BackingField",Time.time-FinalBoss.ChangeLength-.01f); Call(boss);
        var (b2,w2)=RoadMobs.Squad(RoadMobs.Rank(q.Honor));
        Check(boss.Fighting && mobs.SoldiersAlive(13)==b2+w2 && mobs.Shout==FinalBoss.SoldiersCome,"Negative honour: the soldiers say they knew it, and attack him");
        boss.Damage(FinalBoss.MaxHealth/2);
        Check(mobs.SoldiersAlive(13)==2*(b2+w2),"At half health another squad comes");
        Capture("road-final-soldiers",new Vector3(WorldBuilder.OfficialX-4,2.2f,-10),5f);
        boss.Damage(FinalBoss.MaxHealth);
        Check(mobs.SoldiersAlive(13)==0,"When he falls the soldiers withdraw");

        // The road checkpoint: struck down on the road, he rises at the last place it saved, honour as it was.
        combat.Restart();
        Set(combat.KongBoss,"<Now>k__BackingField",KongBoss.Phase.Slain);
        Goto(WorldBuilder.FortStart+3); Call(mobs);
        Check(combat.CanRetryOnRoad && Mathf.Abs(combat.RoadCheckpoint.Value.at.x-(WorldBuilder.FortStart+1.5f))<.01f,"Entering the fortress saves the road");
        Set(combat,"<PlayerHealth>k__BackingField",1); Set(combat,"invulnerableUntil",0f); combat.HurtPlayer();
        Check(combat.PlayerHealth==0 && combat.CanRetryOnRoad,"Struck down on the road");
        combat.RestartOnRoad();
        Check(combat.PlayerHealth==Combat.MaxPlayerHealth && Mathf.Abs(player.position.x-(WorldBuilder.FortStart+1.5f))<.01f && combat.Maps.Current==8,"He rises at the fortress");

        // The developer's F10 and F11.
        combat.Restart();
        Check(combat.Cheats.SkipToRoad(false) && q.Honor==80 && combat.KongBoss.Resolved && combat.BossPurified && combat.Maps.Current==8,"F10: the good road to the fortress");
        combat.Restart();
        Check(combat.Cheats.SkipToRoad(true) && q.Honor==-60 && RoadMobs.Rank(q.Honor)==3 && combat.KongBoss.Resolved && combat.EnemyDefeated && combat.Maps.Current==9,"F11: the wicked road to the station");
        Call(mobs);
        Check(mobs.SoldiersAlive(9)==RoadMobs.Squad(3).blades+RoadMobs.Squad(3).bows,"…where the third rank waits");
        Capture("road-soldiers-rank3",new Vector3(player.position.x+6,1.6f,-10),4.5f);

        // Endings at the examination hall, by honour.
        Check(EndingNarration.VerdictFor(new QuestProgress())==EndingNarration.Verdict.None,"No verdict before the councillor");
        var sword=new QuestProgress(); sword.ResolveFinal();
        var failed=new QuestProgress(); failed.SacrificeElder(); failed.ResolveFinal();
        Check(EndingNarration.VerdictFor(sword)==EndingNarration.Verdict.Sword && EndingNarration.VerdictFor(failed)==EndingNarration.Verdict.Failed,"Honour picks the verdict");
        Check(EndingNarration.Finale(EndingNarration.Verdict.Failed,true).Length==4 && EndingNarration.Finale(EndingNarration.Verdict.Loyal,false).Length==3,"Losing someone adds a page");
        combat.Restart();
    }

    // Shops beyond the first village, and what each monster is called when it falls.
    static void RoadShops()
    {
        combat.Restart();
        var q=combat.Quest.progress; var tavern=combat.Tavern; var peddler=combat.Peddler;
        // Kongbat's pedlar at the village gate, Hanyang's merchant and gukbap house.
        Goto(WorldBuilder.Bangmul.x);
        Check(peddler.NearStall()==PeddlerShop.Kongbat && !tavern.Near,"The pedlar stands at Kongbat's gate");
        Goto(WorldBuilder.Sangin.x);
        Check(peddler.NearStall()==PeddlerShop.Hanyang && combat.Town.Near()<0,"Hanyang's merchant keeps a shop, not a chat");
        Goto(WorldBuilder.Gukbap.x);
        Check(tavern.NearStall()==1 && combat.Town.Near()<0,"Hanyang's gukbap house");
        Check(GameObject.Find("방물장수")!=null && GameObject.Find("상인")!=null && GameObject.Find("국밥집 주모")!=null,"The shopkeepers are drawn");
        // The first village's mourning keeps to the first village: Hanyang still sells soup and bundles.
        q.SacrificeElder();
        tavern.OpenAt(1);
        Check(tavern.IsOpen && combat.Paused,"The gukbap house opens and holds the game");
        Set(combat,"<PlayerHealth>k__BackingField",10); int coins=tavern.wallet.Coins; tavern.Purchase(1);
        Check(combat.PlayerHealth==90 && tavern.wallet.Coins<coins,"Gukbap is sold in Hanyang even while the first village mourns");
        Call(tavern,"Close");
        combat.AddArrows(-15); peddler.OpenAt(PeddlerShop.Hanyang); peddler.Purchase(true);
        Check(combat.Arrows==5+ShopRules.BundleSize,"Bundles too");
        Call(peddler,"Close");
        Check(!combat.Paused,"Closed");
        combat.Restart();
        // While the Kongjwi demon fights, the pedlar has fled.
        Set(combat.KongBoss,"<Now>k__BackingField",KongBoss.Phase.Fighting);
        Goto(WorldBuilder.Bangmul.x);
        Check(peddler.NearStall()<0,"The pedlar is gone during the fight");
        combat.Restart();

        // Names over the health bars and in the note when they fall.
        Check(combat.Camp.Name=="망령" && combat.River.Name=="물귀신" && combat.StationGhosts.Name=="떠도는 망령","Ghosts have names");
        var mobs=combat.Mobs; RoadMobs.Mob wisp=null, brute=null;
        foreach(var m in mobs.mobs) { if(m.kind==RoadMobs.Kind.Wisp && wisp==null) wisp=m; if(m.kind==RoadMobs.Kind.Brute && brute==null) brute=m; }
        Check(RoadMobs.Name(wisp)=="도깨비불" && RoadMobs.Name(brute)=="두억시니","The new monsters have names");
        while(wisp.Alive) mobs.Hit(wisp,Combat.SwordDamage);
        Check(string.Format(combat.CoinsNote,combat.CoinsFound).StartsWith("도깨비불이 꺼지며 엽전 2문"),"A wisp goes out");
        combat.Camp.Hit(combat.Camp.mobs[0],Camp.GhostHealth);
        Check(combat.CoinsNote==Combat.DefaultCoinsNote && combat.CoinsFound>=1 && combat.CoinsFound<=10,"A ghost scatters 1~10 coins");
        q.SacrificeElder(); Goto(310); Call(mobs);
        RoadMobs.Mob soldier=null; foreach(var m in mobs.mobs) if(m.soldier) { soldier=m; break; }
        while(soldier.Alive) mobs.Hit(soldier,Combat.SwordDamage);
        Check(string.Format(combat.CoinsNote,combat.CoinsFound)=="관군 칼병이 죽었습니다. 품에서 엽전 3문이 나왔습니다.","A soldier dies, and the note says so");
        combat.Restart();
    }

    // The developer's cheat window (F8): it holds the game while open, and each number does its one thing.
    static void CheatPanel()
    {
        combat.Restart();
        var cheats=combat.Cheats; var q=combat.Quest.progress; var mobs=combat.Mobs;
        cheats.Show();
        Check(cheats.Open && combat.Paused && !combat.player.CanMove && cheats.Lines().Length>=12,"F8 opens the cheat window and holds the game");
        cheats.Close();
        Check(!cheats.Open && !combat.Paused && combat.player.CanMove,"Closing it lets him move");
        // 1: nothing hurts him.
        cheats.Use(1); Set(combat,"invulnerableUntil",0f); combat.HurtPlayer(50);
        Check(cheats.God && combat.PlayerHealth==Combat.MaxPlayerHealth,"God mode: no blow lands");
        cheats.Use(1); combat.HurtPlayer(50);
        Check(!cheats.God && combat.PlayerHealth==Combat.MaxPlayerHealth-50,"Turned off, blows land again");
        // 3: full health, stamina and arrows.
        combat.player.UseStamina(40); combat.AddArrows(-10); cheats.Use(3);
        Check(combat.PlayerHealth==Combat.MaxPlayerHealth && combat.player.Stamina==combat.player.MaxStamina && combat.Arrows==ShopRules.MaxArrows,"Refilled");
        // 4: coins. 5, 6: honour, which moves the soldiers' rank.
        int coins=combat.Tavern.wallet.Coins; cheats.Use(4);
        Check(combat.Tavern.wallet.Coins==coins+DevCheats.CoinGift,"Coins given");
        cheats.Use(6); cheats.Use(6); cheats.Use(6);
        Check(q.Honor==-30 && RoadMobs.Rank(q.Honor)==2,"Honour down: soldiers of the second rank");
        cheats.Use(5); cheats.Use(5); cheats.Use(5);
        Check(q.Honor==0,"Honour back up");
        // 7, 8: one map left or right.
        int here=combat.Maps.Current; cheats.Use(8);
        Check(combat.Maps.Current==here+1 && !combat.Maps.Travelling,"Next map");
        cheats.Use(7);
        Check(combat.Maps.Current==here,"Previous map");
        // 2: one hit fells anything.
        cheats.GoMap(9);
        RoadMobs.Mob brute=null; foreach(var m in mobs.mobs) if(m.kind==RoadMobs.Kind.Brute && m.map==9) { brute=m; break; }
        cheats.Use(2); mobs.Hit(brute,1);
        Check(cheats.OneHit && !brute.Alive,"One hit fells a dueoksini");
        cheats.Use(2);
        // 9: everything on this map.
        cheats.Use(9);
        Check(mobs.Alive(9)==0 && combat.StationGhosts.Remaining==0,"The station's monsters and ghosts all fall");
        // 0: the fortress opens.
        cheats.Use(0);
        Check(combat.Fort.Open,"The beacon fortress is solved");
        // F12: straight to the councillor's house.
        Check(cheats.SkipToManor() && combat.Maps.Current==13 && combat.FinalBoss.Now==FinalBoss.Phase.Waiting,"F12: to the councillor's gate");
        Check(!cheats.God && !cheats.OneHit,"Toggles left off for the checks after");
        combat.Restart();
    }

    // Puts the seonbi at `x` on the ground there (or at `y`), on that map with no fade.
    static void Goto(float x,float y=float.NaN)
    {
        var at=new Vector2(x,float.IsNaN(y) ? WorldBuilder.Top(x)+.8f : y);
        combat.player.RespawnAt(at);
        var body=combat.player.GetComponent<Rigidbody2D>(); if(body!=null) { body.velocity=Vector2.zero; body.position=at; }
        combat.Maps.Jump();
    }
}
