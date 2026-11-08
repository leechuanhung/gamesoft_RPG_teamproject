using UnityEngine;
// The people of Kongbat village (drawn facing left on the peddler's grid): they turn as PrototypeKongVillage says
// (to the seonbi while talking, to each other in the scene), and Kongjwi darkens once she is unmasked.
public class VillagerVisual : JinaActorVisual
{
    // +1 right, -1 left, 0 as drawn.
    public int lookAt;
    public bool dark;
    protected override void Build() { native=facing=-1; }
    protected override void Pose(ref Frame f,float dx)
    {
        facing=lookAt!=0 ? lookAt : native;
        if(dark) f.tint=new Color(.62f,.7f,.55f);
        f.hitTint=false;
    }
}
