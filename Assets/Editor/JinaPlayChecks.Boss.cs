using System.Reflection;
using UnityEngine;

// The dokkaebi: walk and attack phases, the death motion, and the purification (rules and motion).
public static partial class JinaPlayChecks
{
    static void BossMotion(JinaActorVisual bossVisual)
    {
        for(int i=0;i<4;i++)
        {
            Set(bossVisual,"last",boss.transform.position-Vector3.right*.1f);
            Set(bossVisual,"walkTime",i*.14f); bossVisual.Refresh();
            Check(Picture(bossVisual).sprite.name.Contains("boss_walk_"),"Boss walks while moving");
            Capture("boss-walk-"+i,new Vector3(boss.transform.position.x,1,-10),3);
        }
        bossVisual.Refresh();
        Check(Picture(bossVisual).sprite.name==JinaArt.Boss,"Boss stops to idle");
        for(int i=0;i<4;i++)
        {
            Set(combat,"windupUntil",i<2 ? Time.time+(i==0 ? .5f : .2f) : -1f);
            Set(combat,"bossRecoveryUntil",i>=2 ? Time.time+(i==2 ? .2f : .05f) : 0f);
            bossVisual.Refresh();
            Check(Picture(bossVisual).sprite.name.EndsWith("boss_attack_0"+i),"Boss attack phase "+i);
            Capture("boss-attack-"+i,new Vector3(boss.transform.position.x,1,-10),3);
        }
        Set(combat,"windupUntil",Time.time+.2f);
        bossVisual.Refresh();
        Capture("boss-windup",new Vector3(boss.transform.position.x,1,-10),3);
        Set(combat,"windupUntil",-1f); Set(combat,"bossRecoveryUntil",0f);

        Set(combat,"<EnemyHealth>k__BackingField",0);
        float[] deathTimes={.1f,.3f,.8f,1.2f,1.5f};
        for(int i=0;i<5;i++)
        {
            Set(combat,"<BossDownAt>k__BackingField",Time.time-deathTimes[i]); bossVisual.Refresh();
            Check(Picture(bossVisual).sprite.name.EndsWith("boss_death_0"+i),"Boss death frame "+i);
            Capture("boss-death-"+i,new Vector3(boss.transform.position.x,1,-10),3);
        }
        Set(combat,"<BossDownAt>k__BackingField",Time.time-BossVisual.DeathLength-.01f); bossVisual.Refresh();
        Check(boss.enabled && !Picture(bossVisual).enabled,"Boss vanishes after the death motion");
        Set(combat,"<EnemyHealth>k__BackingField",30); Set(combat,"<BossDownAt>k__BackingField",-1f); bossVisual.Refresh();
        Check(Picture(bossVisual).enabled,"Boss visible again while alive");
    }

    // Purified after killing the elder: he turns to the body, kneels and weeps; purified otherwise, he just stands.
    static void Remorse()
    {
        var bossVisual=boss.GetComponent<JinaActorVisual>(); var tear=Field<SpriteRenderer[]>(bossVisual,"dots")[0];
        boss.transform.position=new Vector3(PrototypeWorld.Elder.x+1.5f,boss.transform.position.y,0);
        combat.Quest.progress.SacrificeElder(); combat.Quest.ShowElderDeath();
        boss.transform.position=new Vector3(PrototypeWorld.Elder.x-3,boss.transform.position.y,0);
        Set(combat,"<BossFacing>k__BackingField",-1);
        Check(WithCharm(combat.Quest.progress).Resolve(true),"Purified after the elder fell");
        foreach(float t in new[]{.3f,.8f,2f})
        {
            Set(combat,"<PurifiedAt>k__BackingField",Time.time-BossVisual.PurifyLength-t); bossVisual.Refresh();
            Check(combat.Remorse.Active,"Remorse follows the purification");
            string expect=t<BossRemorse.Realise ? JinaArt.Purified : t<BossRemorse.Realise+BossRemorse.Kneel ? "boss_grieve_00" : "boss_grieve_01";
            Check(Picture(bossVisual).sprite.name.EndsWith(expect),"Remorse frame "+expect);
            Check(!Picture(bossVisual).flipX,"He turns toward the elder's body");
            Capture("boss-remorse-"+t.ToString("0.0"),new Vector3(PrototypeWorld.Elder.x-2,1,-10),3);
        }
        Check(tear.enabled,"Tears fall while he weeps");
        combat.Restart();
        Check(!combat.Remorse.Active,"Restart ends the remorse");
        var plain=WithCharm(new QuestProgress()); plain.Resolve(true);
        Check(plain.Outcome==EncounterOutcome.Purified && !plain.ElderDead,"No remorse when the elder lives");
    }

    static void Purification(SeonbiVisual player,JinaActorVisual bossVisual)
    {
        var quest=combat.Quest.progress;
        quest.HearClue(); quest.ChooseSymbol(2); quest.ChooseSymbol(1); quest.ChooseSymbol(3);
        Set(combat.Quest,"charmReveal",0f);
        Check(combat.Quest.RevealingCharm,"Charm reveal plays");
        Set(combat.Quest,"charmReveal",-1f);
        player.ResetPose(); player.SendMessage("LateUpdate");
        boss.transform.position=new Vector3(combat.player.transform.position.x+2.5f,boss.transform.position.y,0);
        hp=combat.PlayerHealth;
        Check(combat.TryPurifyBoss(),"Purification succeeds");
        Check(combat.BossPurified && combat.EnemyDefeated && boss.enabled,"Friendly boss remains visible and resolves encounter");
        Check(boss.GetComponentInChildren<SpriteRenderer>().enabled,"Boss renderer enabled");
        typeof(PrototypeCombat).GetMethod("DamageEnemy",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(combat,new object[]{5});
        Check(combat.EnemyHealth==30,"Purified boss ignores damage");
        Check(!combat.TryPurifyBoss() && quest.Honor==30,"No duplicate reward");

        System.Func<string,SpriteRenderer> part=name=>Field<SpriteRenderer>(bossVisual,name);
        foreach(float t in new[]{.3f,.65f,.8f,1.2f})
        {
            Set(combat,"<PurifiedAt>k__BackingField",Time.time-t); bossVisual.Refresh();
            Check(Picture(bossVisual).sprite.name==(t<.7f ? JinaArt.Boss : JinaArt.Purified),"Purify motion swaps the form at its flash");
            Check(part("glow").enabled,"Purify flash shows");
            Check(part("charm").enabled==(t<.9f),"Charm shows while the curse lifts");
            Capture("purify-"+t.ToString("0.00"),new Vector3(boss.transform.position.x,1,-10),3);
        }
        Set(combat,"<PurifiedAt>k__BackingField",Time.time-BossVisual.PurifyLength-.01f); bossVisual.Refresh();
        Check(!part("glow").enabled && !part("charm").enabled,"Purify motion ends cleanly");
        Capture("purified",new Vector3(.5f,1,-10),3);
    }
}
