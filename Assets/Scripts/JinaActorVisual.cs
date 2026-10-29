using UnityEngine;
public class JinaActorVisual : MonoBehaviour
{
    private SpriteRenderer source,picture;
    private PrototypeCombat combat;
    private string kind;
    private Sprite[] attacks;
    private int facing=1;
    private Vector3 last;
    public static JinaActorVisual Attach(SpriteRenderer source,string kind,PrototypeCombat combat)
    {
        var v=source.gameObject.AddComponent<JinaActorVisual>();
        v.source=source; v.kind=kind; v.combat=combat; v.last=source.transform.position;
        var child=new GameObject("표시 이미지"); child.transform.SetParent(source.transform,false);
        v.picture=child.AddComponent<SpriteRenderer>(); v.picture.sortingOrder=3;
        if(kind==JinaArt.Boss) v.attacks=JinaArt.Frames("05_Boss_Brainwashed/boss_attack_");
        source.forceRenderingOff=true; v.Refresh(); return v;
    }
    void LateUpdate() { Refresh(); }
    public void Refresh()
    {
        if(picture==null) return;
        picture.enabled=source.enabled; if(!picture.enabled) return;
        float dx=transform.position.x-last.x;
        if(Mathf.Abs(dx)>.001f && Mathf.Abs(dx)<3) facing=dx>0 ? 1 : -1;
        bool dead=kind==JinaArt.Elder && combat!=null && combat.Quest!=null && combat.Quest.progress.ElderDead;
        bool purified=kind==JinaArt.Boss && combat!=null && combat.BossPurified;
        picture.sprite=JinaArt.Load(purified ? JinaArt.Purified : kind);
        if(kind==JinaArt.Boss && combat!=null)
        {
            facing=combat.BossFacing; int frame=combat.BossAttackFrame;
            if(!purified && frame>=0) picture.sprite=attacks[frame];
        }
        JinaArt.Place(picture,new Vector3(transform.position.x,source.bounds.min.y,0),transform,facing<0);
        picture.transform.rotation=Quaternion.Euler(0,0,dead ? 80 : 0);
        picture.color=dead ? Color.gray : Color.white;
        if(!dead && !purified && source.color.r>.9f && source.color.g>.4f && source.color.g<.8f) picture.color=new Color(1,.7f,.4f);
        if(!dead && !purified && source.color==Color.white) picture.color=new Color(1,.7f,.7f);
        last=transform.position;
    }
}
