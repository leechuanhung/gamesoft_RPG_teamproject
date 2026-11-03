using UnityEngine;
// The village elder: faces whoever he talks to; on the alarm drops his cane and draws a sword toward the boss;
// when struck reels back, falls a quarter turn and lies as his corpse, facing the way he was struck.
public class ElderVisual : JinaActorVisual
{
    public const float FallLength=.85f, DrawLength=.8f;
    // He puts the sword away this long after the village is safe (the boss's own motion plays first).
    public const float SheatheDelay=1.8f;
    private Sprite[] hits,draws;
    private SpriteRenderer sword,cane;
    // The body keeps the direction he faced when struck, whoever moves or reads his note (0 while alive).
    private int deadFacing;

    protected override void Build()
    {
        hits=JinaArt.Frames("08_Village_Elder/elder_hit_",2);
        draws=JinaArt.Frames("08_Village_Elder/elder_draw_",2);
        // He is drawn tipped over; rotating under the non-uniformly scaled marker would shear him flat.
        picture.transform.SetParent(null,false);
        sword=Prop("노인의 환도","04_Weapons/hwando",4);
        cane=Prop("노인의 지팡이","08_Village_Elder/elder_cane",2);
    }
    static SpriteRenderer Prop(string name,string art,int order)
    {
        var r=new GameObject(name).AddComponent<SpriteRenderer>(); r.sortingOrder=order;
        r.sprite=JinaArt.Load(art); return r;
    }
    protected override void HideExtras() { sword.enabled=false; cane.enabled=false; }

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

    // Startled with his cane, then the cane tips over in front of him and the sword comes up from his side.
    void Draw(ref Frame f,float t)
    {
        if(t<.3f) { f.sprite=JinaArt.Load("08_Village_Elder/elder_startle"); return; }
        f.sprite=draws[t<.55f ? 0 : 1];
        float swing=Mathf.Clamp01((t-.3f)/(DrawLength-.3f));
        Hold(sword,Hand(f.feet),Mathf.Lerp(-130,0,1-(1-swing)*(1-swing)));
        float fall=Mathf.Clamp01((t-.3f)/.3f);
        Hold(cane,Cane(f.feet),-90*fall*fall);
    }

    // Flash and reel back, knees give way, then he tips a quarter turn about his feet, rising as he goes so his
    // back lands on the ground, and lies as the corpse drawing (the same body, already turned).
    void Fall(ref Frame f,float t,bool hadSword)
    {
        var feet=f.feet;
        var corpse=JinaArt.Load("08_Village_Elder/elder_corpse");
        float rest=corpse.pivot.y/corpse.pixelsPerUnit;
        f.sprite=draws[0]; f.tint=Color.gray;
        if(t<.45f) { f.sprite=hits[t<.2f ? 0 : 1]; f.tint=Color.white; f.flash=t<.2f ? .9f*(1-t/.2f) : 0; }
        else if(t<FallLength)
        {
            float k=(t-.45f)/(FallLength-.45f);
            f.rotation=90*k*k*facing; f.tint=Color.Lerp(Color.white,Color.gray,k);
            f.feet.y+=rest*Mathf.Sin(90*k*k*Mathf.Deg2Rad);
        }
        else { f.sprite=corpse; f.feet.y+=rest; }
        if(!hadSword) return;
        // The sword drops from his hand to lie beside him; the cane already lies where it fell.
        float drop=Mathf.Clamp01(t/.35f);
        Hold(sword,Vector3.Lerp(Hand(feet),feet+new Vector3(facing*.55f,.04f,0),drop*drop),Mathf.Lerp(0,-40,drop));
        Hold(cane,Cane(feet),-90);
    }

    // His fist, where the cane was held: .30 forward and .68 up from the feet (from the art at 617 px per unit).
    Vector3 Hand(Vector3 feet) => Snap(feet+new Vector3(facing*.30f,.68f,0));
    // The cane's tip, (818-600)/617 forward of his feet.
    Vector3 Cane(Vector3 feet) => feet+new Vector3(facing*.35f,0,0);
    void Hold(SpriteRenderer art,Vector3 at,float angle)
    {
        art.enabled=true; art.flipX=facing<0; art.transform.position=at;
        art.transform.rotation=Quaternion.Euler(0,0,angle*facing);
        art.color=Color.white;
    }

    void OnDestroy()
    {
        if(picture!=null) Destroy(picture.gameObject);
        if(sword!=null) Destroy(sword.gameObject);
        if(cane!=null) Destroy(cane.gameObject);
    }
}
