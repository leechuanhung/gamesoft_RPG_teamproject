using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class JinaPlayChecks
{
    const string Key="JinaPlayChecks.Active";
    static int stage;
    static double due;
    static PrototypeCombat combat;
    static int hp;
    static JinaPlayChecks() { EditorApplication.update += Tick; }
    public static void Begin()
    {
        SessionState.SetBool(Key,true);
        EditorSceneManager.OpenScene("Assets/Scenes/MovementTest.unity");
        EditorApplication.isPlaying=true;
    }
    static T Field<T>(object obj,string name) => (T)obj.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(obj);
    static void Set(object obj,string name,object value) => obj.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(obj,value);
    static void Check(bool ok,string reason) { if(!ok) throw new Exception(reason); }
    static void Tick()
    {
        if(!SessionState.GetBool(Key,false) || !EditorApplication.isPlaying) return;
        try
        {
            if(stage==0) { due=EditorApplication.timeSinceStartup+1; stage=1; return; }
            if(EditorApplication.timeSinceStartup<due) return;
            combat=UnityEngine.Object.FindObjectOfType<PrototypeCombat>();
            Check(combat!=null,"Combat scene started");
            var boss=Field<SpriteRenderer>(combat,"enemy");
            if(stage==1)
            {
                Check(combat.Intro!=null && combat.Intro.IsPlaying && combat.Paused && !combat.player.CanMove,"Intro plays first and holds the game");
                combat.Intro.Skip();
                Check(!combat.Intro.IsPlaying && !combat.Paused && combat.player.CanMove,"Skipping the intro hands control back");
                foreach(string p in new[]{JinaArt.Player,JinaArt.Boss,JinaArt.Purified,JinaArt.Ghost,JinaArt.Elder,JinaArt.Peddler,"04_Weapons/arrow","04_Weapons/bow","04_Weapons/hwando"}) Check(JinaArt.Load(p)!=null,"Load "+p);
                foreach(string prefix in new[]{"02_Player_Attack_Sword/sword_attack_","03_Player_Attack_Bow/bow_attack_","05_Boss_Brainwashed/boss_attack_","09_Player_Jump/jump_","10_Player_Walk/player_walk_","11_Player_Run/player_run_","05_Boss_Brainwashed/boss_walk_"}) Check(JinaArt.Frames(prefix).Length==4,"Animation frames");
                Capture("village",new Vector3(-1,1,-10),3);
                Capture("elder-peddler",new Vector3(PrototypeWorld.Elder.x-2,1,-10),3);
                Check(combat.Peddler!=null && combat.Arrows==ShopRules.MaxArrows,"Peddler placed and quiver full");
                Capture("tavern-peddler",new Vector3(PrototypeWorld.Peddler.x-3,1,-10),3);
                Capture("props-village",new Vector3(8,1.8f,-10),5);
                Capture("props-forest",new Vector3(42,2.6f,-10),5.5f);
                Capture("props-stele",new Vector3(PrototypeWorld.Stone.x,1,-10),2.5f);
                var visual=combat.player.GetComponent<SeonbiVisual>();
                visual.ResetPose();
                Set(combat.player,"<IsGrounded>k__BackingField",false);
                var rb=combat.player.GetComponent<Rigidbody2D>();
                rb.velocity=Vector2.up*5;
                Set(visual,"airTime",.2f); visual.SendMessage("LateUpdate");
                Check(Field<SpriteRenderer>(visual,"body").sprite.name.EndsWith("jump_01"),"Ascending jump frame");
                Capture("jump-rise",new Vector3(0,1,-10),2);
                rb.velocity=Vector2.down*5; visual.SendMessage("LateUpdate");
                Check(Field<SpriteRenderer>(visual,"body").sprite.name.EndsWith("jump_02"),"Falling jump frame");
                Set(combat.player,"<IsGrounded>k__BackingField",true);
                rb.velocity=Vector2.zero; visual.SendMessage("LateUpdate");
                Check(Field<SpriteRenderer>(visual,"body").sprite.name.EndsWith("jump_03"),"Landing jump frame");
                visual.ResetPose(); visual.SendMessage("LateUpdate");
                Check(Field<SpriteRenderer>(visual,"body").sprite.name==JinaArt.Player,"Idle after landing");
                rb.velocity=Vector2.right*3;
                for(int i=0;i<4;i++)
                {
                    Set(visual,"walkTime",i*.12f); visual.SendMessage("LateUpdate");
                    Check(Field<SpriteRenderer>(visual,"body").sprite.name.Contains("player_walk_"),"Player walks while moving");
                    Capture("player-walk-"+i,new Vector3(0,1,-10),2);
                }
                Set(combat.player,"<IsRunning>k__BackingField",true); rb.velocity=Vector2.right*combat.player.runSpeed;
                for(int i=0;i<4;i++)
                {
                    Set(visual,"walkTime",i*.08f+.01f); visual.SendMessage("LateUpdate");
                    Check(Field<SpriteRenderer>(visual,"body").sprite.name.EndsWith("player_run_"+i.ToString("00")),"Player runs while sprinting");
                    Capture("player-run-"+i,new Vector3(0,1,-10),2);
                }
                Set(combat.player,"<IsRunning>k__BackingField",false);
                rb.velocity=Vector2.zero; visual.SendMessage("LateUpdate");
                Check(Field<SpriteRenderer>(visual,"body").sprite.name==JinaArt.Player,"Stop restores idle");
                for(int i=0;i<4;i++)
                {
                    visual.Attack(false,1); Set(visual,"attackTime",i*.1f+.01f); visual.SendMessage("LateUpdate");
                    Capture("sword-"+i,new Vector3(0,1,-10),2);
                    visual.Attack(true,1); Set(visual,"attackTime",i*.15f+.01f); visual.SendMessage("LateUpdate");
                    Capture("bow-"+i,new Vector3(0,1,-10),2);
                }
                Set(combat,"<UsingBow>k__BackingField",true);
                visual.ResetPose(); visual.SendMessage("LateUpdate");
                Check(Mathf.Abs(Field<SpriteRenderer>(visual,"weapon").bounds.size.y-JinaArt.BowHeight)<.01f,"Equipped bow matches attack bow height");
                Capture("bow-equipped",new Vector3(0,1,-10),2);
                foreach(int direction in new[]{1,-1})
                {
                    visual.Attack(true,direction); Set(visual,"attackTime",.31f); visual.SendMessage("LateUpdate");
                    Set(combat,"pendingBow",true); Set(combat,"attackFacing",direction); Set(combat,"playerImpactAt",Time.time);
                    combat.SendMessage("Update");
                    var shot=GameObject.Find("Arrow").GetComponent<SpriteRenderer>();
                    float expectedY=combat.player.GetComponent<SpriteRenderer>().bounds.min.y+312f/(510f/1.9f);
                    Check(Mathf.Abs(shot.transform.position.y-expectedY)<.01f,"Arrow matches drawn nock height in direction "+direction);
                    Check(shot.flipX==(direction<0),"Arrow faces shot direction");
                    Capture("bow-release-"+(direction>0 ? "right" : "left"),new Vector3(0,1,-10),2);
                    shot.enabled=false; shot.gameObject.name="Checked Arrow";
                    typeof(PrototypeCombat).GetMethod("ClearArrows",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(combat,null);
                }
                Set(combat,"<UsingBow>k__BackingField",false);
                var bossVisual=boss.GetComponent<JinaActorVisual>();
                for(int i=0;i<4;i++)
                {
                    Set(bossVisual,"last",boss.transform.position-Vector3.right*.1f);
                    Set(bossVisual,"walkTime",i*.14f); bossVisual.Refresh();
                    Check(Field<SpriteRenderer>(bossVisual,"picture").sprite.name.Contains("boss_walk_"),"Boss walks while moving");
                    Capture("boss-walk-"+i,new Vector3(boss.transform.position.x,1,-10),3);
                }
                bossVisual.Refresh();
                Check(Field<SpriteRenderer>(bossVisual,"picture").sprite.name==JinaArt.Boss,"Boss stops to idle");
                visual.Attack(false,1); Set(visual,"attackTime",.21f);
                visual.SendMessage("LateUpdate");
                Capture("player-sword",new Vector3(0,1,-10),2);
                visual.Attack(true,1); Set(visual,"attackTime",.31f);
                visual.SendMessage("LateUpdate");
                Capture("player-bow",new Vector3(0,1,-10),2);
                for(int i=0;i<4;i++)
                {
                    Set(combat,"windupUntil",i<2 ? Time.time+(i==0 ? .5f : .2f) : -1f);
                    Set(combat,"bossRecoveryUntil",i>=2 ? Time.time+(i==2 ? .2f : .05f) : 0f);
                    bossVisual.Refresh();
                    Check(Field<SpriteRenderer>(bossVisual,"picture").sprite.name.EndsWith("boss_attack_0"+i),"Boss attack phase "+i);
                    Capture("boss-attack-"+i,new Vector3(boss.transform.position.x,1,-10),3);
                }
                Set(combat,"windupUntil",Time.time+.2f);
                boss.GetComponent<JinaActorVisual>().Refresh();
                Capture("boss-windup",new Vector3(boss.transform.position.x,1,-10),3);
                Set(combat,"windupUntil",-1f); Set(combat,"bossRecoveryUntil",0f);
                Set(combat,"<EnemyHealth>k__BackingField",0);
                float[] deathTimes={.1f,.3f,.8f,1.2f,1.5f};
                for(int i=0;i<5;i++)
                {
                    Set(combat,"<BossDownAt>k__BackingField",Time.time-deathTimes[i]); bossVisual.Refresh();
                    Check(Field<SpriteRenderer>(bossVisual,"picture").sprite.name.EndsWith("boss_death_0"+i),"Boss death frame "+i);
                    Capture("boss-death-"+i,new Vector3(boss.transform.position.x,1,-10),3);
                }
                Set(combat,"<BossDownAt>k__BackingField",Time.time-JinaActorVisual.BossDeathLength); bossVisual.Refresh();
                Check(boss.enabled && !Field<SpriteRenderer>(bossVisual,"picture").enabled,"Boss vanishes after the death motion");
                Set(combat,"<EnemyHealth>k__BackingField",30); Set(combat,"<BossDownAt>k__BackingField",-1f); bossVisual.Refresh();
                Check(Field<SpriteRenderer>(bossVisual,"picture").enabled,"Boss visible again while alive");
                combat.Quest.progress.HearClue(); combat.Quest.progress.ChooseSymbol(2); combat.Quest.progress.ChooseSymbol(1); combat.Quest.progress.ChooseSymbol(3);
                Set(combat.Quest,"charmReveal",0f);
                Check(combat.Quest.RevealingCharm,"Charm reveal plays");
                Set(combat.Quest,"charmReveal",-1f);
                visual.ResetPose(); visual.SendMessage("LateUpdate");
                boss.transform.position=new Vector3(combat.player.transform.position.x+2.5f,boss.transform.position.y,0);
                hp=combat.PlayerHealth;
                Check(combat.TryPurifyBoss(),"Purification succeeds");
                Check(combat.BossPurified && combat.EnemyDefeated && boss.enabled,"Friendly boss remains visible and resolves encounter");
                Check(boss.GetComponentInChildren<SpriteRenderer>().enabled,"Boss renderer enabled");
                typeof(PrototypeCombat).GetMethod("DamageEnemy",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(combat,new object[]{5});
                Check(combat.EnemyHealth==30,"Purified boss ignores damage");
                Check(!combat.TryPurifyBoss() && combat.Quest.progress.Honor==30,"No duplicate reward");
                foreach(float t in new[]{.3f,.65f,.8f,1.2f})
                {
                    Set(combat,"<PurifiedAt>k__BackingField",Time.time-t); bossVisual.Refresh();
                    Check(Field<SpriteRenderer>(bossVisual,"picture").sprite.name==(t<.7f ? JinaArt.Boss : JinaArt.Purified),"Purify motion swaps the form at its flash");
                    Check(Field<SpriteRenderer>(bossVisual,"glow").enabled,"Purify flash shows");
                    Check(Field<SpriteRenderer>(bossVisual,"charm").enabled==(t<.9f),"Charm shows while the curse lifts");
                    Capture("purify-"+t.ToString("0.00"),new Vector3(boss.transform.position.x,1,-10),3);
                }
                Set(combat,"<PurifiedAt>k__BackingField",Time.time-JinaActorVisual.PurifyLength); bossVisual.Refresh();
                Check(!Field<SpriteRenderer>(bossVisual,"glow").enabled && !Field<SpriteRenderer>(bossVisual,"charm").enabled,"Purify motion ends cleanly");
                Capture("purified",new Vector3(.5f,1,-10),3);
                stage=2; due=EditorApplication.timeSinceStartup+1.2;
            }
            else
            {
                Check(combat.PlayerHealth==hp && !combat.Quest.progress.ElderDead,"Purified boss never harms player or elder");
                combat.Restart();
                var elderVisual=GameObject.Find("Village Elder").GetComponent<JinaActorVisual>();
                // Struck after the alarm, sword in hand, with the boss on his right.
                Set(combat.Village,"<Alarmed>k__BackingField",true); Set(combat.Village,"<AlarmAt>k__BackingField",Time.time-5);
                boss.transform.position=new Vector3(PrototypeWorld.Elder.x+1.5f,boss.transform.position.y,0);
                combat.Quest.progress.SacrificeElder(); combat.Quest.ShowElderDeath();
                foreach(float t in new[]{.1f,.3f,.6f,.8f,1f})
                {
                    Set(combat.Quest,"<ElderStruckAt>k__BackingField",Time.time-t); elderVisual.Refresh();
                    var pic=Field<SpriteRenderer>(elderVisual,"picture");
                    Check(t<.45f ? pic.sprite.name.Contains("elder_hit_0"+(t<.2f ? 0 : 1)) : pic.sprite.name.EndsWith(t<JinaActorVisual.ElderFallLength ? "elder_draw_00" : "elder_corpse"),"Elder hit frames, then the fall");
                    Check(t<JinaActorVisual.ElderFallLength || (pic.sprite.name.EndsWith("elder_corpse") && Mathf.Abs(pic.transform.eulerAngles.z)<.01f && pic.color==Color.gray),"Elder ends as the corpse drawing");
                    Capture("elder-fall-"+t.ToString("0.0"),new Vector3(PrototypeWorld.Elder.x,1,-10),2.5f);
                }
                // The corpse stays put: neither the boss walking past nor reading his note turns it.
                var body=Field<SpriteRenderer>(elderVisual,"picture"); bool flipped=body.flipX; var lying=body.transform.position;
                boss.transform.position=new Vector3(PrototypeWorld.Elder.x+(flipped ? 3 : -3),boss.transform.position.y,0);
                combat.player.transform.position=new Vector3(PrototypeWorld.Elder.x+(flipped ? 1.2f : -1.2f),combat.player.transform.position.y,0);
                Set(combat.Quest,"<IsDialogOpen>k__BackingField",true); Set(combat.Quest,"shrineDialog",false); elderVisual.Refresh();
                Check(body.flipX==flipped && body.transform.position==lying,"Corpse does not turn when the note is read or the boss passes");
                Capture("elder-corpse",new Vector3(PrototypeWorld.Elder.x,1,-10),2.5f);
                Set(combat.Quest,"<IsDialogOpen>k__BackingField",false);
                combat.Restart();
                Check(!combat.Quest.progress.ElderDead,"Restart revives the elder");
                Village(elderVisual);
                Check(!combat.BossPurified && !combat.EnemyDefeated && combat.EnemyHealth==30,"Restart restores hostile boss");
                Check(combat.Quest.progress.Honor==0,"Restart resets honor");
                combat.AddArrows(-5); Check(combat.Arrows==15,"Arrows spent");
                combat.Restart(); Check(combat.Arrows==ShopRules.MaxArrows,"Restart refills quiver");
                Debug.Log("JINA_PLAY_CHECKS_PASSED: v6 assets, attack/jump/walk frames, boss death, purify and elder fall motions, talk facing, alarm, evacuation and return, movement/idle transitions, purification visibility, immunity, no damage, reset");
                SessionState.SetBool(Key,false); EditorApplication.Exit(0);
            }
        }
        catch(Exception e) { Debug.LogException(e); SessionState.SetBool(Key,false); EditorApplication.Exit(1); }
    }
    // Talking turns villagers to the player; the boss entering the village sets off the alarm and the evacuation.
    static void Village(JinaActorVisual elderVisual)
    {
        var village=combat.Village; var player=combat.player.transform;
        Func<JinaActorVisual,SpriteRenderer> pic=v=>Field<SpriteRenderer>(v,"picture");
        var peddlerVisual=village.Peddler.art.GetComponent<JinaActorVisual>();
        var keeper=village.Keeper.art;
        foreach(float side in new[]{-1.2f,1.2f})
        {
            player.position=new Vector3(PrototypeWorld.Elder.x+side,player.position.y,0);
            Set(combat.Quest,"<IsDialogOpen>k__BackingField",true); Set(combat.Quest,"shrineDialog",false); elderVisual.Refresh();
            Check(pic(elderVisual).flipX==(side<0),"Elder turns to the player on his "+(side<0 ? "left" : "right"));
            Set(combat.Quest,"<IsDialogOpen>k__BackingField",false);
            player.position=new Vector3(PrototypeWorld.Peddler.x+side,player.position.y,0);
            Set(combat.Peddler,"<IsOpen>k__BackingField",true); peddlerVisual.Refresh();
            Check(pic(peddlerVisual).flipX==(side>0),"Peddler turns to the player");
            Set(combat.Peddler,"<IsOpen>k__BackingField",false);
            player.position=new Vector3(PrototypeWorld.Tavern.x+side,player.position.y,0);
            Set(combat.Tavern,"<IsOpen>k__BackingField",true); village.SendMessage("Update");
            Check(keeper.flipX==(side>0),"Keeper turns to the player");
            Set(combat.Tavern,"<IsOpen>k__BackingField",false);
        }
        elderVisual.Refresh(); peddlerVisual.Refresh(); village.SendMessage("Update");
        Check(!pic(elderVisual).flipX && !pic(peddlerVisual).flipX && !keeper.flipX,"Villagers face their usual way after talking");
        player.position=new Vector3(0,player.position.y,0);

        var boss=Field<SpriteRenderer>(combat,"enemy");
        boss.transform.position=new Vector3(PrototypeWorld.Elder.x+4,boss.transform.position.y,0);
        Set(combat,"alerted",true);
        village.SendMessage("Update");
        Check(village.Alarmed && !village.Cleared,"Boss in the village raises the alarm");
        var sword=Field<SpriteRenderer>(elderVisual,"sword");
        foreach(float t in new[]{.1f,.4f,.6f,1f})
        {
            Set(village,"<AlarmAt>k__BackingField",Time.time-t); elderVisual.Refresh();
            string expect=t<.3f ? "elder_startle" : t<.55f ? "elder_draw_00" : "elder_draw_01";
            Check(pic(elderVisual).sprite.name.EndsWith(expect),"Elder alarm frame "+expect);
            Check(sword.enabled==(t>=.3f),"Sword drawn after the startle");
            Capture("elder-alarm-"+t.ToString("0.0"),new Vector3(PrototypeWorld.Elder.x+.5f,1,-10),2);
        }
        village.SendMessage("Update");
        Check(village.Keeper.state==PrototypeVillage.State.Fleeing && village.Peddler.state==PrototypeVillage.State.Fleeing,"Villagers flee");
        village.Keeper.art.transform.position=new Vector3(7,-.5f,0);
        village.Peddler.art.transform.position=new Vector3(9,PrototypeWorld.Peddler.y,0);
        typeof(PrototypeVillage).GetMethod("DrawKeeper",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(village,new object[]{-.05f});
        Check(keeper.sprite.name.Contains("keeper_run_") && !keeper.flipX,"Keeper runs left");
        Set(peddlerVisual,"last",village.Peddler.art.transform.position+Vector3.right*.06f); peddlerVisual.Refresh();
        Check(pic(peddlerVisual).sprite.name.Contains("peddler_run_") && !pic(peddlerVisual).flipX,"Peddler runs left");
        Capture("villagers-flee",new Vector3(7,1,-10),3);
        player.position=new Vector3(PrototypeWorld.Tavern.x,player.position.y,0);
        Check(!combat.Tavern.Near,"No tavern while the keeper is away");
        player.position=new Vector3(0,player.position.y,0);
        village.Keeper.art.transform.position=new Vector3(PrototypeVillage.Shelter,-.5f,0);
        village.Peddler.art.transform.position=new Vector3(PrototypeVillage.Shelter,PrototypeWorld.Peddler.y,0);
        village.SendMessage("Update");
        Check(village.Keeper.state==PrototypeVillage.State.Hidden && !keeper.enabled && !village.Peddler.art.enabled,"Villagers hide in the house");

        Set(combat,"<EnemyHealth>k__BackingField",0);
        village.SendMessage("Update");
        Check(village.Cleared && village.Keeper.state==PrototypeVillage.State.Hidden,"They wait while the boss falls");
        Set(village,"<ClearAt>k__BackingField",Time.time-10); village.SendMessage("Update");
        Check(village.Keeper.state==PrototypeVillage.State.Returning && village.Peddler.state==PrototypeVillage.State.Returning && keeper.enabled,"Villagers come back out");
        elderVisual.Refresh();
        Check(!sword.enabled && pic(elderVisual).sprite.name==JinaArt.Elder,"Elder puts the sword away");
        village.Keeper.art.transform.position=new Vector3(8,-.5f,0);
        typeof(PrototypeVillage).GetMethod("DrawKeeper",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(village,new object[]{.02f});
        Check(keeper.flipX,"Keeper walks home facing right");
        Set(peddlerVisual,"last",village.Peddler.art.transform.position-Vector3.right*.02f); peddlerVisual.Refresh();
        Check(pic(peddlerVisual).flipX,"Peddler walks home facing right");
        Capture("villagers-return",new Vector3(7,1,-10),3);
        village.Keeper.art.transform.position=new Vector3(village.Keeper.home,-.5f,0);
        village.Peddler.art.transform.position=new Vector3(village.Peddler.home,PrototypeWorld.Peddler.y,0);
        village.SendMessage("Update");
        Check(village.KeeperHome && village.PeddlerHome,"Villagers back at their places");
        combat.Restart();
        Check(!village.Alarmed && village.KeeperHome && keeper.enabled && keeper.color.a==1,"Restart resets the village");
    }
    static void Capture(string name,Vector3 position,float size)
    {
        var camera=Camera.main; var oldPosition=camera.transform.position; float oldSize=camera.orthographicSize;
        var rt=new RenderTexture(960,540,24); var oldTarget=camera.targetTexture; var oldActive=RenderTexture.active;
        camera.transform.position=position; camera.orthographicSize=size; camera.targetTexture=rt; camera.Render();
        RenderTexture.active=rt; var image=new Texture2D(960,540,TextureFormat.RGBA32,false);
        image.ReadPixels(new Rect(0,0,960,540),0,0); image.Apply();
        Directory.CreateDirectory("validation"); File.WriteAllBytes("validation/"+name+".png",image.EncodeToPNG());
        camera.targetTexture=oldTarget; camera.transform.position=oldPosition; camera.orthographicSize=oldSize; RenderTexture.active=oldActive;
        UnityEngine.Object.Destroy(image); rt.Release(); UnityEngine.Object.Destroy(rt);
    }
}
