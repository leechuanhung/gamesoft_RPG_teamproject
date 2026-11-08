using UnityEngine;
// The dokkaebi: walk and attack frames while hostile, the death motion when slain, the transition when purified,
// and, if it had killed the elder, kneeling in grief afterwards (BossRemorse times it).
public class BossVisual : JinaActorVisual
{
    public const float DeathLength=1.7f, PurifyLength=1.6f;
    private Sprite[] attacks,walks,deaths,grieves;
    private SpriteRenderer charm;
    private SpriteRenderer[] dots;

    protected override void Build()
    {
        attacks=JinaArt.Frames("05_Boss_Brainwashed/boss_attack_");
        walks=JinaArt.Frames("05_Boss_Brainwashed/boss_walk_");
        deaths=JinaArt.Frames("05_Boss_Brainwashed/boss_death_",5);
        grieves=JinaArt.Frames("06_Boss_Purified/boss_grieve_",2);
        charm=Child("정화 부적",JinaArt.Prop("charm"));
        dots=new SpriteRenderer[16];
        for(int i=0;i<dots.Length;i++) dots[i]=Child("정화 빛",JinaArt.Dot());
    }
    SpriteRenderer Child(string name,Sprite sprite)
    {
        var art=new GameObject(name).AddComponent<SpriteRenderer>(); art.sortingOrder=5;
        art.sprite=sprite; art.transform.SetParent(transform,false); return art;
    }
    protected override void HideExtras()
    {
        charm.enabled=false;
        foreach(var d in dots) d.enabled=false;
    }
    protected override void Pose(ref Frame f,float dx)
    {
        if(combat==null) return;
        bool purified=combat.BossPurified, slain=combat.EnemyHealth<=0 && !purified;
        if(purified) f.sprite=JinaArt.Load(JinaArt.Purified);
        facing=combat.BossFacing; int frame=combat.BossAttackFrame;
        bool moving=Moving(dx) && !combat.EnemyDefeated;
        if(!combat.Paused) walkTime=moving ? walkTime+Time.deltaTime : 0;
        // Steps quicken with speed: .14 s a frame at the old 1.8 units/s, faster on the chase and the charge.
        float speed=Time.deltaTime>0 ? Mathf.Abs(dx)/Time.deltaTime : 0;
        if(!purified && moving && frame<0) f.sprite=walks[(int)(walkTime/Mathf.Max(.06f,.25f/Mathf.Max(speed,1.8f)))%4];
        if(!purified && frame>=0) f.sprite=attacks[frame];
        // Its roar at the first encounter: the club raised high.
        if(!purified && !slain && combat.Encounter!=null && combat.Encounter.Roaring) f.sprite=attacks[0];
        f.hitTint=!purified && !slain;
        if(slain) Die(ref f,Time.time-combat.BossDownAt);
        else if(purified && Time.time-combat.PurifiedAt<PurifyLength) Purify(ref f,Time.time-combat.PurifiedAt);
        else if(purified && combat.Remorse!=null && combat.Remorse.Active) Grieve(ref f,combat.Remorse.Elapsed);
    }

    // He turns to the elder's body, sinks to his knees and stays there, trembling, tears falling from his shut eye.
    void Grieve(ref Frame f,float t)
    {
        facing=Toward(PrototypeWorld.Elder.x);
        if(t<BossRemorse.Realise) return;
        f.sprite=grieves[t<BossRemorse.Realise+BossRemorse.Kneel ? 0 : 1];
        if(t<BossRemorse.Realise+BossRemorse.Kneel) return;
        if((t%.6f)<.12f) f.feet.x+=((int)(t/.04f)%2==0 ? 1 : -1)*ArtPixel;
        // His shut eye in boss_grieve_01: .465 forward and 1.61 up from the feet.
        var eye=f.feet+new Vector3(facing*.465f,1.61f,0);
        for(int i=0;i<2;i++)
        {
            float life=((t+i*.5f)%1)/.8f;
            if(life>=1) continue;
            var d=dots[i]; d.enabled=true;
            Unscale(d.transform);
            d.transform.position=Snap(eye+new Vector3(facing*ArtPixel*i,-life*1.1f,0));
            d.color=new Color(.75f,.9f,1,1-life*life);
        }
    }

    // Recoil, knees buckle, a shudder on the knees, then the body crumbles away.
    void Die(ref Frame f,float t)
    {
        if(t>=DeathLength) { f.hidden=true; return; }
        f.sprite=deaths[t<.18f ? 0 : t<.42f ? 1 : t<1.05f ? 2 : t<1.3f ? 3 : 4];
        f.flash=t<.18f ? .85f*(1-t/.18f) : 0;
        if(t>=.42f && t<.75f) f.feet.x+=((int)(t/.05f)%2==0 ? 1 : -1)*ArtPixel;
        if(t>1.3f) f.tint.a=1-(t-1.3f)/(DeathLength-1.3f);
    }

    // The curse lifts: the boss shudders as the charm rises before him, dark wisps leave his body,
    // a pale flash turns him into his purified self, and golden sparks drift up.
    void Purify(ref Frame f,float t)
    {
        if(t<.7f) f.sprite=JinaArt.Load(JinaArt.Boss);
        if(t<.6f) f.feet.x+=((int)(t/.05f)%2==0 ? 1 : -1)*ArtPixel*(t<.3f ? 1 : 2);
        f.flash=t<.7f ? .15f+.75f*t/.7f : t<1.3f ? 1-(t-.7f)/.6f : 0;
        f.flashColor=new Color(1,.95f,.8f);
        if(t<.9f)
        {
            charm.enabled=true;
            float rise=1-Mathf.Pow(1-Mathf.Clamp01(t/.6f),3);
            Unscale(charm.transform);
            charm.transform.position=Snap(f.feet+new Vector3(combat.BossFacing*.5f,.5f+.4f*rise,0));
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
            Unscale(d.transform);
            d.transform.position=Snap(f.feet+new Vector3(x,y,0));
            float fade=(1-life)*Mathf.Clamp01((end-t)/.3f);
            d.color=wisp ? new Color(11/255f,9/255f,11/255f,fade) : new Color(1,.84f,.4f,fade);
        }
    }
}
