using UnityEngine;
public class SeonbiVisual : MonoBehaviour
{
    private PrototypePlayer player;
    private PrototypeCombat combat;
    private SpriteRenderer source,body,weapon;
    private Sprite[] swordFrames,bowFrames;
    private float attackTime=10;
    private bool bowAttack;
    private int attackFacing=1;
    public const float SwordDuration=.4f,BowDuration=.6f,HitDelay=.2f,BowReleaseDelay=.3f;
    public static Sprite LoadSprite() => JinaArt.Load(JinaArt.Player);
    public static SeonbiVisual Attach(PrototypePlayer player,PrototypeCombat combat)
    {
        var v=player.gameObject.AddComponent<SeonbiVisual>();
        v.player=player; v.combat=combat; v.source=player.GetComponent<SpriteRenderer>();
        v.body=v.MakePart("선비 프레임",5); v.weapon=v.MakePart("장착 무기",6);
        v.swordFrames=JinaArt.Frames("02_Player_Attack_Sword/sword_attack_");
        v.bowFrames=JinaArt.Frames("03_Player_Attack_Bow/bow_attack_");
        v.source.forceRenderingOff=true; v.LateUpdate(); return v;
    }
    SpriteRenderer MakePart(string name,int order)
    {
        var obj=new GameObject(name); obj.transform.SetParent(transform,false);
        var art=obj.AddComponent<SpriteRenderer>(); art.sortingOrder=order; return art;
    }
    public void Attack(bool bow,int facing) { bowAttack=bow; attackFacing=facing; attackTime=0; }
    public void ResetPose() { attackTime=10; }
    void LateUpdate()
    {
        if(body==null) return;
        if(combat.Paused || combat.PlayerHealth<=0) ResetPose();
        float duration=bowAttack ? BowDuration : SwordDuration;
        bool attacking=attackTime<duration;
        int facing=attacking ? attackFacing : player.Facing;
        body.sprite=attacking ? (bowAttack ? bowFrames : swordFrames)[Mathf.Min(3,(int)(attackTime/(duration/4)))] : LoadSprite();
        Vector3 feet=new Vector3(transform.position.x,source.bounds.min.y,0);
        JinaArt.Place(body,feet,transform,facing<0);
        body.color=source.color==Color.gray ? Color.gray : source.color==Color.red ? new Color(1,.4f,.4f) : Color.white;
        weapon.enabled=!attacking && combat.PlayerHealth>0;
        if(weapon.enabled)
        {
            weapon.sprite=JinaArt.Load(combat.UsingBow ? "04_Weapons/bow" : "04_Weapons/hwando");
            JinaArt.Place(weapon,feet+new Vector3(facing*.27f,.75f,0),transform,facing<0);
        }
        if(!combat.Paused) attackTime+=Time.deltaTime;
    }
    void OnDestroy() { if(source!=null) source.forceRenderingOff=false; }
}
