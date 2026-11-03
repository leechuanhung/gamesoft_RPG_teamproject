using UnityEngine;
public class SeonbiVisual : MonoBehaviour
{
    private PrototypePlayer player;
    private PrototypeCombat combat;
    private SpriteRenderer source,body,weapon;
    private Sprite[] swordFrames,bowFrames,jumpFrames,walkFrames,runFrames,hitFrames;
    private bool wasAirborne;
    private float airTime,landingTime,walkTime;
    private float attackTime=10;
    private bool bowAttack;
    private int attackFacing=1;
    public const float SwordDuration=.4f,BowDuration=.6f,HitDelay=.2f,BowReleaseDelay=.3f;
    // Struck down: reel back, knees give way, then a quarter turn onto his back.
    public const float FallLength=.85f;
    public static Sprite LoadSprite() => JinaArt.Load(JinaArt.Player);
    public static SeonbiVisual Attach(PrototypePlayer player,PrototypeCombat combat)
    {
        var v=player.gameObject.AddComponent<SeonbiVisual>();
        v.player=player; v.combat=combat; v.source=player.GetComponent<SpriteRenderer>();
        // Body and weapon hang under nothing: they tip over when he falls, which would shear under his scaled square.
        v.body=v.MakePart("선비 프레임",5); v.weapon=v.MakePart("장착 무기",6);
        v.swordFrames=JinaArt.Frames("02_Player_Attack_Sword/sword_attack_");
        v.bowFrames=JinaArt.Frames("03_Player_Attack_Bow/bow_attack_");
        v.jumpFrames=JinaArt.Frames("09_Player_Jump/jump_");
        v.walkFrames=JinaArt.Frames("10_Player_Walk/player_walk_");
        v.runFrames=JinaArt.Frames("11_Player_Run/player_run_");
        v.hitFrames=JinaArt.Frames("01_Player/player_hit_",2);
        v.source.forceRenderingOff=true; v.LateUpdate(); return v;
    }
    SpriteRenderer MakePart(string name,int order)
    {
        var art=new GameObject(name).AddComponent<SpriteRenderer>(); art.sortingOrder=order; return art;
    }
    public void Attack(bool bow,int facing) { bowAttack=bow; attackFacing=facing; attackTime=0; }
    public void ResetPose() { attackTime=10; walkTime=0; airTime=0; landingTime=0; wasAirborne=false; }
    void LateUpdate()
    {
        if(body==null) return;
        if(combat.Paused || combat.PlayerHealth<=0) ResetPose();
        float duration=bowAttack ? BowDuration : SwordDuration;
        bool attacking=attackTime<duration;
        int facing=attacking ? attackFacing : player.Facing;
        bool airborne=!player.IsGrounded && combat.PlayerHealth>0;
        if(!combat.Paused)
        {
            if(airborne) { airTime+=Time.deltaTime; landingTime=0; }
            else { if(wasAirborne) landingTime=.12f; airTime=0; landingTime=Mathf.Max(0,landingTime-Time.deltaTime); }
            wasAirborne=airborne;
        }
        int jumpFrame=airborne ? (airTime<.10f && player.VerticalSpeed>0 ? 0 : player.VerticalSpeed>0 ? 1 : 2) : 3;
        bool jumping=(airborne || landingTime>0) && combat.PlayerHealth>0;
        bool walking=player.IsGrounded && Mathf.Abs(player.HorizontalSpeed)>.05f && player.CanMove && combat.PlayerHealth>0;
        bool running=walking && player.IsRunning;
        if(!combat.Paused) walkTime=walking ? walkTime+Time.deltaTime : 0;
        Vector3 feet=new Vector3(transform.position.x,source.bounds.min.y,0);
        if(combat.PlayerHealth<=0 && combat.PlayerDownAt>=0) { Fall(feet,facing,Time.time-combat.PlayerDownAt); return; }
        body.sprite=attacking ? (bowAttack ? bowFrames : swordFrames)[Mathf.Min(3,(int)(attackTime/(duration/4)))] : jumping ? jumpFrames[jumpFrame] : running ? runFrames[(int)(walkTime/.08f)%4] : walking ? walkFrames[(int)(walkTime/.12f)%4] : LoadSprite();
        JinaArt.Place(body,feet,facing<0);
        body.transform.rotation=Quaternion.identity; weapon.transform.rotation=Quaternion.identity; weapon.color=Color.white;
        body.color=source.color==Color.gray ? Color.gray : source.color==Color.red ? new Color(1,.4f,.4f) : Color.white;
        weapon.enabled=!attacking && !jumping && combat.PlayerHealth>0;
        if(weapon.enabled)
        {
            weapon.sprite=JinaArt.Load(combat.UsingBow ? "04_Weapons/bow" : "04_Weapons/hwando");
            JinaArt.Place(weapon,Carry(feet,facing,running),facing<0);
        }
        if(!combat.Paused) attackTime+=Time.deltaTime;
    }
    // Where the carried weapon sits. Run frames lean the torso ~45 px (~.16 units) forward, so it follows.
    Vector3 Carry(Vector3 feet,int facing,bool running) =>
        feet+new Vector3(facing*((combat.UsingBow ? .14f : .27f)+(running ? .16f : 0)),combat.UsingBow ? .86f : .75f,0);

    // Like the elder: reel back, knees give way, then he tips a quarter turn about his feet (rising so his back
    // lands on the ground) and lies as the corpse drawing, greyed. His weapon drops to lie beside him.
    void Fall(Vector3 feet,int facing,float t)
    {
        var corpse=JinaArt.Load("01_Player/player_corpse");
        float rest=corpse.pivot.y/corpse.pixelsPerUnit, rotation=0;
        var lying=feet; var tint=Color.gray;
        body.sprite=LoadSprite();
        if(t<.45f) { body.sprite=hitFrames[t<.2f ? 0 : 1]; tint=Color.white; }
        else if(t<FallLength)
        {
            float k=(t-.45f)/(FallLength-.45f);
            rotation=90*k*k*facing; tint=Color.Lerp(Color.white,Color.gray,k);
            lying.y+=rest*Mathf.Sin(90*k*k*Mathf.Deg2Rad);
        }
        else { body.sprite=corpse; lying.y+=rest; }
        JinaArt.Place(body,lying,facing<0);
        body.transform.rotation=Quaternion.Euler(0,0,rotation);
        body.color=tint;
        // The hwando lies flat (its blade is drawn ~40° up); the bow lies on its back.
        float drop=Mathf.Clamp01(t/.35f);
        weapon.enabled=true;
        weapon.sprite=JinaArt.Load(combat.UsingBow ? "04_Weapons/bow" : "04_Weapons/hwando");
        JinaArt.Place(weapon,Vector3.Lerp(Carry(feet,facing,false),feet+new Vector3(facing*.6f,.05f,0),drop*drop),facing<0);
        weapon.transform.rotation=Quaternion.Euler(0,0,Mathf.Lerp(0,combat.UsingBow ? -90 : -40,drop)*facing);
        weapon.color=Color.Lerp(Color.white,Color.gray,drop);
    }
    void OnDestroy()
    {
        if(source!=null) source.forceRenderingOff=false;
        if(body!=null) Destroy(body.gameObject);
        if(weapon!=null) Destroy(weapon.gameObject);
    }
}
