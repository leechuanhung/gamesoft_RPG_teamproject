using UnityEngine;
public class SeonbiVisual : MonoBehaviour
{
    private PrototypePlayer player;
    private PrototypeCombat combat;
    private SpriteRenderer source,body,weapon,gripFingers;
    private Sprite fingerSprite;
    private Sprite[] swordFrames,bowFrames,bowUpFrames,bowDownFrames,jumpFrames,walkFrames,runFrames,hitFrames,dashFrames;
    private bool wasAirborne;
    private float airTime,landingTime,walkTime;
    private float attackTime=10;
    private const float SheathDelay=3f, SheathMotion=.32f;
    private float swordOutTime;
    string EquippedWeapon => combat.UsingBow ? "04_Weapons/bow" :
        swordOutTime>SheathMotion*.5f ? "04_Weapons/hwando" : "04_Weapons/hwando_sheathed";
    private bool bowAttack;
    private int attackFacing=1,attackAim,attackStep;
    private float nextDust,nextGhost;
    private bool wasDodging;
    public const float SwordDuration=.4f,BowDuration=.6f,HitDelay=.2f,BowReleaseDelay=.3f;
    // The sword's three steps: how long each takes and when its blow lands.
    //   The first cut is the drawn four frames. The second is quick, the blade rising from low guard to overhead.
    //   The third holds overhead a moment, then comes down with all his weight.
    public static float SwordLength(int step) => step==1 ? .3f : step==2 ? .48f : SwordDuration;
    public static float SwordHit(int step) => step==1 ? .08f : step==2 ? .18f : HitDelay;
    int SwordFrame(float t)
    {
        if(attackStep==1) return t<.06f ? 0 : t<.24f ? 1 : 0;
        if(attackStep==2) return t<.18f ? 1 : t<.32f ? 2 : 3;
        return Mathf.Min(3,(int)(t/(SwordDuration/4)));
    }
    // Struck down: reel back, knees give way, then a quarter turn onto his back.
    public const float FallLength=.85f;
    // A hit he survives: he reels back for a moment (the death recoil frame), unless mid-swing.
    public const float HurtLength=.3f;
    public static Sprite LoadSprite() => JinaArt.Load(JinaArt.Player);
    public static SeonbiVisual Attach(PrototypePlayer player,PrototypeCombat combat)
    {
        var v=player.gameObject.AddComponent<SeonbiVisual>();
        v.player=player; v.combat=combat; v.source=player.GetComponent<SpriteRenderer>();
        // Body and weapon hang under nothing: they tip over when he falls, which would shear under his scaled square.
        v.body=v.MakePart("선비 프레임",5); v.weapon=v.MakePart("장착 무기",6);
        // Reuse the idle fist to draw just the gripping fingers over the weapon handle.
        var idle=LoadSprite();
        v.fingerSprite=Sprite.Create(idle.texture,new Rect(183,idle.texture.height-363,30,34),
            new Vector2(.5f,18f/34f),idle.pixelsPerUnit,0,SpriteMeshType.FullRect);
        v.gripFingers=v.MakePart("무기 쥔 손",7);
        v.gripFingers.sprite=v.fingerSprite;
        v.swordFrames=JinaArt.Frames("02_Player_Attack_Sword/sword_attack_");
        v.bowFrames=JinaArt.Frames("03_Player_Attack_Bow/bow_attack_");
        v.bowUpFrames=JinaArt.Frames("03_Player_Attack_Bow/bow_up_");
        v.bowDownFrames=JinaArt.Frames("03_Player_Attack_Bow/bow_down_");
        v.jumpFrames=JinaArt.Frames("09_Player_Jump/jump_");
        v.walkFrames=JinaArt.Frames("10_Player_Walk/player_walk_");
        v.runFrames=JinaArt.Frames("11_Player_Run/player_run_");
        v.hitFrames=JinaArt.Frames("01_Player/player_hit_",2);
        v.dashFrames=JinaArt.Frames("21_Dash_Slash/dash_slash_",8);
        v.source.forceRenderingOff=true; v.LateUpdate(); return v;
    }
    SpriteRenderer MakePart(string name,int order)
    {
        var art=new GameObject(name).AddComponent<SpriteRenderer>(); art.sortingOrder=order; return art;
    }
    // aim: +1 up, -1 down, 0 level (bow only).
    public void Attack(bool bow,int facing,int aim=0,int step=0) { bowAttack=bow; attackFacing=facing; attackAim=bow ? aim : 0; attackStep=bow ? 0 : step; attackTime=0; swordOutTime=bow ? 0 : SwordLength(step)+SheathDelay+SheathMotion; }
    public void ResetPose() { swordOutTime=0; ResetMovementPose(); }
    void ResetMovementPose() { attackTime=10; walkTime=0; airTime=0; landingTime=0; wasAirborne=false; }
    void LateUpdate()
    {
        if(body==null) return;
        gripFingers.enabled=false;
        if(combat.PlayerHealth<=0) ResetPose();
        else if(combat.Paused) ResetMovementPose();
        if(combat.UsingBow) swordOutTime=0;
        else if(!combat.Paused) swordOutTime=Mathf.Max(0,swordOutTime-Time.deltaTime);
        float duration=bowAttack ? BowDuration : SwordLength(attackStep);
        bool attacking=attackTime<duration;
        int facing=attacking ? attackFacing : player.Facing;
        bool airborne=!player.IsGrounded && combat.PlayerHealth>0;
        if(!combat.Paused)
        {
            var ground=new Vector3(transform.position.x,source.bounds.min.y,0);
            if(airborne) { if(!wasAirborne) { JinaFx.Dust(ground,0,.8f); } airTime+=Time.deltaTime; landingTime=0; }
            else
            {
                if(wasAirborne) { landingTime=.12f; JinaFx.Dust(ground,-.9f,1); JinaFx.Dust(ground,.9f,1); }
                airTime=0; landingTime=Mathf.Max(0,landingTime-Time.deltaTime);
            }
            wasAirborne=airborne;
        }
        int jumpFrame=airborne ? (airTime<.10f && player.VerticalSpeed>0 ? 0 : player.VerticalSpeed>0 ? 1 : 2) : 3;
        bool jumping=(airborne || landingTime>0) && combat.PlayerHealth>0;
        bool walking=player.IsGrounded && Mathf.Abs(player.HorizontalSpeed)>.05f && player.CanMove && combat.PlayerHealth>0;
        bool running=walking && player.IsRunning;
        if(!combat.Paused) walkTime=walking ? walkTime+Time.deltaTime : 0;
        Vector3 feet=new Vector3(transform.position.x,source.bounds.min.y,0);
        if(combat.PlayerHealth<=0 && combat.PlayerDownAt>=0) { Fall(feet,facing,Time.time-combat.PlayerDownAt); return; }
        bool hurt=combat.PlayerHurtAt>=0 && Time.time-combat.PlayerHurtAt<HurtLength && !attacking;
        bool dodging=player.Dodging && combat.PlayerHealth>0;
        bool dashSlashing=dodging && player.DashSlashing;
        var shot=attackAim>0 ? bowUpFrames : attackAim<0 ? bowDownFrames : bowFrames;
        int strideFrame=(int)(walkTime/(running ? .08f : .12f))%4;
        body.sprite=dashSlashing ? dashFrames[Mathf.Clamp((int)((1f-Mathf.Clamp01(player.DashRemaining / PrototypePlayer.DodgeLength))*8f),0,7)] : dodging ? JinaArt.Load("01_Player/player_dodge") : hurt ? hitFrames[0] : attacking ? (bowAttack ? shot[Mathf.Min(3,(int)(attackTime/(duration/4)))] : swordFrames[SwordFrame(attackTime)]) : jumping ? jumpFrames[jumpFrame] : running ? runFrames[strideFrame] : walking ? walkFrames[strideFrame] : LoadSprite();
        JinaArt.Place(body,feet,facing<0);
        body.transform.rotation=Quaternion.identity; weapon.transform.rotation=Quaternion.identity; weapon.color=Color.white;
        // Standing still, his chest rises and falls by one art pixel.
        bool idle=!attacking && !jumping && !walking && !dodging && !hurt && !combat.Paused && combat.PlayerHealth>0;
        float breath=idle && Mathf.Sin(Time.time*2.1f)>0 ? 1 : 0;
        if(breath>0) body.transform.localScale=new Vector3(1,1+.042f/1.9f,1);
        // His feet kick up dust on the run; the backstep leaves a fading trail of himself.
        if(!combat.Paused)
        {
            if(running && Time.time>=nextDust) { nextDust=Time.time+.2f; JinaFx.Dust(feet,-facing*.8f,.72f); }
            if(dodging)
            {
                if(!wasDodging) { JinaFx.Dust(feet,facing*1.2f,1); }
                if(Time.time>=nextGhost) { nextGhost=Time.time+.045f; JinaFx.AfterImage(body,new Color(.7f,.84f,1,.55f)); }
            }
            wasDodging=dodging;
        }
        body.color=source.color==Color.gray ? Color.gray : source.color==Color.red ? new Color(1,.4f,.4f) : Color.white;
        weapon.enabled=!attacking && !jumping && !dodging && !hurt && combat.PlayerHealth>0;
        if(weapon.enabled)
        {
            weapon.sprite=JinaArt.Load(EquippedWeapon);
            PlaceHeldWeapon(facing,walking,running,strideFrame);
        }
        if(!combat.Paused) attackTime+=Time.deltaTime;
    }
    // Texture coordinates from the top left. The near sleeve swings from back to front
    // between frames 1 and 2; the far sleeve swings in the opposite direction.
    static readonly Vector2[] FarWalkPalm={new Vector2(263,347),new Vector2(284,348),new Vector2(72,357),new Vector2(62,334)};
    static readonly Vector2[] FarRunPalm={new Vector2(313,375),new Vector2(333,356),new Vector2(117,385),new Vector2(111,342)};
    static readonly Vector2[] NearWalkPalm={new Vector2(78,395),new Vector2(76,373),new Vector2(279,350),new Vector2(268,347)};
    static readonly Vector2[] NearRunPalm={new Vector2(117,423),new Vector2(125,381),new Vector2(330,378),new Vector2(317,355)};
    void PlaceHeldWeapon(int facing,bool walking,bool running,int frame)
    {
        bool bow=combat.UsingBow;
        bool handNear=bow ? facing<0 : facing>0;
        var palm=handNear ? new Vector2(67,342) : new Vector2(198,345);
        if(running) palm=(handNear ? NearRunPalm : FarRunPalm)[frame];
        else if(walking) palm=(handNear ? NearWalkPalm : FarWalkPalm)[frame];
        weapon.sortingOrder=body.sortingOrder+(handNear ? 1 : -1);
        var sprite=body.sprite;
        palm.y=sprite.texture.height-palm.y;
        var hand=(palm-sprite.rect.min-sprite.pivot)/sprite.pixelsPerUnit;
        hand.x*=facing;
        var grip=weapon.sprite;
        bool sheathed=!bow && swordOutTime<=SheathMotion*.5f;
        var gripPixel=sheathed ? new Vector2(305,grip.texture.height-398) : bow ? new Vector2(134,grip.texture.height-374) : new Vector2(143,grip.texture.height-493);
        var wrap=(gripPixel-grip.rect.min-grip.pivot)/grip.pixelsPerUnit;
        wrap.x*=facing;
        float angle=0;
        if(bow)
        {
            angle=running ? -64f : walking ? -59f : -52f;
            if(walking) angle+=frame==1 ? 4f : frame==3 ? -4f : 0f;
        }
        else if(walking)
        {
            // Lower the blade as the right arm swings back, lift it on the forward swing.
            angle=Mathf.Clamp(hand.x*facing*35f,-16f,16f);
        }
        if(!bow)
        {
            if(sheathed) angle+=150f; // Hilt forward, scabbard trailing behind the hand.
            if(swordOutTime>0 && swordOutTime<SheathMotion)
                angle-=(sheathed ? 16f : 46f)*Mathf.Sin(Mathf.PI*swordOutTime/SheathMotion);
        }
        var rotation=Quaternion.Euler(0,0,angle*facing);
        Vector3 palmWorld=body.transform.TransformPoint(hand);
        float carryScale=bow ? 1f : .92f;
        JinaArt.Place(weapon,palmWorld-rotation*(Vector3)(wrap*carryScale),facing<0);
        weapon.transform.localScale*=carryScale;
        JinaArt.Place(gripFingers,palmWorld,facing<0);
        gripFingers.enabled=handNear;
        gripFingers.color=body.color;
        weapon.transform.rotation=rotation;
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
        weapon.sortingOrder=body.sortingOrder+1;
        weapon.enabled=true;
        weapon.sprite=JinaArt.Load(EquippedWeapon);
        JinaArt.Place(weapon,Vector3.Lerp(Carry(feet,facing,false),feet+new Vector3(facing*.6f,.05f,0),drop*drop),facing<0);
        weapon.transform.rotation=Quaternion.Euler(0,0,Mathf.Lerp(0,combat.UsingBow ? -90 : -40,drop)*facing);
        weapon.color=Color.Lerp(Color.white,Color.gray,drop);
    }
    void OnDestroy()
    {
        if(source!=null) source.forceRenderingOff=false;
        if(body!=null) Destroy(body.gameObject);
        if(weapon!=null) Destroy(weapon.gameObject);
        if(gripFingers!=null) Destroy(gripFingers.gameObject);
        if(fingerSprite!=null) Destroy(fingerSprite);
    }
}
