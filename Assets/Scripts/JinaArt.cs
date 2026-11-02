using System.Collections.Generic;
using UnityEngine;
public static class JinaArt
{
    public const string Player="01_Player/player_idle", Boss="05_Boss_Brainwashed/boss_brainwashed_idle", Purified="06_Boss_Purified/boss_purified_idle", Ghost="07_Ghost/ghost_guard_idle", Elder="08_Village_Elder/village_elder_idle";
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
        if(path.Contains("Player_Attack") || path.Contains("Player_Jump") || path.Contains("Player_Walk") || path.Contains("boss_attack") || path.Contains("boss_walk"))
        {
            rect=new Rect(0,0,t.width,t.height);
            anchor=new Vector2(t.width*.5f,8);
            if(path.Contains("Player_Attack_Sword")) { anchor.x=270; ppu=450f/1.9f; }
            else if(path.Contains("Player_Attack_Bow")) { anchor.x=300; ppu=510f/1.9f; }
            else if(path.Contains("Player_Jump")) ppu=535f/1.9f;
            else if(path.Contains("Player_Walk")) ppu=546f/1.9f;
            else if(path.Contains("boss_attack")) { anchor=new Vector2(260,b); ppu=480f/2.6f; }
            else ppu=586f/2.6f;
        }
        else if(path==Boss || path==Purified) { anchor.x=rect.center.x; ppu=rect.height/2.6f; }
        else if(path==Ghost) { anchor.x=650; ppu=rect.height/1.65f; }
        else if(path==Elder) { anchor.x=600; ppu=rect.height/1.8f; }
        else if(path.StartsWith("04_Weapons"))
        {
            anchor=rect.center; ppu=path.EndsWith("bow") ? rect.height/BowHeight : rect.width/(path.EndsWith("arrow") ? .7f : 1f);
            if(path.EndsWith("hwando")) anchor=new Vector2(95,95);
        }
        result=Sprite.Create(t,rect,new Vector2((anchor.x-rect.x)/rect.width,(anchor.y-rect.y)/rect.height),ppu,0,SpriteMeshType.FullRect);
        result.name=path;
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
