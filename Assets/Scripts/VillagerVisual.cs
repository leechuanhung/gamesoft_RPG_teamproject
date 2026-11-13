using UnityEngine;
// The people of Kongbat village (drawn facing left on the peddler's grid): they turn as PrototypeKongVillage says
// (to the seonbi while talking, to each other in the scene), and Kongjwi darkens once she is unmasked.
// When the demon comes: bound in the curse's cords (trembling while she squeezes them), then lying dead.
// After it: Kongjwi kneeling, bent over in grief, or standing with her eyes down; tears fall from whoever weeps.
public class VillagerVisual : JinaActorVisual
{
    public enum Look { Stand, Bound, Dead, Kneel, Bow, Sad }
    // +1 right, -1 left, 0 as drawn.
    public int lookAt;
    public bool dark, tears, trembling, running;
    public Look look;
    private SpriteRenderer cords;
    private SpriteRenderer[] drops;

    protected override void Build()
    {
        native=facing=-1;
        cords=Extra("저주의 끈",JinaArt.Prop("kong_bind"),5);
        drops=new SpriteRenderer[3];
        for(int i=0;i<drops.Length;i++) drops[i]=Extra("눈물",JinaArt.Dot(),5);
    }
    SpriteRenderer Extra(string name,Sprite sprite,int order)
    {
        var art=new GameObject(name).AddComponent<SpriteRenderer>(); art.sprite=sprite; art.sortingOrder=order;
        art.transform.SetParent(transform,false); art.enabled=false; return art;
    }
    protected override void HideExtras() { cords.enabled=false; foreach(var d in drops) d.enabled=false; }

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
                else if((t%1.4f)<.2f) f.feet.x+=((int)(t/.05f)%2==0 ? 1 : -1)*ArtPixel;   // struggling now and then
                break;
        }
        if(running && (int)(t*12)%2==0) f.feet.y+=ArtPixel;
        if(look==Look.Bow && tears) f.feet.x+=((t%.5f)<.1f ? 1 : 0)*ArtPixel*((int)(t/.03f)%2==0 ? 1 : -1);
        if(tears) Weep(f.feet,t);
    }

    // Two drops at a time falling from the eye (kneeling: hidden behind her hand; bent over: onto the ground).
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
