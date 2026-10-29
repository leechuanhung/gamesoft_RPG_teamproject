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
                foreach(string p in new[]{JinaArt.Player,JinaArt.Boss,JinaArt.Purified,JinaArt.Ghost,JinaArt.Elder,"04_Weapons/arrow","04_Weapons/bow","04_Weapons/hwando"}) Check(JinaArt.Load(p)!=null,"Load "+p);
                foreach(string prefix in new[]{"02_Player_Attack_Sword/sword_attack_","03_Player_Attack_Bow/bow_attack_","05_Boss_Brainwashed/boss_attack_"}) Check(JinaArt.Frames(prefix).Length==4,"Animation frames");
                Capture("village",new Vector3(-1,1,-10),3);
                var visual=combat.player.GetComponent<SeonbiVisual>();
                visual.Attack(false,1); Set(visual,"attackTime",.21f);
                visual.SendMessage("LateUpdate");
                Capture("player-sword",new Vector3(0,1,-10),2);
                visual.Attack(true,1); Set(visual,"attackTime",.31f);
                visual.SendMessage("LateUpdate");
                Capture("player-bow",new Vector3(0,1,-10),2);
                Set(combat,"windupUntil",Time.time+.2f);
                boss.GetComponent<JinaActorVisual>().Refresh();
                Capture("boss-windup",new Vector3(boss.transform.position.x,1,-10),3);
                combat.Quest.progress.HearClue(); combat.Quest.progress.ChooseSymbol(2); combat.Quest.progress.ChooseSymbol(1); combat.Quest.progress.ChooseSymbol(3);
                boss.transform.position=combat.player.transform.position+Vector3.right;
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
                Debug.Log("JINA_PLAY_CHECKS_PASSED: assets, frames, purification visibility, immunity, no damage, reset");
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
