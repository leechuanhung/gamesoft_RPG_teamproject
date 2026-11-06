using System.Reflection;
using UnityEngine;

// The seonbi: jump, walk, run and attack frames, the carried bow, and where arrows leave the bow.
public static partial class JinaPlayChecks
{
    static void PlayerMotion(SeonbiVisual visual)
    {
        System.Func<string> body=()=>Field<SpriteRenderer>(visual,"body").sprite.name;
        visual.ResetPose();
        Set(combat.player,"<IsGrounded>k__BackingField",false);
        var rb=combat.player.GetComponent<Rigidbody2D>();
        rb.velocity=Vector2.up*5;
        Set(visual,"airTime",.2f); visual.SendMessage("LateUpdate");
        Check(body().EndsWith("jump_01"),"Ascending jump frame");
        Capture("jump-rise",new Vector3(0,1,-10),2);
        rb.velocity=Vector2.down*5; visual.SendMessage("LateUpdate");
        Check(body().EndsWith("jump_02"),"Falling jump frame");
        Set(combat.player,"<IsGrounded>k__BackingField",true);
        rb.velocity=Vector2.zero; visual.SendMessage("LateUpdate");
        Check(body().EndsWith("jump_03"),"Landing jump frame");
        visual.ResetPose(); visual.SendMessage("LateUpdate");
        Check(body()==JinaArt.Player,"Idle after landing");

        rb.velocity=Vector2.right*3;
        for(int i=0;i<4;i++)
        {
            Set(visual,"walkTime",i*.12f); visual.SendMessage("LateUpdate");
            Check(body().Contains("player_walk_"),"Player walks while moving");
            Capture("player-walk-"+i,new Vector3(0,1,-10),2);
        }
        Set(combat.player,"<IsRunning>k__BackingField",true); rb.velocity=Vector2.right*combat.player.runSpeed;
        for(int i=0;i<4;i++)
        {
            Set(visual,"walkTime",i*.08f+.01f); visual.SendMessage("LateUpdate");
            Check(body().EndsWith("player_run_"+i.ToString("00")),"Player runs while sprinting");
            Capture("player-run-"+i,new Vector3(0,1,-10),2);
        }
        Set(combat.player,"<IsRunning>k__BackingField",false);
        rb.velocity=Vector2.zero; visual.SendMessage("LateUpdate");
        Check(body()==JinaArt.Player,"Stop restores idle");

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
            Call(combat);
            var shot=GameObject.Find("Arrow").GetComponent<SpriteRenderer>();
            float expectedY=combat.player.GetComponent<SpriteRenderer>().bounds.min.y+312f/(510f/1.9f);
            Check(Mathf.Abs(shot.transform.position.y-expectedY)<.01f,"Arrow matches drawn nock height in direction "+direction);
            Check(shot.flipX==(direction<0),"Arrow faces shot direction");
            Capture("bow-release-"+(direction>0 ? "right" : "left"),new Vector3(0,1,-10),2);
            shot.enabled=false; shot.gameObject.name="Checked Arrow";
            typeof(PrototypeCombat).GetMethod("ClearArrows",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(combat,null);
        }
        Set(combat,"<UsingBow>k__BackingField",false);

        visual.Attack(false,1); Set(visual,"attackTime",.21f);
        visual.SendMessage("LateUpdate");
        Capture("player-sword",new Vector3(0,1,-10),2);
        visual.Attack(true,1); Set(visual,"attackTime",.31f);
        visual.SendMessage("LateUpdate");
        Capture("player-bow",new Vector3(0,1,-10),2);

        // A hit he survives: he reels back, then stands again. The backstep (K) ducks away and cannot be hit.
        visual.ResetPose(); Set(combat,"<PlayerHurtAt>k__BackingField",Time.time); visual.SendMessage("LateUpdate");
        Check(body().EndsWith("player_hit_00"),"Survived hit shows the recoil");
        Check(!Field<SpriteRenderer>(visual,"weapon").enabled,"No carried weapon while reeling");
        Capture("player-hurt",new Vector3(0,1,-10),2);
        Set(combat,"<PlayerHurtAt>k__BackingField",Time.time-SeonbiVisual.HurtLength-.01f); visual.SendMessage("LateUpdate");
        Check(body()==JinaArt.Player,"Recoil ends in idle");
        Set(combat,"<PlayerHurtAt>k__BackingField",-1f);
        Set(combat.player,"dodgeUntil",Time.time+1); visual.SendMessage("LateUpdate");
        Check(body().EndsWith("player_dodge"),"Backstep frame");
        Capture("player-dodge",new Vector3(0,1,-10),2);
        combat.player.SendMessage("FixedUpdate");
        Check(rb.velocity.x*combat.player.Facing<0,"Backstep moves away from where he faces");
        int health=combat.PlayerHealth; Set(combat,"invulnerableUntil",0f); combat.HurtPlayer();
        Check(combat.PlayerHealth==health && combat.PlayerHurtAt<0,"Backstep avoids the hit");
        Set(combat.player,"dodgeUntil",0f); rb.velocity=Vector2.zero; visual.ResetPose(); visual.SendMessage("LateUpdate");
    }
}
