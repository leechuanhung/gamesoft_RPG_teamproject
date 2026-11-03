using System.Collections.Generic;
using UnityEngine;
// Loads the Jina art from Resources/JinaV4 as sprites standing on their feet, at the scale of each character.
public static class JinaArt
{
    public const string Player="01_Player/player_idle", Boss="05_Boss_Brainwashed/boss_brainwashed_idle", Purified="06_Boss_Purified/boss_purified_idle", Ghost="07_Ghost/ghost_guard_idle", Elder="08_Village_Elder/village_elder_idle", Peddler="12_Peddler/peddler_idle";
    // Match the v6 drawn bow (~440 px) and nocked arrow (312 px above feet).
    public const float BowHeight=440f/(510f/1.9f), BowArrowHeight=312f/(510f/1.9f);
    // Props are drawn one texel per art pixel; characters use ~12 px art pixels at 546 px per 1.9 units.
    public const float PropPixelsPerUnit=546f/1.9f/12f;
    // One square effect dot, two character art pixels wide (the size of the boss's crumbling chunks).
    public const float DotSize=2*12f/(546f/1.9f);
    private static readonly Dictionary<string,Sprite> cache=new Dictionary<string,Sprite>();

    static Sprite Cached(string key,System.Func<Sprite> make)
    {
        Sprite result;
        if(cache.TryGetValue(key,out result) && result!=null) return result;
        result=make(); result.name=key;
        cache[key]=result; return result;
    }
    static Texture2D Texture(string path)
    {
        var t=Resources.Load<Texture2D>("JinaV4/"+path);
        if(t==null) throw new System.InvalidOperationException("Missing Jina asset: "+path);
        return t;
    }

    // A character picture. `anchor` (the feet) and `rect` are in source pixels; ppu sets the on-screen height.
    public static Sprite Load(string path) => Cached(path,()=>
    {
        var t=Texture(path);
        var bounds=Opaque(t);
        Rect rect=bounds; Vector2 anchor=new Vector2(bounds.center.x,bounds.y); float ppu=bounds.height/1.9f;
        var canvas=new Rect(0,0,t.width,t.height);
        // Animation frames on one shared canvas per animation, feet 8 px above the bottom.
        if(path.Contains("Player_Attack") || path.Contains("Player_Jump") || path.Contains("Player_Walk") || path.Contains("Player_Run") || path.Contains("boss_attack") || path.Contains("boss_walk"))
        {
            rect=canvas; anchor=new Vector2(t.width*.5f,8);
            if(path.Contains("Player_Attack_Sword")) { anchor.x=270; ppu=450f/1.9f; }
            else if(path.Contains("Player_Attack_Bow")) { anchor.x=300; ppu=510f/1.9f; }
            else if(path.Contains("Player_Jump")) ppu=535f/1.9f;
            // Run frames are built from the walk frames, so they share its scale and feet line.
            else if(path.Contains("Player_Walk") || path.Contains("Player_Run")) ppu=546f/1.9f;
            else if(path.Contains("boss_attack")) { anchor=new Vector2(260,bounds.y); ppu=480f/2.6f; }
            else ppu=586f/2.6f;
        }
        // Frames cut from an idle keep that idle's scale and feet line (see Tools/ArtGen).
        else if(path.Contains("boss_death")) { rect=canvas; anchor=new Vector2(351,8); ppu=Load(Boss).pixelsPerUnit; }
        // The corpse is cropped to the body; its pivot is where his feet were, so it rests on the ground below that.
        else if(path.EndsWith("elder_corpse")) { anchor=new Vector2(1166,600); ppu=Load(Elder).pixelsPerUnit; }
        // Elder frames keep the idle canvas; the cut-out cane pivots on its own tip.
        else if(path.StartsWith("08_Village_Elder/elder_"))
        {
            var idle=Load(Elder);
            rect=canvas; anchor=new Vector2(path.EndsWith("cane") ? 818 : 600,idle.rect.y); ppu=idle.pixelsPerUnit;
        }
        // Run frames add one 12 px block on each side and above the idle box.
        else if(path.Contains("peddler_run")) { rect=canvas; anchor=new Vector2(108+12,0); ppu=Load(Peddler).pixelsPerUnit; }
        // Idles, cropped to the opaque body; the anchor sits under the feet, not under what they carry.
        else if(path==Boss || path==Purified) ppu=bounds.height/2.6f;
        else if(path==Ghost) { anchor.x=650; ppu=bounds.height/1.65f; }
        else if(path==Elder) { anchor.x=600; ppu=bounds.height/1.8f; }
        // Feet sit under the peddler, not under the carrier frame on his back; pole tips add height.
        else if(path==Peddler) { anchor.x=108; ppu=bounds.height/1.95f; }
        else if(path.StartsWith("04_Weapons"))
        {
            anchor=bounds.center; ppu=path.EndsWith("bow") ? bounds.height/BowHeight : bounds.width/(path.EndsWith("arrow") ? .7f : 1f);
            if(path.EndsWith("hwando")) anchor=new Vector2(95,95);
        }
        return Sprite.Create(t,rect,new Vector2((anchor.x-rect.x)/rect.width,(anchor.y-rect.y)/rect.height),ppu,0,SpriteMeshType.FullRect);
    });
    // The box of pixels more than slightly opaque; the V6 art has a faint halo around it.
    static Rect Opaque(Texture2D t)
    {
        var pixels=t.GetPixels32(); int l=t.width,b=t.height,r=0,top=0;
        for(int y=0;y<t.height;y++) for(int x=0;x<t.width;x++) if(pixels[y*t.width+x].a>32)
        { l=Mathf.Min(l,x); r=Mathf.Max(r,x); b=Mathf.Min(b,y); top=Mathf.Max(top,y); }
        return new Rect(l,b,r-l+1,top-b+1);
    }
    public static Sprite[] Frames(string prefix,int count=4)
    {
        var frames=new Sprite[count]; for(int i=0;i<count;i++) frames[i]=Load(prefix+i.ToString("00")); return frames;
    }

    // A prop (13_Props), standing on the bottom centre of its texture.
    public static Sprite Prop(string name) => Cached("13_Props/"+name,()=>
    {
        var t=Texture("13_Props/"+name);
        return Sprite.Create(t,new Rect(0,0,t.width,t.height),new Vector2(.5f,0),PropPixelsPerUnit,0,SpriteMeshType.FullRect);
    });

    // A white copy of a sprite (same rect, pivot and scale) laid over it for flashes.
    public static Sprite Silhouette(Sprite sprite) => Cached("white:"+sprite.name,()=>
    {
        var t=new Texture2D(sprite.texture.width,sprite.texture.height,TextureFormat.RGBA32,false){filterMode=FilterMode.Point};
        var pixels=sprite.texture.GetPixels32();
        for(int i=0;i<pixels.Length;i++) pixels[i]=new Color32(255,255,255,pixels[i].a);
        t.SetPixels32(pixels); t.Apply();
        return Sprite.Create(t,sprite.rect,new Vector2(sprite.pivot.x/sprite.rect.width,sprite.pivot.y/sprite.rect.height),sprite.pixelsPerUnit,0,SpriteMeshType.FullRect);
    });
    public static Sprite Dot() => Cached("dot",()=>
    {
        var t=Texture2D.whiteTexture;
        return Sprite.Create(t,new Rect(0,0,t.width,t.height),new Vector2(.5f,.5f),t.width/DotSize,0,SpriteMeshType.FullRect);
    });

    // Stands `art` (a child of `parent`) on `feet`, undoing the parent's scale.
    public static void Place(SpriteRenderer art,Vector3 feet,Transform parent,bool flip)
    {
        art.flipX=flip; art.transform.localScale=new Vector3(1/parent.lossyScale.x,1/parent.lossyScale.y,1); art.transform.position=feet;
    }
}
