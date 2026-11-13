using UnityEngine;

// The seonbi struck down, and the three endings at the road to Hanyang.
public static partial class JinaPlayChecks
{
    static void PlayerFall()
    {
        var visual=combat.player.GetComponent<SeonbiVisual>();
        var body=Field<SpriteRenderer>(visual,"body"); var weapon=Field<SpriteRenderer>(visual,"weapon");
        Set(combat,"<PlayerHealth>k__BackingField",1); Set(combat,"invulnerableUntil",0f);
        combat.HurtPlayer();
        Check(combat.PlayerHealth==0 && combat.PlayerDownAt>=0 && combat.player.Downed && !combat.player.CanMove,"Seonbi is struck down");
        foreach(float t in new[]{.1f,.3f,.6f,1f})
        {
            Set(combat,"<PlayerDownAt>k__BackingField",Time.time-t); visual.SendMessage("LateUpdate");
            string expect=t<.2f ? "player_hit_00" : t<.45f ? "player_hit_01" : t<SeonbiVisual.FallLength ? JinaArt.Player : "player_corpse";
            Check(body.sprite.name.EndsWith(expect),"Seonbi fall frame "+expect);
            Check(weapon.enabled,"His weapon drops beside him");
            Capture("player-fall-"+t.ToString("0.0"),new Vector3(combat.player.transform.position.x,1,-10),2);
        }
        Check(body.color==Color.gray && Mathf.Abs(body.transform.eulerAngles.z)<.01f,"Seonbi lies as the greyed corpse drawing");

        // Game over: black over the world, and the seonbi alone collapsing in its own motion.
        var over=combat.GameOver; var actor=Field<SpriteRenderer>(over,"actor"); var veil=Field<SpriteRenderer>(over,"veil");
        foreach(var (t,frame) in new[]{(.1f,"player_ko_00"),(.5f,"player_ko_01"),(1f,"player_ko_02"),(PrototypeGameOver.Down+.01f,"player_facedown")})
        {
            Set(combat,"<PlayerDownAt>k__BackingField",Time.time-t); Call(over,"LateUpdate");
            Check(veil.enabled && actor.enabled && actor.sprite.name.EndsWith(frame),"Game over frame "+frame);
            Check(actor.sortingOrder>veil.sortingOrder && veil.sortingOrder>body.sortingOrder,"Only the game-over seonbi shows above the black");
            Capture("game-over-"+t.ToString("0.00"),new Vector3(combat.player.transform.position.x,1,-10),2);
        }
        Check(Mathf.Abs(actor.transform.eulerAngles.z)<.01f && veil.color.a==1,"He lies face down in full black");
        combat.Restart(); visual.SendMessage("LateUpdate");
        Check(combat.PlayerHealth==PrototypeCombat.MaxPlayerHealth && !combat.player.Downed && body.sprite.name==JinaArt.Player && body.transform.rotation==Quaternion.identity && weapon.color==Color.white,"Restart stands him back up");
        Call(over,"LateUpdate");
        Check(!veil.enabled && !actor.enabled,"Restart lifts the black");
    }

    static void Endings()
    {
        var slain=new QuestProgress(); slain.Resolve(false);
        var purified=WithCharm(new QuestProgress()); purified.Resolve(true);
        var sacrifice=WithCharm(new QuestProgress()); sacrifice.SacrificeElder(); sacrifice.Resolve(true);
        Check(PrototypeEnding.For(slain)==PrototypeEnding.Kind.Slain && PrototypeEnding.For(purified)==PrototypeEnding.Kind.Purified
            && PrototypeEnding.For(sacrifice)==PrototypeEnding.Kind.Sacrifice,"Each choice picks its ending");

        var ending=combat.Ending; var player=combat.player.transform;
        player.position=new Vector3(PrototypeWorld.GoalX-3,player.position.y,0);
        Set(combat,"<EnemyHealth>k__BackingField",0); Call(ending);
        Check(!ending.Active,"No ending before the road");
        player.position=new Vector3(PrototypeWorld.GoalX,player.position.y,0);
        Set(combat,"<EnemyHealth>k__BackingField",PrototypeCombat.EnemyMaxHealth); Call(ending);
        Check(!ending.Active,"No ending while the boss stands");
        Set(combat,"<EnemyHealth>k__BackingField",0); combat.Quest.progress.Resolve(false); Call(ending);
        Check(!ending.Active,"No ending while the Kongjwi demon stands on the hill");
        Set(combat.KongBoss,"<Now>k__BackingField",PrototypeKongBoss.Phase.Slain); Call(ending);
        Check(ending.Active && ending.IsPlaying && ending.Ending==PrototypeEnding.Kind.Slain && combat.Paused && !combat.player.CanMove,"Reaching the road plays the ending");
        ending.Skip();
        Check(ending.Active && !ending.IsPlaying && combat.Paused,"Skipping leaves the ending title up");
        combat.Restart();
        Check(!ending.Active && !combat.Paused && combat.player.CanMove,"Restart closes the ending");
    }
}
