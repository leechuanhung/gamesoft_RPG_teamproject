using UnityEngine;

// The first encounter with the boss, the villagers' chatter, and talking with the purified dokkaebi.
public static partial class JinaPlayChecks
{
    static void Encounter()
    {
        var encounter=combat.Encounter; var bossVisual=boss.GetComponent<JinaActorVisual>();
        var view=Camera.main.GetComponent<PrototypeView>();
        combat.player.transform.position=new Vector3(WorldBuilder.ForestStart+1,combat.player.transform.position.y,0);
        Call(encounter);
        Check(!encounter.Playing,"No encounter before the boss notices");
        Set(combat,"alerted",true); Call(encounter);
        Check(encounter.Playing && combat.Paused && !combat.player.CanMove,"The boss noticing starts the encounter and holds the game");
        Set(encounter,"startAt",Time.time-BossEncounter.RoarAt-.1f); Call(encounter); bossVisual.Refresh();
        Check(encounter.Roaring && Picture(bossVisual).sprite.name.EndsWith("boss_attack_00"),"It roars with the club raised");
        Set(encounter,"startAt",Time.time-2f); Call(encounter); Call(view,"LateUpdate");
        Check(Mathf.Abs(Camera.main.transform.position.x-view.ViewX(boss.transform.position.x))<.3f,"The camera turns to the boss");
        Capture("encounter",Camera.main.transform.position,5);
        Set(encounter,"startAt",Time.time-BossEncounter.Length-.01f); Call(encounter); Call(view,"LateUpdate");
        Check(!encounter.Playing && !combat.Paused && combat.player.CanMove,"Play resumes after the encounter");
        Check(Mathf.Abs(Camera.main.transform.position.x-view.ViewX(combat.player.transform.position.x))<.3f,"The camera returns to the seonbi");
        Call(encounter);
        Check(!encounter.Playing,"The encounter plays once");
        combat.Restart();
        Set(combat,"alerted",true); Call(encounter);
        Check(encounter.Playing,"Restart lets it play again");
        combat.Restart();
    }

    static void Chatter()
    {
        var chatter=combat.Chatter; var player=combat.player.transform;
        var slain=new QuestProgress(); slain.Resolve(false);
        var purified=WithCharm(new QuestProgress()); purified.Resolve(true);
        var mourning=new QuestProgress(); mourning.SacrificeElder();
        var topics=new[]{ShopChatter.Topics(new QuestProgress()),ShopChatter.Topics(slain),ShopChatter.Topics(purified),ShopChatter.Topics(mourning)};
        for(int i=0;i<topics.Length;i++)
        {
            Check(topics[i].Length>0 && System.Array.TrueForAll(topics[i],pair=>pair.Length==2),"Each exchange is a line and an answer");
            for(int j=0;j<i;j++) Check(topics[i][0][0]!=topics[j][0][0],"Each turn of the story has its own talk");
        }
        player.position=new Vector3(0,player.position.y,0);
        Check(!chatter.CanChat,"They keep quiet when the seonbi is far");
        player.position=new Vector3((WorldBuilder.Tavern.x+WorldBuilder.Peddler.x)/2,player.position.y,0);
        Check(chatter.CanChat,"They chat when he is nearby");
        Set(chatter,"nextAt",0f); Call(chatter);
        Check(chatter.Talking && Field<string[]>(chatter,"current")==topics[0][0],"The first exchange starts");
        Set(combat.Tavern,"<IsOpen>k__BackingField",true); Call(chatter);
        Check(!chatter.Talking,"A shop opening cuts the chatter");
        Set(combat.Tavern,"<IsOpen>k__BackingField",false);
        Set(chatter,"nextAt",0f); Call(chatter);
        Check(chatter.Talking && Field<string[]>(chatter,"current")==topics[0][1],"Then the next topic");
        combat.Restart();
        Check(!chatter.Talking,"Restart quiets them");
    }

    static void CheckBossTalk()
    {
        var talk=combat.BossTalk; var player=combat.player.transform;
        foreach(bool elderDead in new[]{false,true})
        {
            if(elderDead) { boss.transform.position=new Vector3(WorldBuilder.Elder.x+1.5f,boss.transform.position.y,0); combat.Quest.progress.SacrificeElder(); combat.Quest.ShowElderDeath(); }
            boss.transform.position=new Vector3(WorldBuilder.ForestStart+6,boss.transform.position.y,0);
            player.position=new Vector3(boss.transform.position.x-1.5f,player.position.y,0);
            WithCharm(combat.Quest.progress);
            Check(combat.TryPurifyBoss(),"Purified for the talk");
            Check(!talk.Near,"No talk during the purification");
            float wait=BossVisual.PurifyLength+(elderDead ? BossRemorse.Spoken : 0)+.1f;
            if(elderDead)
            {
                Set(combat,"<PurifiedAt>k__BackingField",Time.time-BossVisual.PurifyLength-.1f);
                Check(!talk.Near,"No talk while he is still grieving aloud");
            }
            Set(combat,"<PurifiedAt>k__BackingField",Time.time-wait);
            Check(talk.Near,"The purified dokkaebi can be spoken to");
            talk.Talk();
            Check(talk.IsOpen && combat.Paused && !combat.player.CanMove,"Talking holds the game");
            int pages=1;
            while(talk.IsOpen && pages<20) { talk.Talk(); if(talk.IsOpen) pages++; }
            Check(!talk.IsOpen && combat.player.CanMove,"The last page closes the talk");
            Check(elderDead ? pages==1 : pages>=4,elderDead ? "After killing the elder he only asks to be left alone" : "He tells his story over several pages");
            combat.Restart();
            Check(!talk.IsOpen && !talk.Near,"Restart ends the talk");
        }
    }
}
