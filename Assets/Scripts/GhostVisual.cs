using UnityEngine;
// A ghost (the camp's guards, or tinted blue for the river's water ghosts): when felled it flashes, reels back and
// breaks up into drifting chunks.
public class GhostVisual : JinaActorVisual
{
    public const float DeathLength=.8f;
    private Sprite[] deaths;
    // When it was felled (negative while it stands).
    public float DownAt { get; private set; }=-1;
    public void Down() => DownAt=Time.time;
    public void Revive() => DownAt=-1;
    // Multiplied over the drawing (white for the camp's guards).
    public Color tint=Color.white;

    protected override void Build() { deaths=JinaArt.Frames("07_Ghost/ghost_death_",3); }
    protected override void Pose(ref Frame f,float dx)
    {
        f.tint=tint;
        if(DownAt<0) return;
        float t=Time.time-DownAt;
        if(t>=DeathLength) { f.hidden=true; return; }
        f.hitTint=false;
        f.sprite=deaths[t<.15f ? 0 : t<.4f ? 1 : 2];
        f.flash=t<.15f ? .85f*(1-t/.15f) : 0;
        if(t>.4f) f.tint.a=1-(t-.4f)/(DeathLength-.4f);
    }
}
