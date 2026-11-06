using UnityEngine;
// The ferryman (drawn facing left, like the peddler): turns to the seonbi while they talk, otherwise looks downriver.
public class FerrymanVisual : JinaActorVisual
{
    protected override void Build() { native=facing=-1; }
    protected override void Pose(ref Frame f,float dx)
    {
        if(combat==null) return;
        facing=combat.Ferry!=null && combat.Ferry.IsOpen ? Toward(combat.player.transform.position.x) : native;
    }
}
