using System.Collections.Generic;
using UnityEngine;
public static class JinaArt
{
    public const string Player="01_Player/player_idle", Boss="05_Boss_Brainwashed/boss_brainwashed_idle", Purified="06_Boss_Purified/boss_purified_idle", Ghost="07_Ghost/ghost_guard_idle", Elder="08_Village_Elder/village_elder_idle";
    private static readonly Dictionary<string,Sprite> cache=new Dictionary<string,Sprite>();
    public static Sprite Load(string path)
    {
        Sprite result;
        if(cache.TryGetValue(path,out result) && result!=null) return result;
        var t=Resources.Load<Texture2D>("JinaV4/"+path);
        if(t==null) throw new System.InvalidOperationException("Missing Jina asset: "+path);
        var pixels=t.GetPixels32(); int l=t.width,b=t.height,r=0,top=0;
        for(int y=0;y<t.height;y++) for(int x=0;x<t.width;x++) if(pixels[y*t.width+x].a>32)
        { l=Mathf.Min(l,x); r=Mathf.Max(r,x); b=Mathf.Min(b,y); top=Mathf.Max(top,y); }
        Rect rect=new Rect(l,b,r-l+1,top-b+1);
        Vector2 anchor=new Vector2(rect.center.x,b); float ppu=rect.height/1.9f;
        if(path.Contains("Player_Attack"))
        {
            rect=new Rect(0,0,t.width,t.height); int f=int.Parse(path.Substring(path.Length-2));
            bool sword=path.Contains("Sword");
            anchor=new Vector2(sword ? new[]{350f,330f,330f,340f}[f] : 340f,t.height-(sword ? new[]{689f,689f,680f,679f}[f] : 679f));
            ppu=550f/1.9f;
        }
        else if(path.Contains("boss_attack"))
        {
            int f=int.Parse(path.Substring(path.Length-2));
            rect=new Rect(0,0,f==0 ? 610 : f==1 ? 540 : 768,768);
            anchor=new Vector2(335,24); ppu=500f/2.6f;
        }
        else if(path==Boss || path==Purified) { anchor.x=530; ppu=rect.height/2.6f; }
        else if(path==Ghost) { anchor.x=650; ppu=rect.height/1.65f; }
        else if(path==Elder) { anchor.x=600; ppu=rect.height/1.8f; }
        else if(path.StartsWith("04_Weapons"))
        {
            anchor=rect.center; ppu=path.EndsWith("bow") ? rect.height : rect.width/(path.EndsWith("arrow") ? .7f : 1f);
            if(path.EndsWith("hwando")) anchor=new Vector2(180,t.height-445);
        }
        result=Sprite.Create(t,rect,new Vector2((anchor.x-rect.x)/rect.width,(anchor.y-rect.y)/rect.height),ppu,0,SpriteMeshType.FullRect);
        result.name=path;
        if(path.EndsWith("boss_attack_03"))
        {
            // Hide a disconnected neighboring-frame fragment without changing the PNG.
            Vector2[] v={new Vector2(0,0),new Vector2(768,0),new Vector2(768,318),new Vector2(0,318),new Vector2(230,318),new Vector2(768,318),new Vector2(768,768),new Vector2(230,768)};
            for(int i=0;i<v.Length;i++) v[i]=(v[i]-anchor)/ppu;
            result.OverrideGeometry(v,new ushort[]{0,1,2,0,2,3,4,5,6,4,6,7});
        }
        cache[path]=result; return result;
    }
    public static Sprite[] Frames(string prefix)
    {
        var frames=new Sprite[4]; for(int i=0;i<4;i++) frames[i]=Load(prefix+i.ToString("00")); return frames;
    }
    public static void Place(SpriteRenderer art,Vector3 feet,Transform parent,bool flip)
    {
        art.flipX=flip; art.transform.localScale=new Vector3(1/parent.lossyScale.x,1/parent.lossyScale.y,1); art.transform.position=feet;
    }
}
