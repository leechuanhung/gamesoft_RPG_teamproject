using UnityEngine;
// Draws a Jina character over its hidden placeholder square: each LateUpdate picks a frame, stands it on the
// placeholder's feet, faces it, tints it and lays a white silhouette over it for flashes.
// The ghosts use this as is; the boss, the elder and the peddler add their own motions in subclasses.
public class JinaActorVisual : MonoBehaviour
{
    // One character art pixel in world units (12 px at 546 px per 1.9 units); motions move in whole art pixels.
    protected const float ArtPixel=12f/(546f/1.9f);
    protected SpriteRenderer source,picture,glow;
    protected PrototypeCombat combat;
    protected string kind;
    protected float walkTime;
    // Direction the art is drawn facing, and the direction it faces now (+1 right, -1 left).
    protected int native=1,facing=1;
    protected Vector3 last;

    // What one frame shows. Subclasses start from the idle and change what their motion needs.
    protected struct Frame
    {
        public Sprite sprite;
        public Vector3 feet;
        public float rotation, flash;
        public Color tint, flashColor;
        public bool hidden;
        // Windup orange and hit pink from the placeholder's colour (off once a character is dead or friendly).
        public bool hitTint;
    }

    public static JinaActorVisual Attach(SpriteRenderer source,string kind,PrototypeCombat combat)
    {
        var obj=source.gameObject;
        JinaActorVisual v=kind==JinaArt.Boss ? obj.AddComponent<BossVisual>()
            : kind==JinaArt.Elder ? obj.AddComponent<ElderVisual>()
            : kind==JinaArt.Peddler ? obj.AddComponent<PeddlerVisual>()
            : obj.AddComponent<JinaActorVisual>();
        v.source=source; v.kind=kind; v.combat=combat; v.last=source.transform.position;
        var child=new GameObject("표시 이미지"); child.transform.SetParent(source.transform,false);
        v.picture=child.AddComponent<SpriteRenderer>(); v.picture.sortingOrder=3;
        v.glow=new GameObject("섬광").AddComponent<SpriteRenderer>(); v.glow.sortingOrder=4;
        v.glow.transform.SetParent(child.transform,false);
        v.Build();
        source.forceRenderingOff=true; v.Refresh(); return v;
    }
    // Loads frames and makes extra renderers (props held, effects).
    protected virtual void Build() {}
    // Turns those extra renderers off at the start of every frame; the motion turns on what it shows.
    protected virtual void HideExtras() {}
    // Picks this frame; `dx` is how far the placeholder moved since the last one.
    protected virtual void Pose(ref Frame f,float dx) {}

    void LateUpdate() { Refresh(); }
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
        JinaArt.Place(picture,f.feet,transform,facing!=native);
        if(picture.transform.parent==null) picture.transform.localScale=Vector3.one;
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

    // Frame-to-frame movement; a jump of 3 units or more is a teleport (restart), not a step.
    protected static bool Moving(float dx) => Mathf.Abs(dx)>.001f && Mathf.Abs(dx)<3;
    protected int Toward(float x) => x>=transform.position.x ? 1 : -1;
    protected static Vector3 Snap(Vector3 p) => new Vector3(Mathf.Round(p.x/ArtPixel)*ArtPixel,Mathf.Round(p.y/ArtPixel)*ArtPixel,p.z);
    // Undoes the placeholder's non-uniform scale for a child renderer.
    protected void Unscale(Transform child) => child.localScale=new Vector3(1/transform.lossyScale.x,1/transform.lossyScale.y,1);
}
