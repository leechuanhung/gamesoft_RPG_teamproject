using UnityEngine;
using System.Collections.Generic;

// ===== 공통 바탕: 원래 사각형 위에 그림을 얹는다 (JinaActorVisual) =====
// 숨겨 둔 사각형(자리 표시) 위에 인물 그림을 그린다. LateUpdate마다 프레임을 고르고, 사각형의 발 위치에 세우고,
// 방향을 맞추고, 색을 입히고, 번쩍일 때는 흰 실루엣을 덮는다.
// 인물마다 하위 클래스에서 자기 동작을 더한다(BossVisual, ElderVisual, PeddlerVisual, GhostVisual).
public class JinaActorVisual : MonoBehaviour
{
    // 인물 그림 1도트의 월드 크기(1.9칸에 546px일 때 12px). 동작은 도트 단위로 움직인다.
    protected const float ArtPixel=12f/(546f/1.9f);
    protected SpriteRenderer source,picture,glow;
    protected PrototypeCombat combat;
    protected string kind;
    protected float walkTime;
    // 그림이 그려진 방향과 지금 보는 방향(+1 오른쪽, -1 왼쪽).
    protected int native=1,facing=1;
    protected Vector3 last;
    public SpriteRenderer Picture => picture;

    // 한 프레임에 보여 줄 것. 하위 클래스는 대기 자세에서 시작해 동작에 필요한 것만 바꾼다.
    protected struct Frame
    {
        public Sprite sprite;
        public Vector3 feet;
        public float rotation, flash;
        public Color tint, flashColor;
        public bool hidden;
        // 사각형 색에서 읽은 준비 동작(주황)과 피격(분홍). 죽었거나 우호적이면 끈다.
        public bool hitTint;
    }

    // 사각형에 인물 그림을 붙인다. `kind`로 어떤 인물인지 고른다.
    public static JinaActorVisual Attach(SpriteRenderer source,string kind,PrototypeCombat combat)
    {
        var obj=source.gameObject;
        JinaActorVisual v=kind==JinaArt.Boss ? obj.AddComponent<BossVisual>()
            : kind==JinaArt.Ghost ? obj.AddComponent<GhostVisual>()
            : kind==JinaArt.Elder ? obj.AddComponent<ElderVisual>()
            : kind==JinaArt.Peddler ? obj.AddComponent<PeddlerVisual>()
            : kind==JinaArt.Ferryman ? obj.AddComponent<FerrymanVisual>()
            : kind.StartsWith("16_Kong/") ? obj.AddComponent<VillagerVisual>()
            : obj.AddComponent<JinaActorVisual>();
        v.source=source; v.kind=kind; v.combat=combat; v.last=source.transform.position;
        var child=new GameObject("표시 이미지"); child.transform.SetParent(source.transform,false);
        v.picture=child.AddComponent<SpriteRenderer>(); v.picture.sortingOrder=3;
        v.glow=new GameObject("섬광").AddComponent<SpriteRenderer>(); v.glow.sortingOrder=4;
        v.glow.transform.SetParent(child.transform,false);
        v.Build();
        source.forceRenderingOff=true; v.Refresh(); return v;
    }
    // 프레임을 불러오고 추가 렌더러(든 물건, 효과)를 만든다.
    protected virtual void Build() {}
    // 프레임마다 추가 렌더러를 끈다. 동작이 보여 줄 것만 다시 켠다.
    protected virtual void HideExtras() {}
    // 이번 프레임을 고른다. `dx`는 지난 프레임 뒤 사각형이 움직인 거리.
    protected virtual void Pose(ref Frame f,float dx) {}

    void LateUpdate() { Refresh(); }
    // 지금 상태로 바로 다시 그린다.
    public void Refresh()
    {
        if(picture==null) return;
        glow.enabled=false;
        HideExtras();
        picture.enabled=source.enabled; if(!picture.enabled) return;
        float dx=transform.position.x-last.x;
        if(Moving(dx)) facing=dx>0 ? 1 : -1;
        var f=new Frame
        {
            sprite=JinaArt.Load(kind), feet=new Vector3(transform.position.x,source.bounds.min.y,0),
            tint=Color.white, flashColor=Color.white, hitTint=true,
        };
        Pose(ref f,dx);
        last=transform.position;
        if(f.hidden) { picture.enabled=false; return; }
        picture.sprite=f.sprite;
        JinaArt.Place(picture,f.feet,facing!=native);
        // 가만히 서 있으면 가슴이 1도트씩 오르내린다. 인물마다 박자가 다르다.
        if(f.sprite==JinaArt.Load(kind) && f.rotation==0 && (combat==null || !combat.Paused))
        {
            float breath=Mathf.Sin(Time.time*2.1f+transform.position.x*1.7f)>0 ? 1 : 0;
            var scale=picture.transform.localScale; scale.y*=1+breath*ArtPixel/1.9f; picture.transform.localScale=scale;
        }
        picture.transform.rotation=Quaternion.Euler(0,0,f.rotation);
        picture.color=f.tint;
        if(f.hitTint && source.color.r>.9f && source.color.g>.4f && source.color.g<.8f) picture.color=new Color(1,.7f,.4f);
        if(f.hitTint && source.color==Color.white) picture.color=new Color(1,.7f,.7f);
        if(f.flash>0)
        {
            glow.enabled=true; glow.sprite=JinaArt.Silhouette(picture.sprite); glow.flipX=picture.flipX;
            f.flashColor.a=f.flash*f.tint.a; glow.color=f.flashColor;
        }
    }

    // 프레임 사이 이동량. 3칸 이상 뛰면 걸음이 아니라 순간이동(재시작)으로 본다.
    protected static bool Moving(float dx) => Mathf.Abs(dx)>.001f && Mathf.Abs(dx)<3;
    protected int Toward(float x) => x>=transform.position.x ? 1 : -1;
    protected static Vector3 Snap(Vector3 p) => new Vector3(Mathf.Round(p.x/ArtPixel)*ArtPixel,Mathf.Round(p.y/ArtPixel)*ArtPixel,p.z);
    // 자식 렌더러에 걸린 사각형의 비균일 크기를 되돌린다.
    protected void Unscale(Transform child) => child.localScale=new Vector3(1/transform.lossyScale.x,1/transform.lossyScale.y,1);
}

// ===== 도깨비 (BossVisual) =====
// 도깨비: 적일 때는 걷기·공격 프레임, 토벌되면 쓰러짐, 정화되면 변하는 연출,
// 노인을 죽였다면 그 뒤 무릎 꿇고 슬퍼함(시간은 BossRemorse가 잰다).
public class BossVisual : JinaActorVisual
{
    public const float DeathLength=1.7f, PurifyLength=1.6f;
    private Sprite[] attacks,walks,deaths,grieves;
    private SpriteRenderer charm;
    private SpriteRenderer[] dots;

    // 도깨비 프레임과 부적·불티 렌더러를 준비한다.
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
    // 그림을 얹은 자식 렌더러를 만든다.
    SpriteRenderer Child(string name,Sprite sprite)
    {
        var art=new GameObject(name).AddComponent<SpriteRenderer>(); art.sortingOrder=5;
        art.sprite=sprite; art.transform.SetParent(transform,false); return art;
    }
    // 부적·불티 렌더러를 끈다.
    protected override void HideExtras()
    {
        charm.enabled=false;
        foreach(var d in dots) d.enabled=false;
    }
    // 도깨비 상태(걷기·공격·포효·쓰러짐·정화·애도)에 맞는 프레임을 고른다.
    protected override void Pose(ref Frame f,float dx)
    {
        if(combat==null) return;
        bool purified=combat.BossPurified, slain=combat.EnemyHealth<=0 && !purified;
        if(purified) f.sprite=JinaArt.Load(JinaArt.Purified);
        facing=combat.BossFacing; int frame=combat.BossAttackFrame;
        bool moving=Moving(dx) && !combat.EnemyDefeated;
        if(!combat.Paused) walkTime=moving ? walkTime+Time.deltaTime : 0;
        // 빠를수록 걸음이 빨라진다: 예전 초속 1.8칸에서 프레임당 .14초, 추격·돌진 때는 더 빠르게.
        float speed=Time.deltaTime>0 ? Mathf.Abs(dx)/Time.deltaTime : 0;
        if(!purified && moving && frame<0) f.sprite=walks[(int)(walkTime/Mathf.Max(.06f,.25f/Mathf.Max(speed,1.8f)))%4];
        if(!purified && frame>=0) f.sprite=attacks[frame];
        // 첫 조우의 포효: 몽둥이를 높이 든다.
        if(!purified && !slain && combat.Encounter!=null && combat.Encounter.Roaring) f.sprite=attacks[0];
        f.hitTint=!purified && !slain;
        float sinceHit=combat.BossHitAt<0 ? 1000 : Time.time-combat.BossHitAt;
        if(!purified && !slain && sinceHit<.11f)
        {
            f.flash=.8f*(1-sinceHit/.11f);
            f.feet.x+=(combat.player.transform.position.x<transform.position.x ? 1 : -1)*ArtPixel;
        }
        if(slain) Die(ref f,Time.time-combat.BossDownAt);
        else if(purified && Time.time-combat.PurifiedAt<PurifyLength) Purify(ref f,Time.time-combat.PurifiedAt);
        else if(purified && combat.Remorse!=null && combat.Remorse.Active) Grieve(ref f,combat.Remorse.Elapsed);
    }

    // 노인 시체 쪽으로 돌아서 무릎을 꿇고, 떨면서 감은 눈에서 눈물을 흘린다.
    void Grieve(ref Frame f,float t)
    {
        facing=Toward(PrototypeWorld.Elder.x);
        if(t<BossRemorse.Realise) return;
        f.sprite=grieves[t<BossRemorse.Realise+BossRemorse.Kneel ? 0 : 1];
        if(t<BossRemorse.Realise+BossRemorse.Kneel) return;
        if((t%.6f)<.12f) f.feet.x+=((int)(t/.04f)%2==0 ? 1 : -1)*ArtPixel;
        // boss_grieve_01의 감은 눈 위치: 발에서 앞으로 .465, 위로 1.61.
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

    // 움찔하고, 무릎이 꺾이고, 무릎 꿇은 채 떨다가 몸이 부서져 사라진다.
    void Die(ref Frame f,float t)
    {
        if(t>=DeathLength) { f.hidden=true; return; }
        f.sprite=deaths[t<.18f ? 0 : t<.42f ? 1 : t<1.05f ? 2 : t<1.3f ? 3 : 4];
        f.flash=t<.18f ? .85f*(1-t/.18f) : 0;
        if(t>=.42f && t<.75f) f.feet.x+=((int)(t/.05f)%2==0 ? 1 : -1)*ArtPixel;
        if(t>1.3f) f.tint.a=1-(t-1.3f)/(DeathLength-1.3f);
    }

    // 저주가 풀린다: 부적이 앞에 떠오르자 몸을 떨고, 검은 기운이 빠져나가고,
    // 희미한 빛과 함께 정화된 모습으로 바뀌고, 금빛 불티가 떠오른다.
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

// ===== 노인 (ElderVisual) =====
// 마을 노인: 말하는 상대를 본다. 경보가 울리면 지팡이를 떨어뜨리고 도깨비 쪽으로 칼을 뽑는다.
// 맞으면 휘청이다 90도 쓰러져 시체로 눕고, 맞은 방향을 그대로 본다.
public class ElderVisual : JinaActorVisual
{
    public const float FallLength=.85f, DrawLength=.8f;
    // 마을이 안전해진 뒤 이만큼 지나서 칼을 거둔다(도깨비 동작이 먼저 끝나도록).
    public const float SheatheDelay=1.8f;
    private Sprite[] hits,draws;
    private Sprite down;
    private SpriteRenderer sword,cane;
    // 시체는 맞을 때 보던 방향을 지킨다. 누가 옮기거나 쪽지를 읽어도 그대로다(살아 있으면 0).
    private int deadFacing;

    // 노인 프레임과 지팡이·칼 렌더러를 준비한다.
    protected override void Build()
    {
        hits=JinaArt.Frames("08_Village_Elder/elder_hit_",2);
        draws=JinaArt.Frames("08_Village_Elder/elder_draw_",2);
        down=JinaArt.Load("08_Village_Elder/elder_down");
        // 기운 그림이다. 크기가 비균일한 사각형 아래에서 돌리면 납작하게 찌그러진다.
        picture.transform.SetParent(null,false);
        sword=Prop("노인의 환도","04_Weapons/hwando",4);
        cane=Prop("노인의 지팡이","08_Village_Elder/elder_cane",2);
    }
    // 노인이 드는 물건(지팡이, 칼) 렌더러를 만든다.
    static SpriteRenderer Prop(string name,string art,int order)
    {
        var r=new GameObject(name).AddComponent<SpriteRenderer>(); r.sortingOrder=order;
        r.sprite=JinaArt.Load(art); return r;
    }
    protected override void HideExtras() { sword.enabled=false; cane.enabled=false; }

    // 노인 상태(대화·경보·칼 뽑기·맞음·시체)에 맞는 프레임을 고른다.
    protected override void Pose(ref Frame f,float dx)
    {
        if(combat==null || combat.Quest==null) return;
        bool dead=combat.Quest.progress.ElderDead;
        var village=combat.Village;
        float alarm=village!=null && village.Alarmed ? Time.time-village.AlarmAt : -1;
        bool armed=alarm>=0 && (!village.Cleared || Time.time-village.ClearAt<SheatheDelay);
        if(!dead) deadFacing=0;
        else if(deadFacing==0) deadFacing=Toward(combat.BossPosition.x);
        facing=dead ? deadFacing : combat.Quest.TalkingToElder ? Toward(combat.player.transform.position.x)
            : armed ? Toward(combat.BossPosition.x) : native;
        f.hitTint=!dead;
        if(dead) Fall(ref f,Time.time-combat.Quest.ElderStruckAt,alarm>=0);
        else if(armed) Draw(ref f,alarm);
    }

    // 지팡이를 든 채 놀라고, 지팡이가 앞으로 쓰러지면서 옆에서 칼이 올라온다.
    void Draw(ref Frame f,float t)
    {
        if(t<.3f) { f.sprite=JinaArt.Load("08_Village_Elder/elder_startle"); return; }
        f.sprite=draws[t<.55f ? 0 : 1];
        float swing=Mathf.Clamp01((t-.3f)/(DrawLength-.3f));
        Hold(sword,Hand(f.feet),Mathf.Lerp(-130,0,1-(1-swing)*(1-swing)));
        float fall=Mathf.Clamp01((t-.3f)/.3f);
        Hold(cane,Cane(f.feet),-90*fall*fall);
    }

    // 번쩍이며 휘청이고, 무릎이 꺾이고, 발을 축으로 90도 기운다(갓이 얼굴 위로 미끄러짐).
    // 기울면서 몸을 올려 등이 땅에 닿게 하고, 시체 그림(같은 그림을 돌린 것)으로 눕는다.
    void Fall(ref Frame f,float t,bool hadSword)
    {
        var feet=f.feet;
        var corpse=JinaArt.Load("08_Village_Elder/elder_corpse");
        float rest=corpse.pivot.y/corpse.pixelsPerUnit;
        f.sprite=down; f.tint=Color.gray;
        if(t<.45f) { f.sprite=hits[t<.2f ? 0 : 1]; f.tint=Color.white; f.flash=t<.2f ? .9f*(1-t/.2f) : 0; }
        else if(t<FallLength)
        {
            float k=(t-.45f)/(FallLength-.45f);
            f.rotation=90*k*k*facing; f.tint=Color.Lerp(Color.white,Color.gray,k);
            f.feet.y+=rest*Mathf.Sin(90*k*k*Mathf.Deg2Rad);
        }
        else { f.sprite=corpse; f.feet.y+=rest; }
        if(!hadSword) return;
        // 칼이 손에서 떨어져 옆에 놓인다. 지팡이는 이미 떨어진 자리에 있다.
        float drop=Mathf.Clamp01(t/.35f);
        Hold(sword,Vector3.Lerp(Hand(feet),feet+new Vector3(facing*.55f,.04f,0),drop*drop),Mathf.Lerp(0,-40,drop));
        Hold(cane,Cane(feet),-90);
    }

    // 지팡이를 쥐었던 주먹: 발에서 앞으로 .30, 위로 .68(그림 기준 1칸에 617px).
    Vector3 Hand(Vector3 feet) => Snap(feet+new Vector3(facing*.30f,.68f,0));
    // 지팡이 끝: 발에서 앞으로 (818-600)/617.
    Vector3 Cane(Vector3 feet) => feet+new Vector3(facing*.35f,0,0);
    // 든 물건을 손 위치에 각도를 주어 놓는다.
    void Hold(SpriteRenderer art,Vector3 at,float angle)
    {
        art.enabled=true; art.flipX=facing<0; art.transform.position=at;
        art.transform.rotation=Quaternion.Euler(0,0,angle*facing);
        art.color=Color.white;
    }

    // 부모 없이 둔 그림을 같이 지운다.
    void OnDestroy()
    {
        if(picture!=null) Destroy(picture.gameObject);
        if(sword!=null) Destroy(sword.gameObject);
        if(cane!=null) Destroy(cane.gameObject);
    }
}

// ===== 보부상 (PeddlerVisual) =====
// 보부상(왼쪽을 보고 그려짐): 대피할 때 뛰고, 집에 갈 때 천천히 걷고, 그 밖에는 손님을 보거나
// (애도 중에는 쓰러진 노인을 보거나) 그려진 그대로 선다.
public class PeddlerVisual : JinaActorVisual
{
    private Sprite[] runs;

    // 보부상 프레임을 준비한다.
    protected override void Build()
    {
        native=facing=-1;
        runs=JinaArt.Frames("12_Peddler/peddler_run_");
    }
    // 보부상 상태(대피·귀가·손님·애도)에 맞는 프레임을 고른다.
    protected override void Pose(ref Frame f,float dx)
    {
        if(combat==null) return;
        bool moving=Moving(dx);
        float speed=Time.deltaTime>0 ? Mathf.Abs(dx)/Time.deltaTime : 0;
        if(!combat.Paused) walkTime=moving ? walkTime+Time.deltaTime : 0;
        if(moving) f.sprite=runs[(int)(walkTime/(speed>2 ? .09f : .2f))%4];
        else if(combat.Village!=null && combat.Village.IsMourning(source)) facing=Toward(PrototypeWorld.Elder.x);
        else facing=combat.Peddler!=null && combat.Peddler.IsOpen ? Toward(combat.player.transform.position.x) : native;
        // 마을은 사각형의 알파값으로 집 문 앞에서 그를 나타나고 사라지게 한다.
        f.tint.a=source.color.a;
    }
}

// ===== 주모 (KeeperVisual) =====
// 주모는 기물 스프라이트(그림 1도트당 텍셀 1개, 왼쪽을 보고 그려짐)라서 자기 렌더러에서 프레임을 바꾼다:
// 움직일 때는 뛰는 프레임(집에 갈 때는 느린 걸음), 그 밖에는 그려진 대로 서거나
// 상대하는 손님 쪽, 애도 중에는 쓰러진 노인 쪽으로 돈다.
public class KeeperVisual : MonoBehaviour
{
    private PrototypeCombat combat;
    private SpriteRenderer art;
    private Sprite idle;
    private Sprite[] runs;
    private float walkTime;
    private Vector3 last;
    // 주모 스프라이트에 프레임 담당을 붙인다.
    public static KeeperVisual Attach(SpriteRenderer art,PrototypeCombat combat)
    {
        var v=art.gameObject.AddComponent<KeeperVisual>();
        v.art=art; v.combat=combat; v.idle=JinaArt.Prop("keeper");
        v.runs=new Sprite[4]; for(int i=0;i<4;i++) v.runs[i]=JinaArt.Prop("keeper_run_"+i.ToString("00"));
        v.ResetPose(); return v;
    }
    // 순간이동(재시작) 뒤에는 그 자리에 가만히 선다.
    public void ResetPose() { last=transform.position; walkTime=0; art.sprite=idle; art.flipX=false; }
    void LateUpdate() { Refresh(); }
    // 주모의 프레임과 방향을 지금 상태로 맞춘다.
    public void Refresh()
    {
        float dx=transform.position.x-last.x; last=transform.position;
        if(Mathf.Abs(dx)>.0001f && Mathf.Abs(dx)<3)
        {
            float speed=Time.deltaTime>0 ? Mathf.Abs(dx)/Time.deltaTime : 0;
            walkTime+=Time.deltaTime;
            art.sprite=runs[(int)(walkTime/(speed>2 ? .09f : .2f))%4];
            art.flipX=dx>0;
            return;
        }
        walkTime=0;
        art.sprite=idle;
        // 애도 중에는 쓰러진 노인을, 그 밖에는 상대하는 손님을 본다.
        if(combat!=null && combat.Village!=null && combat.Village.IsMourning(art)) art.flipX=PrototypeWorld.Elder.x>transform.position.x;
        else art.flipX=combat!=null && combat.Tavern!=null && combat.Tavern.IsOpen && combat.player.transform.position.x>transform.position.x;
    }
}

// ===== 뱃사공 (FerrymanVisual) =====
// 뱃사공(보부상처럼 왼쪽을 보고 그려짐): 대화 중에는 선비를 보고, 그 밖에는 강 아래쪽을 본다.
public class FerrymanVisual : JinaActorVisual
{
    protected override void Build() { native=facing=-1; }
    // 뱃사공이 대화 중이면 선비를, 아니면 강 아래쪽을 본다.
    protected override void Pose(ref Frame f,float dx)
    {
        if(combat==null) return;
        facing=combat.Ferry!=null && combat.Ferry.IsOpen ? Toward(combat.player.transform.position.x) : native;
    }
}

// ===== 콩밭 마을 사람들 (VillagerVisual) =====
// 콩밭 마을 사람들(보부상 격자에 왼쪽을 보고 그려짐): PrototypeKongVillage가 정한 쪽을 본다
// (대화 중에는 선비를, 장면에서는 서로를). 콩쥐는 정체가 드러나면 어두워진다.
// 요괴가 오면: 저주의 끈에 묶이고(요괴가 조이면 떤다), 그다음 죽어 눕는다.
// 싸움 뒤: 콩쥐는 무릎 꿇거나, 엎드려 울거나, 눈을 내리깔고 선다. 우는 사람에게서는 눈물이 떨어진다.
public class VillagerVisual : JinaActorVisual
{
    public enum Look { Stand, Bound, Dead, Kneel, Bow, Sad }
    // +1 오른쪽, -1 왼쪽, 0이면 그려진 방향.
    public int lookAt;
    public bool dark, tears, trembling, running;
    public Look look;
    private SpriteRenderer cords;
    private SpriteRenderer[] drops;

    // 마을 사람 프레임과 끈·눈물 렌더러를 준비한다.
    protected override void Build()
    {
        native=facing=-1;
        cords=Extra("저주의 끈",JinaArt.Prop("kong_bind"),5);
        drops=new SpriteRenderer[3];
        for(int i=0;i<drops.Length;i++) drops[i]=Extra("눈물",JinaArt.Dot(),5);
    }
    // 끈·눈물 같은 추가 렌더러를 만든다.
    SpriteRenderer Extra(string name,Sprite sprite,int order)
    {
        var art=new GameObject(name).AddComponent<SpriteRenderer>(); art.sprite=sprite; art.sortingOrder=order;
        art.transform.SetParent(transform,false); art.enabled=false; return art;
    }
    protected override void HideExtras() { cords.enabled=false; foreach(var d in drops) d.enabled=false; }

    // 마을 사람 상태(대화·묶임·죽음·애도)에 맞는 프레임을 고른다.
    protected override void Pose(ref Frame f,float dx)
    {
        facing=lookAt!=0 ? lookAt : native;
        if(dark) f.tint=new Color(.62f,.7f,.55f);
        f.hitTint=false;
        float t=Time.time+transform.position.x;
        switch(look)
        {
            case Look.Dead: f.sprite=JinaArt.Load(kind+"_down"); facing=native; return;
            case Look.Kneel: f.sprite=JinaArt.Load(kind+"_kneel"); break;
            case Look.Bow: f.sprite=JinaArt.Load(kind+"_bow"); break;
            case Look.Sad: f.sprite=JinaArt.Load(kind+"_sad"); break;
            case Look.Bound:
                f.tint=new Color(.78f,.72f,.88f);
                cords.enabled=true; Unscale(cords.transform);
                cords.transform.position=Snap(f.feet+new Vector3(0,.1f,0));
                cords.color=new Color(1,1,1,.75f+.25f*Mathf.Sin(t*5));
                if(trembling) f.feet.x+=((int)(t/.04f)%2==0 ? 1 : -1)*ArtPixel;
                else if((t%1.4f)<.2f) f.feet.x+=((int)(t/.05f)%2==0 ? 1 : -1)*ArtPixel;   // 가끔 몸부림친다
                break;
        }
        if(running && (int)(t*12)%2==0) f.feet.y+=ArtPixel;
        if(look==Look.Bow && tears) f.feet.x+=((t%.5f)<.1f ? 1 : 0)*ArtPixel*((int)(t/.03f)%2==0 ? 1 : -1);
        if(tears) Weep(f.feet,t);
    }

    // 눈에서 두 방울씩 떨어진다(무릎 꿇으면 손에 가려지고, 엎드리면 땅으로).
    void Weep(Vector3 feet,float t)
    {
        var eye=feet+new Vector3(facing*(look==Look.Bow ? .42f : .26f),look==Look.Bow ? .62f : look==Look.Kneel ? .92f : 1.25f,0);
        for(int i=0;i<drops.Length;i++)
        {
            float life=((t*1.2f+i*.37f)%1)/.8f;
            if(life>=1) continue;
            var d=drops[i]; d.enabled=true; Unscale(d.transform);
            d.transform.position=Snap(eye+new Vector3(facing*ArtPixel*(i%2),-life*(look==Look.Bow ? .55f : .8f),0));
            d.color=new Color(.75f,.9f,1,1-life*life);
        }
    }
}

// ===== 유령·물귀신 (GhostVisual) =====
// 유령(들판의 수호 유령, 또는 파랗게 물든 강의 물귀신). 공격: 준비하며 뒤로 젖혔다가
// 발톱을 휘두르며 덤빈다. 쓰러지면 번쩍이고, 휘청이고, 조각으로 흩어져 떠간다.
public class GhostVisual : JinaActorVisual
{
    public const float DeathLength=.8f;
    private Sprite[] deaths;
    // 쓰러진 시각(서 있는 동안은 음수).
    public float DownAt { get; private set; }=-1;
    public void Down() => DownAt=Time.time;
    public void Revive() { DownAt=-1; Winding=false; StruckAt=-1; HurtAt=-1; }
    // 맞고도 서 있으면 잠깐 번쩍인다.
    public float HurtAt=-1;
    public void Hurt() { HurtAt=Time.time; }
    // PrototypeCamp가 정하는 공격: AimX 쪽으로 준비 동작, StruckAt부터 휘두르기.
    public const float SwipeLength=.14f, StrikeLength=.32f, Lunge=.3f;
    public bool Winding;
    public float AimX, StruckAt=-1;
    private Sprite[] attacks;
    // 휘두르기를 시작한다(PrototypeCamp가 부름).
    public void Strike()
    {
        Winding=false; StruckAt=Time.time;
        // 발톱의 작은 초승달, 유령 자기 색으로.
        int dir=Toward(AimX);
        JinaFx.Arc(transform.position+new Vector3(dir*.7f,.1f,0),dir,-30,.62f,Color.Lerp(new Color(.9f,.82f,1),tint,.5f),.14f);
    }
    // 그림에 곱하는 색(들판 유령은 흰색).
    public Color tint=Color.white;

    protected override void Build() { deaths=JinaArt.Frames("07_Ghost/ghost_death_",3); attacks=JinaArt.Frames("07_Ghost/ghost_attack_",3); }
    // 유령 상태(떠다님·준비·휘두르기·쓰러짐)에 맞는 프레임을 고른다.
    protected override void Pose(ref Frame f,float dx)
    {
        f.tint=tint;
        if(DownAt<0)
        {
            float hurt=HurtAt<0 ? 1000 : Time.time-HurtAt;
            if(hurt<.12f) f.flash=.85f*(1-hurt/.12f);
            float since=StruckAt<0 ? 1000 : Time.time-StruckAt;
            if(Winding) { facing=Toward(AimX); f.sprite=attacks[0]; }
            else if(since<StrikeLength)
            {
                facing=Toward(AimX);
                f.sprite=attacks[since<SwipeLength ? 1 : 2];
                f.feet.x+=facing*Lunge*(1-since/StrikeLength);
            }
            return;
        }
        float t=Time.time-DownAt;
        if(t>=DeathLength) { f.hidden=true; return; }
        f.hitTint=false;
        f.sprite=deaths[t<.15f ? 0 : t<.4f ? 1 : 2];
        f.flash=t<.15f ? .85f*(1-t/.15f) : 0;
        if(t>.4f) f.tint.a=1-(t-.4f)/(DeathLength-.4f);
    }
}

// ===== 일반 몬스터 (MonsterVisual) =====
// 팩 그림에 작은 절차적 움직임을 더한다. 전투 판정 범위는 기존 그대로.
public class MonsterVisual : MonoBehaviour
{
    private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
    private SpriteRenderer hitArea, picture;
    private Vector3 lastPosition;
    private float width;
    private float phase;

    // 몬스터 그림을 불러온다.
    public static Sprite Load(string name)
    {
        Sprite sprite;
        if (cache.TryGetValue(name, out sprite) && sprite != null) return sprite;
        var texture = Resources.Load<Texture2D>("MonsterArt/" + name);
        if (texture == null) return null;
        var pixels = texture.GetPixels32();
        int left = texture.width, bottom = texture.height, right = -1, top = -1;
        for (int y = 0; y < texture.height; y++)
            for (int x = 0; x < texture.width; x++)
                if (pixels[y * texture.width + x].a > 0)
                {
                    left = Mathf.Min(left, x); right = Mathf.Max(right, x);
                    bottom = Mathf.Min(bottom, y); top = Mathf.Max(top, y);
                }
        if (right < left) return null;
        sprite = Sprite.Create(texture, new Rect(left, bottom, right - left + 1, top - bottom + 1),
            new Vector2(.5f, 0), 32, 0, SpriteMeshType.FullRect);
        sprite.name = name;
        cache[name] = sprite;
        return sprite;
    }

    // 몬스터 사각형에 그림을 붙인다.
    public static void Attach(SpriteRenderer root, string name, float visualWidth)
    {
        var sprite = Load(name);
        if (sprite == null) { Debug.LogWarning("Monster art missing: " + name); return; }
        var component = root.gameObject.AddComponent<MonsterVisual>();
        component.hitArea = root;
        component.width = visualWidth;
        component.lastPosition = root.transform.position;
        var child = new GameObject("몬스터 외형");
        child.transform.SetParent(root.transform, false);
        component.picture = child.AddComponent<SpriteRenderer>();
        component.picture.sprite = sprite;
        component.picture.sortingOrder = 2;
        root.forceRenderingOff = true;
        component.LateUpdate();
    }

    // 매 프레임 몬스터 그림을 사각형에 맞춰 움직인다.
    void LateUpdate()
    {
        if (hitArea == null || picture == null) return;
        picture.enabled = hitArea.enabled;
        if (!picture.enabled) { lastPosition = transform.position; return; }
        float movement = transform.position.x - lastPosition.x;
        if (Mathf.Abs(movement) > .0001f && Mathf.Abs(movement) < 2f)
            picture.flipX = movement > 0; // 원본 동물 그림은 왼쪽을 본다.
        bool teleported = Vector3.Distance(transform.position, lastPosition) > 2f;
        if (teleported) { phase = 0; picture.flipX = false; }
        var combat = FindObjectOfType<PrototypeCombat>();
        bool paused = combat != null && (combat.PlayerHealth <= 0 || combat.Paused);
        if (!paused) phase += Time.deltaTime * (Mathf.Abs(movement) > .0001f ? 12f : 3f);
        float bounce = paused ? 0 : Mathf.Abs(Mathf.Sin(phase)) * (Mathf.Abs(movement) > .0001f ? .07f : .015f);
        float uniform = width / picture.sprite.bounds.size.x;
        picture.transform.localScale = new Vector3(uniform / transform.lossyScale.x, uniform / transform.lossyScale.y, 1);
        picture.transform.position = new Vector3(transform.position.x, hitArea.bounds.min.y + bounce, 0);
        var color = hitArea.color;
        bool warning = color.r > .9f && color.g > .4f && color.g < .8f;
        picture.color = warning ? new Color(1f,.6f,.25f) : Color.white;
        // 짧게 밝아지는 것으로 기존 보스 피격 느낌을 유지한다.
        if (color == Color.white) picture.color = new Color(1f,.65f,.65f);
        lastPosition = transform.position;
    }
}
