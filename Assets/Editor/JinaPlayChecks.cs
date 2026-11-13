using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Plays MovementTest and checks the art and motions in the running scene, leaving renders in validation/.
// Stage 1 runs right after the intro; stage 2 runs a moment later, after the purified boss has had time to act.
// The checks themselves are split by subject: Player, Boss, Camp, Elder, Village, Story, Terrain and Ending.
[InitializeOnLoad]
public static partial class JinaPlayChecks
{
    const string Key="JinaPlayChecks.Active";
    static int stage;
    static double due;
    static PrototypeCombat combat;
    static SpriteRenderer boss;
    static int hp;
    static JinaPlayChecks() { EditorApplication.update += Tick; }
    public static void Begin()
    {
        SessionState.SetBool(Key,true);
        EditorSceneManager.OpenScene("Assets/Scenes/MovementTest.unity");
        EditorApplication.isPlaying=true;
    }
    static void Tick()
    {
        if(!SessionState.GetBool(Key,false) || !EditorApplication.isPlaying) return;
        try
        {
            if(stage==0) { due=EditorApplication.timeSinceStartup+1; stage=1; return; }
            if(EditorApplication.timeSinceStartup<due) return;
            combat=UnityEngine.Object.FindObjectOfType<PrototypeCombat>();
            Check(combat!=null,"Combat scene started");
            boss=Field<SpriteRenderer>(combat,"enemy");
            if(stage==1)
            {
                IntroAndAssets();
                var player=combat.player.GetComponent<SeonbiVisual>();
                PlayerMotion(player);
                var bossVisual=boss.GetComponent<JinaActorVisual>();
                BossMotion(bossVisual);
                Purification(player,bossVisual);
                stage=2; due=EditorApplication.timeSinceStartup+1.2;
            }
            else
            {
                Check(combat.PlayerHealth==hp && !combat.Quest.progress.ElderDead,"Purified boss never harms player or elder");
                combat.Restart();
                var elderVisual=GameObject.Find("Village Elder").GetComponent<JinaActorVisual>();
                ElderDeath(elderVisual);
                combat.Restart();
                Check(!combat.Quest.progress.ElderDead,"Restart revives the elder");
                Village(elderVisual);
                GhostDeath();
                Mourning(elderVisual);
                Remorse();
                Encounter();
                Chatter();
                CheckBossTalk();
                Terrain();
                AimedArrows();
                FlyingGhosts();
                BossMoves();
                Items();
                Maps();
                CheckFerry();
                Pass();
                CheckKongVillage();
                CheckKongBoss();
                Polish();
                PlayerFall();
                Endings();
                Check(!combat.BossPurified && !combat.EnemyDefeated && combat.EnemyHealth==PrototypeCombat.EnemyMaxHealth,"Restart restores hostile boss");
                Check(combat.Quest.progress.Honor==0,"Restart resets honor");
                combat.AddArrows(-5); Check(combat.Arrows==15,"Arrows spent");
                combat.Restart(); Check(combat.Arrows==ShopRules.MaxArrows,"Restart refills quiver");
                Debug.Log("JINA_PLAY_CHECKS_PASSED: v6 assets, attack/jump/walk frames, boss death, purify and elder fall motions, talk facing, alarm, evacuation and return, ghost and seonbi deaths, mourning, remorse, encounter, chatter, dokkaebi talk, backdrops and ledges, aimed arrows, flying ghosts, boss charge and leap, endings, movement/idle transitions, purification visibility, immunity, no damage, reset");
                SessionState.SetBool(Key,false); EditorApplication.Exit(0);
            }
        }
        catch(Exception e) { Debug.LogException(e); SessionState.SetBool(Key,false); EditorApplication.Exit(1); }
    }

    static void IntroAndAssets()
    {
        Check(combat.Intro!=null && combat.Intro.IsPlaying && combat.Paused && !combat.player.CanMove,"Intro plays first and holds the game");
        combat.Intro.Skip();
        Check(!combat.Intro.IsPlaying && !combat.Paused && combat.player.CanMove,"Skipping the intro hands control back");
        foreach(string p in new[]{JinaArt.Player,JinaArt.Boss,JinaArt.Purified,JinaArt.Ghost,JinaArt.Elder,JinaArt.Peddler,"04_Weapons/arrow","04_Weapons/bow","04_Weapons/hwando"}) Check(JinaArt.Load(p)!=null,"Load "+p);
        foreach(string prefix in new[]{"02_Player_Attack_Sword/sword_attack_","03_Player_Attack_Bow/bow_attack_","05_Boss_Brainwashed/boss_attack_","09_Player_Jump/jump_","10_Player_Walk/player_walk_","11_Player_Run/player_run_","05_Boss_Brainwashed/boss_walk_"}) Check(JinaArt.Frames(prefix).Length==4,"Animation frames");
        Capture("village",new Vector3(-1,1,-10),3);
        Capture("elder-peddler",new Vector3(WorldBuilder.Elder.x-2,1,-10),3);
        Check(combat.Peddler!=null && combat.Arrows==ShopRules.MaxArrows,"Peddler placed and quiver full");
        Capture("tavern-peddler",new Vector3(WorldBuilder.Peddler.x-3,1,-10),3);
        Capture("props-village",new Vector3(8,1.8f,-10),5);
        Capture("props-forest",new Vector3(42,2.6f,-10),5.5f);
        Capture("props-stele",new Vector3(WorldBuilder.Stone.x,1,-10),2.5f);
    }

    // Private state is reached by reflection; auto-properties are "<Name>k__BackingField".
    static T Field<T>(object obj,string name) => (T)obj.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(obj);
    static void Set(object obj,string name,object value) => obj.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(obj,value);
    static SpriteRenderer Picture(JinaActorVisual v) => Field<SpriteRenderer>(v,"picture");
    // Purifying takes the charm: hear the clue and read the stele (moon, river, mountain).
    static QuestProgress WithCharm(QuestProgress q) { q.HearClue(); q.ChooseSymbol(2); q.ChooseSymbol(1); q.ChooseSymbol(3); return q; }
    // Runs one component's Update/LateUpdate. SendMessage would reach every component on its object, and the combat
    // object also carries the village, encounter, chatter and the rest.
    static void Call(Component c,string method="Update") => c.GetType().GetMethod(method,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(c,null);
    static void Check(bool ok,string reason) { if(!ok) throw new Exception(reason); }
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
