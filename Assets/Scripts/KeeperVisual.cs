using UnityEngine;
// The tavern keeper is a prop sprite (one texel per art pixel, drawn facing left), so she changes frames on her
// own renderer: run frames while moving (slower steps on the way home), otherwise she stands as drawn
// or turns to the customer she is serving.
public class KeeperVisual : MonoBehaviour
{
    private PrototypeCombat combat;
    private SpriteRenderer art;
    private Sprite idle;
    private Sprite[] runs;
    private float walkTime;
    private Vector3 last;
    public static KeeperVisual Attach(SpriteRenderer art,PrototypeCombat combat)
    {
        var v=art.gameObject.AddComponent<KeeperVisual>();
        v.art=art; v.combat=combat; v.idle=JinaArt.Prop("keeper");
        v.runs=new Sprite[4]; for(int i=0;i<4;i++) v.runs[i]=JinaArt.Prop("keeper_run_"+i.ToString("00"));
        v.ResetPose(); return v;
    }
    // After a teleport (restart) she stands still where she is.
    public void ResetPose() { last=transform.position; walkTime=0; art.sprite=idle; art.flipX=false; }
    void LateUpdate() { Refresh(); }
    public void Refresh()
    {
        float dx=transform.position.x-last.x; last=transform.position;
        if(Mathf.Abs(dx)>.0001f && Mathf.Abs(dx)<3)
        {
            float speed=Time.deltaTime>0 ? Mathf.Abs(dx)/Time.deltaTime : 0;
            walkTime+=Time.deltaTime;
            art.sprite=runs[(int)(walkTime/(speed>2 ? .09f : .2f))%4];
            art.flipX=dx>0;
            return;
        }
        walkTime=0;
        art.sprite=idle;
        art.flipX=combat!=null && combat.Tavern!=null && combat.Tavern.IsOpen && combat.player.transform.position.x>transform.position.x;
    }
}
