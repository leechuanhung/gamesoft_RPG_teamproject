using System.Collections.Generic;
using UnityEngine;
public static class JinaArt
{
    public const string Player="01_Player/player_idle", Boss="05_Boss_Brainwashed/boss_brainwashed_idle", Purified="06_Boss_Purified/boss_purified_idle", Ghost="07_Ghost/ghost_guard_idle", Elder="08_Village_Elder/village_elder_idle", Peddler="12_Peddler/peddler_idle";
    // Match the v6 drawn bow (~440 px) and nocked arrow (312 px above feet).
    public const float BowHeight=440f/(510f/1.9f), BowArrowHeight=312f/(510f/1.9f);
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
        // V6 files use separate canvas sizes; anchors are measured in source pixels.
        if(path.Contains("Player_Attack") || path.Contains("Player_Jump") || path.Contains("Player_Walk") || path.Contains("Player_Run") || path.Contains("boss_attack") || path.Contains("boss_walk"))
        {
            rect=new Rect(0,0,t.width,t.height);
            anchor=new Vector2(t.width*.5f,8);
            if(path.Contains("Player_Attack_Sword")) { anchor.x=270; ppu=450f/1.9f; }
            else if(path.Contains("Player_Attack_Bow")) { anchor.x=300; ppu=510f/1.9f; }
            else if(path.Contains("Player_Jump")) ppu=535f/1.9f;
            // Run frames are built from the walk frames, so they share its scale and feet line.
            else if(path.Contains("Player_Walk") || path.Contains("Player_Run")) ppu=546f/1.9f;
            else if(path.Contains("boss_attack")) { anchor=new Vector2(260,b); ppu=480f/2.6f; }
            else ppu=586f/2.6f;
        }
        // Death and hit frames are cut from the idle art on its own canvas, so they keep the idle's scale and feet line.
        else if(path.Contains("boss_death")) { var idle=Load(Boss); rect=new Rect(0,0,t.width,t.height); anchor=new Vector2(351,8); ppu=idle.pixelsPerUnit; }
        // The corpse is cropped to the body; its pivot is where his feet were, so it rests on the ground below that.
        else if(path.EndsWith("elder_corpse")) { anchor=new Vector2(1166,600); ppu=Load(Elder).pixelsPerUnit; }
        // Elder frames keep the idle canvas; the cut-out cane pivots on its own tip.
        else if(path.StartsWith("08_Village_Elder/elder_")) { var idle=Load(Elder); rect=new Rect(0,0,t.width,t.height); anchor=new Vector2(path.EndsWith("cane") ? 818 : 600,idle.rect.y); ppu=idle.pixelsPerUnit; }
        // Run frames add one 12 px block on each side and above the idle box.
        else if(path.Contains("peddler_run")) { var idle=Load(Peddler); rect=new Rect(0,0,t.width,t.height); anchor=new Vector2(108+12,0); ppu=idle.pixelsPerUnit; }
        else if(path==Boss || path==Purified) { anchor.x=rect.center.x; ppu=rect.height/2.6f; }
        else if(path==Ghost) { anchor.x=650; ppu=rect.height/1.65f; }
        else if(path==Elder) { anchor.x=600; ppu=rect.height/1.8f; }
        // Feet sit under the peddler, not under the carrier frame on his back; pole tips add height.
        else if(path==Peddler) { anchor.x=108; ppu=rect.height/1.95f; }
        else if(path.StartsWith("04_Weapons"))
        {
            anchor=rect.center; ppu=path.EndsWith("bow") ? rect.height/BowHeight : rect.width/(path.EndsWith("arrow") ? .7f : 1f);
            if(path.EndsWith("hwando")) anchor=new Vector2(95,95);
        }
        result=Sprite.Create(t,rect,new Vector2((anchor.x-rect.x)/rect.width,(anchor.y-rect.y)/rect.height),ppu,0,SpriteMeshType.FullRect);
        result.name=path;
        cache[path]=result; return result;
    }
    // Props are drawn one texel per art pixel; characters use ~12 px art pixels at 546 px per 1.9 units.
    public const float PropPixelsPerUnit=546f/1.9f/12f;
    public static Sprite Prop(string name)
    {
        string path="13_Props/"+name; Sprite result;
        if(cache.TryGetValue(path,out result) && result!=null) return result;
        var t=Resources.Load<Texture2D>("JinaV4/"+path);
        if(t==null) throw new System.InvalidOperationException("Missing Jina asset: "+path);
        result=Sprite.Create(t,new Rect(0,0,t.width,t.height),new Vector2(.5f,0),PropPixelsPerUnit,0,SpriteMeshType.FullRect);
        result.name=path;
        cache[path]=result; return result;
    }
    public static Sprite[] Frames(string prefix,int count=4)
    {
        var frames=new Sprite[count]; for(int i=0;i<count;i++) frames[i]=Load(prefix+i.ToString("00")); return frames;
    }
    // A white copy of a sprite (same rect, pivot and scale) laid over it for flashes.
    public static Sprite Silhouette(Sprite sprite)
    {
        string path="white:"+sprite.name; Sprite result;
        if(cache.TryGetValue(path,out result) && result!=null) return result;
        var t=new Texture2D(sprite.texture.width,sprite.texture.height,TextureFormat.RGBA32,false){filterMode=FilterMode.Point};
        var pixels=sprite.texture.GetPixels32();
        for(int i=0;i<pixels.Length;i++) pixels[i]=new Color32(255,255,255,pixels[i].a);
        t.SetPixels32(pixels); t.Apply();
        result=Sprite.Create(t,sprite.rect,new Vector2(sprite.pivot.x/sprite.rect.width,sprite.pivot.y/sprite.rect.height),sprite.pixelsPerUnit,0,SpriteMeshType.FullRect);
        result.name=path;
        cache[path]=result; return result;
    }
    // One square effect dot, two character art pixels wide (the size of the boss's crumbling chunks).
    public const float DotSize=2*12f/(546f/1.9f);
    public static Sprite Dot()
    {
        Sprite result;
        if(cache.TryGetValue("dot",out result) && result!=null) return result;
        var t=Texture2D.whiteTexture;
        result=Sprite.Create(t,new Rect(0,0,t.width,t.height),new Vector2(.5f,.5f),t.width/DotSize,0,SpriteMeshType.FullRect);
        cache["dot"]=result; return result;
    }
    public static void Place(SpriteRenderer art,Vector3 feet,Transform parent,bool flip)
    {
        art.flipX=flip; art.transform.localScale=new Vector3(1/parent.lossyScale.x,1/parent.lossyScale.y,1); art.transform.position=feet;
    }
}
