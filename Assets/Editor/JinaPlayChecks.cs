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
                Capture("purified",new Vector3(.5f,1,-10),3);
                stage=2; due=EditorApplication.timeSinceStartup+1.2;
            }
            else
            {
                Check(combat.PlayerHealth==hp && !combat.Quest.progress.ElderDead,"Purified boss never harms player or elder");
                combat.Restart();
                Check(!combat.BossPurified && !combat.EnemyDefeated && combat.EnemyHealth==30,"Restart restores hostile boss");
                Check(combat.Quest.progress.Honor==0,"Restart resets honor");
                combat.AddArrows(-5); Check(combat.Arrows==15,"Arrows spent");
                combat.Restart(); Check(combat.Arrows==ShopRules.MaxArrows,"Restart refills quiver");
                Debug.Log("JINA_PLAY_CHECKS_PASSED: v6 assets, attack/jump/walk frames, movement/idle transitions, purification visibility, immunity, no damage, reset");
                SessionState.SetBool(Key,false); EditorApplication.Exit(0);
            }
        }
        catch(Exception e) { Debug.LogException(e); SessionState.SetBool(Key,false); EditorApplication.Exit(1); }
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
