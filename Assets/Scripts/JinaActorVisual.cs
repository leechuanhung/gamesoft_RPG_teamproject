using UnityEngine;
public class JinaActorVisual : MonoBehaviour
{
    // Scripted motions, in seconds: the boss crumbling, the curse lifting, the elder falling.
    public const float BossDeathLength=1.7f, PurifyLength=1.6f, ElderFallLength=.85f;
    const float ArtPixel=12f/(546f/1.9f);
    private SpriteRenderer source,picture,glow,charm;
    private SpriteRenderer[] dots;
    private PrototypeCombat combat;
    private string kind;
    private Sprite[] attacks,walks,deaths,hits;
    private float walkTime;
    private int facing=1;
    private Vector3 last;
    public static JinaActorVisual Attach(SpriteRenderer source,string kind,PrototypeCombat combat)
    {
        var v=source.gameObject.AddComponent<JinaActorVisual>();
        v.source=source; v.kind=kind; v.combat=combat; v.last=source.transform.position;
        var child=new GameObject("표시 이미지"); child.transform.SetParent(source.transform,false);
        v.picture=child.AddComponent<SpriteRenderer>(); v.picture.sortingOrder=3;
        v.glow=new GameObject("섬광").AddComponent<SpriteRenderer>(); v.glow.sortingOrder=4;
        v.glow.transform.SetParent(child.transform,false);
        if(kind==JinaArt.Boss)
        {
            v.attacks=JinaArt.Frames("05_Boss_Brainwashed/boss_attack_");
            v.walks=JinaArt.Frames("05_Boss_Brainwashed/boss_walk_");
            v.deaths=JinaArt.Frames("05_Boss_Brainwashed/boss_death_",5);
            v.charm=new GameObject("정화 부적").AddComponent<SpriteRenderer>(); v.charm.sortingOrder=5;
            v.charm.sprite=JinaArt.Prop("charm"); v.charm.transform.SetParent(source.transform,false);
            v.dots=new SpriteRenderer[16];
            for(int i=0;i<v.dots.Length;i++)
            {
                v.dots[i]=new GameObject("정화 빛").AddComponent<SpriteRenderer>(); v.dots[i].sortingOrder=5;
                v.dots[i].sprite=JinaArt.Dot(); v.dots[i].transform.SetParent(source.transform,false);
            }
        }
        if(kind==JinaArt.Elder)
        {
            v.hits=JinaArt.Frames("08_Village_Elder/elder_hit_",2);
            // He is drawn tipped over; rotating under the non-uniformly scaled marker would shear him flat.
            child.transform.SetParent(null,false);
        }
        source.forceRenderingOff=true; v.Refresh(); return v;
    }
    void LateUpdate() { Refresh(); }
    public void Refresh()
    {
        if(picture==null) return;
        glow.enabled=false;
        if(charm!=null) charm.enabled=false;
        if(dots!=null) foreach(var d in dots) d.enabled=false;
        picture.enabled=source.enabled; if(!picture.enabled) return;
        float dx=transform.position.x-last.x;
        if(Mathf.Abs(dx)>.001f && Mathf.Abs(dx)<3) facing=dx>0 ? 1 : -1;
        bool dead=kind==JinaArt.Elder && combat!=null && combat.Quest!=null && combat.Quest.progress.ElderDead;
        bool purified=kind==JinaArt.Boss && combat!=null && combat.BossPurified;
        bool slain=kind==JinaArt.Boss && combat!=null && combat.EnemyHealth<=0 && !purified;
        picture.sprite=JinaArt.Load(purified ? JinaArt.Purified : kind);
        if(kind==JinaArt.Boss && combat!=null)
        {
            facing=combat.BossFacing; int frame=combat.BossAttackFrame;
            bool moving=Mathf.Abs(dx)>.001f && Mathf.Abs(dx)<3 && !combat.EnemyDefeated;
            if(!combat.Paused) walkTime=moving ? walkTime+Time.deltaTime : 0;
            if(!purified && moving && frame<0) picture.sprite=walks[(int)(walkTime/.14f)%4];
            if(!purified && frame>=0) picture.sprite=attacks[frame];
        }
        var feet=new Vector3(transform.position.x,source.bounds.min.y,0);
        float rotation=dead ? 80 : 0, flash=0;
        Color tint=dead ? Color.gray : Color.white;
        var flashColor=Color.white;
        if(slain)
        {
            // Recoil, knees buckle, a shudder on the knees, then the body crumbles away.
            float t=Time.time-combat.BossDownAt;
            if(t>=BossDeathLength) { picture.enabled=false; last=transform.position; return; }
            picture.sprite=deaths[t<.18f ? 0 : t<.42f ? 1 : t<1.05f ? 2 : t<1.3f ? 3 : 4];
            flash=t<.18f ? .85f*(1-t/.18f) : 0;
            if(t>=.42f && t<.75f) feet.x+=((int)(t/.05f)%2==0 ? 1 : -1)*ArtPixel;
            if(t>1.3f) tint.a=1-(t-1.3f)/(BossDeathLength-1.3f);
        }
        else if(purified && Time.time-combat.PurifiedAt<PurifyLength) Purify(Time.time-combat.PurifiedAt,ref feet,ref flash,ref flashColor);
        if(dead && Time.time-combat.Quest.ElderStruckAt<ElderFallLength)
        {
            // Struck: flash and reel back, knees give way, then he topples into the fallen pose.
            float t=Time.time-combat.Quest.ElderStruckAt;
            facing=1;
            if(t<.45f) { picture.sprite=hits[t<.2f ? 0 : 1]; rotation=0; tint=Color.white; flash=t<.2f ? .9f*(1-t/.2f) : 0; }
            else { float k=(t-.45f)/(ElderFallLength-.45f); rotation=80*k*k; tint=Color.Lerp(Color.white,Color.gray,k); }
        }
        // Tipped backwards about the feet, the back of the body would sink into the ground.
        if(rotation>0) feet.y+=.32f*Mathf.Sin(rotation*Mathf.Deg2Rad);
        JinaArt.Place(picture,feet,transform,facing<0);
        if(picture.transform.parent==null) picture.transform.localScale=Vector3.one;
        picture.transform.rotation=Quaternion.Euler(0,0,rotation);
        picture.color=tint;
        if(!dead && !purified && !slain && source.color.r>.9f && source.color.g>.4f && source.color.g<.8f) picture.color=new Color(1,.7f,.4f);
        if(!dead && !purified && !slain && source.color==Color.white) picture.color=new Color(1,.7f,.7f);
        if(flash>0)
        {
            glow.enabled=true; glow.sprite=JinaArt.Silhouette(picture.sprite); glow.flipX=picture.flipX;
            flashColor.a=flash*tint.a; glow.color=flashColor;
        }
        last=transform.position;
    }
    // The curse lifts: the boss shudders as the charm rises before him, dark wisps leave his body,
    // a pale flash turns him into his purified self, and golden sparks drift up.
    void Purify(float t,ref Vector3 feet,ref float flash,ref Color flashColor)
    {
        if(t<.7f) picture.sprite=JinaArt.Load(JinaArt.Boss);
        if(t<.6f) feet.x+=((int)(t/.05f)%2==0 ? 1 : -1)*ArtPixel*(t<.3f ? 1 : 2);
        flash=t<.7f ? .15f+.75f*t/.7f : t<1.3f ? 1-(t-.7f)/.6f : 0;
        flashColor=new Color(1,.95f,.8f);
        if(t<.9f)
        {
            charm.enabled=true;
            float rise=1-Mathf.Pow(1-Mathf.Clamp01(t/.6f),3);
            charm.transform.localScale=new Vector3(1/transform.lossyScale.x,1/transform.lossyScale.y,1);
            charm.transform.position=Snap(feet+new Vector3(combat.BossFacing*.5f,.5f+.4f*rise,0));
            charm.color=new Color(1,1,1,Mathf.Clamp01(t/.2f)*(t>.6f ? 1-(t-.6f)/.3f : 1));
        }
        for(int i=0;i<dots.Length;i++)
        {
            bool wisp=i<8;
            float start=wisp ? 0 : .6f, end=wisp ? .9f : PurifyLength;
            if(t<start || t>=end) continue;
            float life=((t-start)*(wisp ? 1.6f : 1.1f)+i*.137f)%1;
            float x=Mathf.Sin(i*2.3f)*.75f, y=.5f+life*(wisp ? 2.4f : 2.8f);
            var d=dots[i]; d.enabled=true;
            d.transform.localScale=new Vector3(1/transform.lossyScale.x,1/transform.lossyScale.y,1);
            d.transform.position=Snap(feet+new Vector3(x,y,0));
            float fade=(1-life)*Mathf.Clamp01((end-t)/.3f);
            d.color=wisp ? new Color(11/255f,9/255f,11/255f,fade) : new Color(1,.84f,.4f,fade);
        }
    }
    void OnDestroy() { if(picture!=null && picture.transform.parent==null) Destroy(picture.gameObject); }
    static Vector3 Snap(Vector3 p) => new Vector3(Mathf.Round(p.x/ArtPixel)*ArtPixel,Mathf.Round(p.y/ArtPixel)*ArtPixel,p.z);
}
