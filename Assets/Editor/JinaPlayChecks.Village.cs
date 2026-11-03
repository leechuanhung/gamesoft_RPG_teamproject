using UnityEngine;

// Talking turns villagers to the player; the boss entering the village sets off the alarm and the evacuation,
// and once it falls the villagers walk home.
public static partial class JinaPlayChecks
{
    static void Village(JinaActorVisual elderVisual)
    {
        var village=combat.Village; var player=combat.player.transform;
        var peddlerVisual=village.Peddler.art.GetComponent<JinaActorVisual>();
        var keeper=village.Keeper.art; var keeperVisual=keeper.GetComponent<KeeperVisual>();
        TalkFacing(elderVisual,peddlerVisual,keeperVisual,keeper);
        player.position=new Vector3(0,player.position.y,0);

        // Alarm: the elder is startled, then draws his sword.
        boss.transform.position=new Vector3(PrototypeWorld.Elder.x+4,boss.transform.position.y,0);
        Set(combat,"alerted",true);
        village.SendMessage("Update");
        Check(village.Alarmed && !village.Cleared,"Boss in the village raises the alarm");
        var sword=Field<SpriteRenderer>(elderVisual,"sword");
        foreach(float t in new[]{.1f,.4f,.6f,1f})
        {
            Set(village,"<AlarmAt>k__BackingField",Time.time-t); elderVisual.Refresh();
            string expect=t<.3f ? "elder_startle" : t<.55f ? "elder_draw_00" : "elder_draw_01";
            Check(Picture(elderVisual).sprite.name.EndsWith(expect),"Elder alarm frame "+expect);
            Check(sword.enabled==(t>=.3f),"Sword drawn after the startle");
            Capture("elder-alarm-"+t.ToString("0.0"),new Vector3(PrototypeWorld.Elder.x+.5f,1,-10),2);
        }

        // Evacuation: both run left into the house; the tavern is shut meanwhile.
        village.SendMessage("Update");
        Check(village.Keeper.state==PrototypeVillage.State.Fleeing && village.Peddler.state==PrototypeVillage.State.Fleeing,"Villagers flee");
        keeper.transform.position=new Vector3(7,-.5f,0);
        village.Peddler.art.transform.position=new Vector3(9,PrototypeWorld.Peddler.y,0);
        Set(keeperVisual,"last",keeper.transform.position+Vector3.right*.06f); keeperVisual.Refresh();
        Check(keeper.sprite.name.Contains("keeper_run_") && !keeper.flipX,"Keeper runs left");
        Set(peddlerVisual,"last",village.Peddler.art.transform.position+Vector3.right*.06f); peddlerVisual.Refresh();
        Check(Picture(peddlerVisual).sprite.name.Contains("peddler_run_") && !Picture(peddlerVisual).flipX,"Peddler runs left");
        Capture("villagers-flee",new Vector3(7,1,-10),3);
        player.position=new Vector3(PrototypeWorld.Tavern.x,player.position.y,0);
        Check(!combat.Tavern.Near,"No tavern while the keeper is away");
        player.position=new Vector3(0,player.position.y,0);
        keeper.transform.position=new Vector3(PrototypeVillage.Shelter,-.5f,0);
        village.Peddler.art.transform.position=new Vector3(PrototypeVillage.Shelter,PrototypeWorld.Peddler.y,0);
        village.SendMessage("Update");
        Check(village.Keeper.state==PrototypeVillage.State.Hidden && !keeper.enabled && !village.Peddler.art.enabled,"Villagers hide in the house");

        // All clear: they wait for the boss's motion, come out and walk home; the elder sheathes his sword.
        Set(combat,"<EnemyHealth>k__BackingField",0);
        village.SendMessage("Update");
        Check(village.Cleared && village.Keeper.state==PrototypeVillage.State.Hidden,"They wait while the boss falls");
        Set(village,"<ClearAt>k__BackingField",Time.time-10); village.SendMessage("Update");
        Check(village.Keeper.state==PrototypeVillage.State.Returning && village.Peddler.state==PrototypeVillage.State.Returning && keeper.enabled,"Villagers come back out");
        elderVisual.Refresh();
        Check(!sword.enabled && Picture(elderVisual).sprite.name==JinaArt.Elder,"Elder puts the sword away");
        keeper.transform.position=new Vector3(8,-.5f,0);
        Set(keeperVisual,"last",keeper.transform.position-Vector3.right*.02f); keeperVisual.Refresh();
        Check(keeper.flipX,"Keeper walks home facing right");
        Set(peddlerVisual,"last",village.Peddler.art.transform.position-Vector3.right*.02f); peddlerVisual.Refresh();
        Check(Picture(peddlerVisual).flipX,"Peddler walks home facing right");
        Capture("villagers-return",new Vector3(7,1,-10),3);
        keeper.transform.position=new Vector3(village.Keeper.home,-.5f,0);
        village.Peddler.art.transform.position=new Vector3(village.Peddler.home,PrototypeWorld.Peddler.y,0);
        village.SendMessage("Update");
        Check(village.KeeperHome && village.PeddlerHome,"Villagers back at their places");
        combat.Restart();
        Check(!village.Alarmed && village.KeeperHome && keeper.enabled && keeper.color.a==1,"Restart resets the village");
    }

    // Each villager turns to the player on either side while talking, and back to their usual way after.
    static void TalkFacing(JinaActorVisual elderVisual,JinaActorVisual peddlerVisual,KeeperVisual keeperVisual,SpriteRenderer keeper)
    {
        var player=combat.player.transform;
        foreach(float side in new[]{-1.2f,1.2f})
        {
            player.position=new Vector3(PrototypeWorld.Elder.x+side,player.position.y,0);
            Set(combat.Quest,"<IsDialogOpen>k__BackingField",true); Set(combat.Quest,"shrineDialog",false); elderVisual.Refresh();
            Check(Picture(elderVisual).flipX==(side<0),"Elder turns to the player on his "+(side<0 ? "left" : "right"));
            Set(combat.Quest,"<IsDialogOpen>k__BackingField",false);
            player.position=new Vector3(PrototypeWorld.Peddler.x+side,player.position.y,0);
            Set(combat.Peddler,"<IsOpen>k__BackingField",true); peddlerVisual.Refresh();
            Check(Picture(peddlerVisual).flipX==(side>0),"Peddler turns to the player");
            Set(combat.Peddler,"<IsOpen>k__BackingField",false);
            player.position=new Vector3(PrototypeWorld.Tavern.x+side,player.position.y,0);
            Set(combat.Tavern,"<IsOpen>k__BackingField",true); keeperVisual.Refresh();
            Check(keeper.flipX==(side>0),"Keeper turns to the player");
            Set(combat.Tavern,"<IsOpen>k__BackingField",false);
        }
        elderVisual.Refresh(); peddlerVisual.Refresh(); keeperVisual.Refresh();
        Check(!Picture(elderVisual).flipX && !Picture(peddlerVisual).flipX && !keeper.flipX,"Villagers face their usual way after talking");
    }
}
