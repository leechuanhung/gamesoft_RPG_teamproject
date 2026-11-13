using System.Collections.Generic;
using UnityEngine;
using Who = PrototypeKongVillage.Who;

// What follows the fight with the Kongjwi demon, line by line in speech bubbles (the game goes on meanwhile).
// Purified out on the hill or the pass: Kongjwi wakes on her knees and he tells her it is over (the field scene);
// she goes home once he has left her map. In the village (at once if purified there, or when he next comes):
//   - Patjwi dead: Kongjwi sees her, runs past the other bodies to hers, and falls over it wailing; she stays there.
//   - others dead: she kneels and weeps for them, like the dokkaebi over the elder.
//   - survivors who saw the demon: they cry out that it was Kongjwi and come at her to kill her; Patjwi steps between
//     them and the seonbi tells them the toad is gone; they back away.
//   - nobody saw it: Patjwi comes to her sister.
// Slain in the village: a survivor's cry, and Patjwi's one word.
public class PrototypeKongAftermath : MonoBehaviour
{
    public PrototypeCombat combat;
    public const float LineLength = 3.4f, RunSpeed = 3.4f;
    class Cue { public Who who; public bool seonbi, field, big; public string line; public System.Action act; public System.Func<bool> wait; public float length = LineLength; }
    readonly List<Cue> cues = new List<Cue>();
    private int at = -1;
    private float cueTime;
    private bool fieldPlayed, villagePlayed, slainPlayed;
    public bool Playing => at >= 0 && at < cues.Count;
    // The field scene is over (or there was none): she may go home.
    public bool FieldDone => fieldPlayed && !Playing;
    public string Scene { get; private set; } = "";
    public int CueCount => cues.Count;
    public int CueIndex => at;
    public string CurrentLine => Playing ? cues[at].line : null;
    public string CurrentSpeaker => !Playing ? null : cues[at].seonbi ? "선비" : PrototypeKongVillage.Name(cues[at].who);

    public void ResetAftermath() { cues.Clear(); at = -1; cueTime = 0; fieldPlayed = villagePlayed = slainPlayed = false; Scene = ""; }

    PrototypeKongVillage Kong => combat.Kong;
    PrototypeKongBoss Boss => combat.KongBoss;
    float PlayerX => combat.player.transform.position.x;
    bool PlayerInVillage => PlayerX >= PrototypeWorld.KongStart && PlayerX < PrototypeWorld.KongEnd;

    void Update()
    {
        if (combat == null || Kong == null || Boss == null || combat.PlayerHealth <= 0 || combat.Paused) return;
        if (Playing) { Step(Time.deltaTime); return; }
        if (Boss.Now == PrototypeKongBoss.Phase.Purified && Time.time - Boss.PurifiedAt >= PrototypeKongBoss.PurifyLength)
        {
            bool inVillage = Boss.PurifiedX >= PrototypeWorld.KongStart && Boss.PurifiedX < PrototypeWorld.KongEnd;
            if (!inVillage && !fieldPlayed) { fieldPlayed = true; Field(); return; }
            if (Boss.HandedBack && !villagePlayed && PlayerInVillage && Mathf.Abs(PlayerX - Kong.PersonX(Who.Kongjwi)) < 12)
            { villagePlayed = true; Village(!inVillage); }
        }
        if (Boss.Now == PrototypeKongBoss.Phase.Slain && !slainPlayed && Time.time - Boss.DownAt >= PrototypeKongBoss.DeathLength)
        {
            slainPlayed = true;
            if (PlayerInVillage && Boss.Feet.x < PrototypeWorld.KongEnd) Slain();
        }
    }

    // Runs the current cue for `dt`; the next starts once its time is up and whatever it waits for has happened.
    public void Step(float dt)
    {
        if (!Playing) return;
        cueTime += dt;
        var cue = cues[at];
        if (cueTime < cue.length || (cue.wait != null && !cue.wait())) return;
        Begin(at + 1);
    }
    // Moves straight on to the next line (the checks use it).
    public void Advance() { if (Playing) Begin(at + 1); }
    void Begin(int index)
    {
        at = index; cueTime = 0;
        if (Playing && cues[at].act != null) cues[at].act();
    }
    void Play(string scene) { Scene = scene; Begin(0); }

    Cue Say(Who who, string line, System.Action act = null) { var c = new Cue { who = who, line = line, act = act }; cues.Add(c); return c; }
    Cue Seonbi(string line, System.Action act = null) { var c = new Cue { seonbi = true, line = line, act = act }; cues.Add(c); return c; }
    void Turn(Who who, float x) => Kong.Face(who, x >= Kong.PersonX(who) ? 1 : -1);

    // On the hill or the pass, beside the kneeling girl the toad left behind.
    void Field()
    {
        cues.Clear();
        Say(Who.Kongjwi, "…선비님? 여기가… 저, 저는…").field = true;
        Seonbi("독에 깃든 두꺼비가 그대를 부리고 있었소. 이제 끝났소.");
        Say(Who.Kongjwi, Kong.Lost > 0 ? "…마을. 마을 사람들이… 아니야, 아니야…" : "팥쥐는… 팥쥐는 무사한가요? 집에… 집에 가야 해요.").field = true;
        Play("field");
    }

    void Village(bool cameHome)
    {
        cues.Clear();
        var K = Who.Kongjwi; var P = Who.Patjwi;
        var survivors = new List<Who>();
        foreach (var w in new[] { Who.Hunjang, Who.Yeot, Who.Namu, Who.Halmi, Who.Anak }) if (Kong.Alive(w)) survivors.Add(w);
        survivors.Sort((a, b) => Mathf.Abs(Kong.PersonX(a) - Kong.PersonX(K)).CompareTo(Mathf.Abs(Kong.PersonX(b) - Kong.PersonX(K))));
        int lost = Kong.Lost;
        if (!cameHome) Say(K, "…여, 여기가… 저는…", () => Kong.SetLook(K, VillagerVisual.Look.Kneel));
        if (!Kong.Alive(P))
        {
            // Past every other body to her sister's.
            float body = Kong.PersonX(P) + .9f;
            Say(K, "…팥쥐야?", () => { Kong.SetLook(K, VillagerVisual.Look.Sad); Turn(K, Kong.PersonX(P)); });
            var run = Say(K, "팥쥐야!!", () => { Kong.WalkTo(K, body, RunSpeed); Turn(K, Kong.PersonX(P)); });
            run.length = .6f; run.wait = () => Kong.Arrived(K);
            Say(K, "안 돼… 안 돼, 눈 좀 떠 봐. 언니 왔어. 언니가 왔다니까…", () =>
            {
                Kong.SetLook(K, VillagerVisual.Look.Bow, true); Turn(K, Kong.PersonX(P));
                PrototypeView.Shake(.05f, .4f);
            });
            Say(K, "내가… 내가 그랬어. 어머니도, 너도… 다 내가…");
            var wail = Say(K, "팥쥐야아아아——!!", () => PrototypeView.Shake(.14f, 1.6f));
            wail.big = true; wail.length = 4.5f;
            Play("wail");
            return;
        }
        if (lost > 0)
        {
            Who nearest = P; float best = float.MaxValue;
            for (int i = 1; i < PrototypeKongVillage.People; i++)
                if (!Kong.Alive((Who)i) && Mathf.Abs(Kong.PersonX((Who)i) - Kong.PersonX(K)) < best) { best = Mathf.Abs(Kong.PersonX((Who)i) - Kong.PersonX(K)); nearest = (Who)i; }
            Say(K, "저, 저 사람들… 아주머니… 아저씨…", () => { Kong.SetLook(K, VillagerVisual.Look.Sad); Turn(K, Kong.PersonX(nearest)); });
            Say(K, "내가… 내가 그런 거예요? 이 손으로…", () => Kong.SetLook(K, VillagerVisual.Look.Kneel, true));
            Say(K, "용서해 주세요… 용서해 주세요…");
        }
        if (Kong.Raided && survivors.Count > 0)
        {
            var s1 = survivors[0]; var s2 = survivors.Count > 1 ? survivors[1] : s1;
            float kx = Kong.PersonX(K);
            float side = 0; foreach (var s in survivors) side += Kong.PersonX(s) - kx;
            int toward = side >= 0 ? 1 : -1;
            Say(s1, "콩, 콩쥐?! 저 괴물 속에서 콩쥐가 나왔어!", () => { foreach (var s in survivors) Turn(s, kx); });
            Say(s2, "저게 콩쥐였다고? 우리를 다 잡아먹으려던 게?!");
            // They close in on her, each a step behind the last; the line holds until they are there.
            var anger = Say(s1, "저 요물을 살려 둘 수 없소! 죽여야 하오!", () =>
            {
                for (int i = 0; i < survivors.Count; i++) Kong.WalkTo(survivors[i], kx + toward * (2.6f + i * .9f), 3f);
            });
            anger.wait = () => survivors.TrueForAll(s => Kong.Arrived(s));
            // Patjwi runs in from wherever she was and stands between them.
            var run = Say(P, "언니!", () => { Kong.WalkTo(P, kx + toward * 1.1f, RunSpeed); Turn(P, kx + toward * 1.1f); });
            run.length = .6f; run.wait = () => Kong.Arrived(P);
            Say(P, "그만해요! 언니 잘못이 아니에요… 저 두꺼비가 언니를 삼키고 있었던 거예요!", () => Kong.Face(P, toward));
            Seonbi("물러서시오. 사람을 해친 것은 독에 깃든 두꺼비요. 그것은 은비녀에 타서 사라졌소.");
            Say(P, "어머니 비녀가… 언니를 돌려준 거예요. 그러니까 제발…");
            Say(s1, "…허, 허나…");
            Say(s2, lost > 0 ? "…선비님 말씀이 그러하다면. 허나 죽은 이들은 어찌하오…" : "…알겠소. 선비님 말씀이 그러하다면.", () =>
            {
                foreach (var s in survivors) { Kong.WalkTo(s, PrototypeKongVillage.Spot(s).x, 1.1f); Kong.Face(s, 0); }
            });
            Say(K, "…미안해요. 정말… 미안해요.", () => { Kong.SetLook(K, VillagerVisual.Look.Kneel, true); Turn(K, Kong.PersonX(P)); });
        }
        else
        {
            // Nobody else saw the demon (or nobody else is left): her sister comes to her.
            float kx = Kong.PersonX(K); int side = Kong.PersonX(P) >= kx ? 1 : -1;
            Say(P, Kong.Raided ? "…언니. 언니였구나… 그 안에." : "…언니? 언니 맞아?", () => { Kong.WalkTo(P, kx + side * 1.1f, 2.2f); Kong.Face(P, -side); });
            Say(K, "팥쥐야… 미안해. 다 기억나. 어머니 일도…", () => { Kong.SetLook(K, VillagerVisual.Look.Kneel, true); Turn(K, kx + side); });
            Say(P, Kong.Raided ? "…알아. 언니 탓 아니야. …알아." : "…됐어. 돌아왔으면 됐어.");
        }
        Say(K, "……", () => { Kong.SetLook(K, VillagerVisual.Look.Sad); }).length = 1.2f;
        Play("village");
    }

    void Slain()
    {
        cues.Clear();
        var survivors = new List<Who>();
        foreach (var w in new[] { Who.Hunjang, Who.Yeot, Who.Namu, Who.Halmi, Who.Anak }) if (Kong.Alive(w)) survivors.Add(w);
        if (survivors.Count > 0) Say(survivors[0], "괴, 괴물이… 쓰러졌다…!");
        if (Kong.Alive(Who.Patjwi)) Say(Who.Patjwi, "……언니.", () => Turn(Who.Patjwi, Boss.Feet.x));
        if (cues.Count > 0) Play("slain");
    }

    void OnGUI()
    {
        if (!Playing || combat == null || combat.Paused || combat.PlayerHealth <= 0) return;
        PrototypeKoreanUI.Apply();
        var cue = cues[at];
        var head = cue.seonbi ? combat.player.transform.position + Vector3.up * 1.3f
            : cue.field ? Boss.Feet + Vector3.up * 1.3f : Kong.Head(cue.who);
        if (cue.big)
        {
            // Her wail over Patjwi: the screen dims red and pulses with it.
            float pulse = .14f + .08f * Mathf.Sin(cueTime * 9);
            JinaUI.Fill(new Rect(0, 0, JinaUI.W, JinaUI.H), new Color(.3f, 0, .02f, pulse * Mathf.Clamp01(cueTime / .3f)));
        }
        if (SpeechBubble.OnScreen(head)) SpeechBubble.Draw(head, CurrentSpeaker, cue.line);
    }
}
