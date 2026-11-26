using System;
using System.Collections.Generic;
using UnityEngine;

// 배경 음악과 효과음(Resources/JinaSound). 팀원이 만든 소리가 먼저이고, 비어 있던 곳은 Tools/ArtGen/make_sound.py로
// 합성한 소리(bgm_pass, sfx_charm, sfx_w_… 등)로 채웠다.
// 배경 음악은 지금 선 맵과 연출·보스전에 따라 고르고 바뀔 때 천천히 엇갈려 바꾼다.
// 효과음은 `Play`로 어디서든 울리고(Combat이 붙인 것이 없으면 조용히 넘어간다), 보스의 공격처럼 세상 한 자리에서
// 나는 소리는 `PlayAt`으로 선비가 있는 맵에서만 울린다. 시각이 기록되는 일(봉화, 정화,
// 변신 …)은 여기서 그 시각이 바뀌는 것을 지켜보다 울린다. 고개의 바위 소리는 굴러가는 동안 계속 되풀이한다.
public class GameAudio : MonoBehaviour
{
    public const float MusicVolume = .5f, SfxVolume = .8f, CrossFade = 1.2f;
    // 지켜보던 시각이 이만큼 넘게 지난 뒤에야 알았으면(검사·치트로 옮겨 놓은 시각) 울리지 않는다.
    public const float Stale = .5f;
    public enum Sfx
    {
        Sword, Bow, DokkaebiDie, Crock, Item,                                   // 팀원 소리
        Charm, Keepsake, Beacon, BeaconWrong, Gate, Purify, BossDie, Transform, Dark,
        Hit, Hurt, Coin, Heal, GhostDie, MobDie, Alarm, Splash, Squad, GameOver, LevelUp, Rare,
        Myeonggeom, Saingeom, Byeokjo, Dagger,                                  // 귀한 무기마다 다른 소리
        // 보스의 공격: 도깨비(몽둥이를 듦·내려침, 돌진, 도약, 내려찍기), 콩쥐 요괴(젓갈, 콩알, 혀, 소금 비, 독째 착지,
        // 마을을 묶는 저주), 좌의정(휩쓸기, 국새를 듦·찍음, 먹물과 떨어지는 먹물, 그림자 웅덩이와 솟는 손, 엽전)
        DkGrowl, DkClub, DkCharge, DkLeap, DkLand,
        KongSpit, KongBeans, KongTongue, KongSalt, KongLand, KongCast,
        FbSweep, FbRaise, FbSlam, FbInk, FbInkDrop, FbHands, FbHand, FbCoins,
    }
    static readonly Dictionary<Sfx, string> Files = new Dictionary<Sfx, string>
    {
        [Sfx.Sword] = "sfx_sword", [Sfx.Bow] = "sfx_bow", [Sfx.DokkaebiDie] = "sfx_dokkaebi_die", [Sfx.Crock] = "sfx_crock",
        [Sfx.Item] = "sfx_item", [Sfx.Charm] = "sfx_charm", [Sfx.Keepsake] = "sfx_keepsake", [Sfx.Beacon] = "sfx_beacon",
        [Sfx.BeaconWrong] = "sfx_beacon_wrong", [Sfx.Gate] = "sfx_gate", [Sfx.Purify] = "sfx_purify", [Sfx.BossDie] = "sfx_boss_die",
        [Sfx.Transform] = "sfx_transform", [Sfx.Dark] = "sfx_dark", [Sfx.Hit] = "sfx_hit", [Sfx.Hurt] = "sfx_hurt",
        [Sfx.Coin] = "sfx_coin", [Sfx.Heal] = "sfx_heal", [Sfx.GhostDie] = "sfx_ghost_die", [Sfx.MobDie] = "sfx_mob_die",
        [Sfx.Alarm] = "sfx_alarm", [Sfx.Splash] = "sfx_splash", [Sfx.Squad] = "sfx_squad", [Sfx.GameOver] = "sfx_gameover",
        [Sfx.LevelUp] = "sfx_levelup", [Sfx.Rare] = "sfx_rare", [Sfx.Myeonggeom] = "sfx_w_myeonggeom",
        [Sfx.Saingeom] = "sfx_w_saingeom", [Sfx.Byeokjo] = "sfx_w_byeokjo", [Sfx.Dagger] = "sfx_w_dagger",
        [Sfx.DkGrowl] = "sfx_dk_growl", [Sfx.DkClub] = "sfx_dk_club", [Sfx.DkCharge] = "sfx_dk_charge", [Sfx.DkLeap] = "sfx_dk_leap",
        [Sfx.DkLand] = "sfx_dk_land", [Sfx.KongSpit] = "sfx_kong_spit", [Sfx.KongBeans] = "sfx_kong_beans",
        [Sfx.KongTongue] = "sfx_kong_tongue", [Sfx.KongSalt] = "sfx_kong_salt", [Sfx.KongLand] = "sfx_kong_land",
        [Sfx.KongCast] = "sfx_kong_cast", [Sfx.FbSweep] = "sfx_fb_sweep", [Sfx.FbRaise] = "sfx_fb_raise", [Sfx.FbSlam] = "sfx_fb_slam",
        [Sfx.FbInk] = "sfx_fb_ink", [Sfx.FbInkDrop] = "sfx_fb_ink_drop", [Sfx.FbHands] = "sfx_fb_hands", [Sfx.FbHand] = "sfx_fb_hand",
        [Sfx.FbCoins] = "sfx_fb_coins",
    };
    // 자주 나는 소리는 조금 작게.
    static float Loudness(Sfx sfx) => sfx == Sfx.Hit ? .55f : sfx == Sfx.Coin ? .45f : sfx == Sfx.Dagger ? .7f
        : sfx == Sfx.KongLand ? .6f : sfx == Sfx.FbInkDrop ? .5f : sfx == Sfx.FbHand ? .7f : sfx == Sfx.DkGrowl ? .8f : 1f;

    public Combat combat;
    static GameAudio instance;
    readonly AudioSource[] music = new AudioSource[2];
    int front;
    AudioSource effects, rolling;
    string wanted;
    int lastItems = -1, lastLevel = -1;
    bool hadCharm, started, special;

    // 시각 하나를 지켜보다가 바뀌면(offset초 뒤에) 효과음을 울린다.
    class Watch
    {
        public Func<float> at; public float offset; public Sfx sfx; public Func<bool> when;
        public float seen = -1, due = -1;
    }
    readonly List<Watch> watches = new List<Watch>();
    void On(Func<float> at, Sfx sfx, float offset = 0, Func<bool> when = null) =>
        watches.Add(new Watch { at = at, sfx = sfx, offset = offset, when = when });

    void Awake()
    {
        instance = this;
        // 장면에 귀(AudioListener)가 없으면 카메라에 단다(예전에 소리를 빼면서 같이 빠졌다).
        if (FindObjectOfType<AudioListener>() == null)
            (Camera.main != null ? Camera.main.gameObject : gameObject).AddComponent<AudioListener>();
        for (int i = 0; i < 2; i++)
        {
            music[i] = gameObject.AddComponent<AudioSource>();
            music[i].loop = true; music[i].playOnAwake = false; music[i].volume = 0;
        }
        effects = gameObject.AddComponent<AudioSource>();
        effects.playOnAwake = false;
        rolling = gameObject.AddComponent<AudioSource>();
        rolling.playOnAwake = false; rolling.loop = true; rolling.volume = SfxVolume;
        rolling.clip = Clip("sfx_boulder");

        float Of(UnityEngine.Object o, Func<float> f) => o != null ? f() : -1;
        On(() => combat.BossDownAt, Sfx.DokkaebiDie);                                     // 도깨비 토벌
        On(() => combat.PurifiedAt, Sfx.Purify);                                          // 도깨비 정화
        On(() => combat.PlayerHurtAt, Sfx.Hurt);
        On(() => combat.PlayerDownAt, Sfx.GameOver, 0, () => combat.PlayerHealth <= 0);
        On(() => combat.CoinsFoundAt, Sfx.Coin);
        On(() => combat.KeepsakeFreedAt, Sfx.Keepsake);                                   // 물귀신 열 마리째의 은비녀
        On(() => Of(combat.Fort, () => combat.Fort.LitAt), Sfx.Beacon);
        On(() => Of(combat.Fort, () => combat.Fort.WrongAt), Sfx.BeaconWrong);
        On(() => Of(combat.Fort, () => combat.Fort.OpenedAt), Sfx.Gate, .5f);
        On(() => Of(combat.Pass, () => combat.Pass.ShrineAt), Sfx.Heal);                  // 서낭당
        On(() => Of(combat.Ferry, () => combat.Ferry.SplashAt), Sfx.Splash);
        On(() => Of(combat.Village, () => combat.Village.AlarmAt), Sfx.Alarm);            // 도깨비가 마을에 듦
        On(() => Of(combat.Mobs, () => combat.Mobs.ShoutAt), Sfx.Squad);                  // 관군이 몰려옴
        On(() => Of(combat.KongBoss, () => combat.KongBoss.ChangedAt), Sfx.Transform);
        On(() => Of(combat.KongBoss, () => combat.KongBoss.PurifiedAt), Sfx.Purify);
        On(() => Of(combat.KongBoss, () => combat.KongBoss.DownAt), Sfx.BossDie, 0, () => combat.KongBoss.Now == KongBoss.Phase.Slain);
        On(() => Of(combat.FinalBoss, () => combat.FinalBoss.ChangedAt), Sfx.Transform, FinalBoss.FlashAt - KongBoss.FlashAt);
        On(() => Of(combat.FinalBoss, () => combat.FinalBoss.DarkAt), Sfx.Dark);
        On(() => Of(combat.FinalBoss, () => combat.FinalBoss.DownAt), Sfx.BossDie);
    }

    void OnDestroy() { if (instance == this) instance = null; }

    static AudioClip Clip(string name) => Resources.Load<AudioClip>("JinaSound/" + name);

    // 효과음 하나를 울린다.
    // 휘두르는 소리처럼 두 벌(이름_2)이 있으면 번갈아 써서 연격이 기계처럼 똑같이 들리지 않게 한다.
    static readonly HashSet<Sfx> TwoTakes = new HashSet<Sfx> { Sfx.Myeonggeom, Sfx.Saingeom, Sfx.Dagger };
    static int take;

    public static void Play(Sfx sfx, float volume = 1)
    {
        if (instance == null) return;
        var clip = Clip(Files[sfx] + (TwoTakes.Contains(sfx) && take++ % 2 == 1 ? "_2" : ""));
        if (clip != null) instance.effects.PlayOneShot(clip, SfxVolume * volume * Loudness(sfx));
        if (sfx == Sfx.Keepsake || sfx == Sfx.Charm || sfx == Sfx.Rare) instance.special = true;
    }

    // 세상의 `at`에서 나는 소리: 선비가 있는 맵(Maps.Current)에서만 들린다(강 건너 마을에서 콩쥐가 날뛰는 소리 같은 것은 안 들림).
    public static void PlayAt(Sfx sfx, Vector3 at, float volume = 1)
    {
        if (instance == null) return;
        var maps = instance.combat != null ? instance.combat.Maps : null;
        if (maps != null && (maps.Travelling || Maps.Index(at.x) != maps.Current)) return;
        Play(sfx, volume);
    }

    // 무기를 휘두른다: 귀한 칼은 저마다 소리가 다르다(환도는 팀원의 칼 소리). 도깨비 방망이는 도깨비가 내려치던 소리.
    public static void Swing(string weapon) =>
        Play(weapon == "myeonggeom" ? Sfx.Myeonggeom : weapon == "saingeom" ? Sfx.Saingeom : weapon == "dagger" ? Sfx.Dagger
            : weapon == "dokkaebi_club" || weapon == "blood_club" ? Sfx.DkClub : Sfx.Sword);

    // 시위를 놓는다: 벽조궁은 번개가 튄다.
    public static void Shot(string bow) { Play(Sfx.Bow); if (bow == "byeokjogung") Play(Sfx.Byeokjo); }

    // 지금 틀어야 할 배경 음악(없으면 null).
    public string Music()
    {
        if (combat == null) return null;
        if (combat.Intro != null && combat.Intro.IsPlaying) return "bgm_intro";
        if (combat.Ending != null && combat.Ending.Active) return "bgm_ending";
        if (combat.PlayerHealth <= 0) return null;
        // 콩밭 하룻밤 연출: 밤이 된 순간부터 틀고, 새벽(수탉 컷)부터는 끈다.
        if (combat.Stay != null && combat.Stay.Now == KongNightStay.Phase.Night) return "bgm_kong_night";
        if (combat.BossInVillage) return "bgm_dokkaebi_mad";
        if (combat.Maps == null) return null;
        int map = combat.Maps.Current;
        // 보스가 변하는 동안은 음악을 걷어 내고, 싸움이 붙으면 싸움 음악.
        var kong = combat.KongBoss;
        if (kong != null && map >= 5 && map <= 7)
        {
            if (kong.Now == KongBoss.Phase.Changing) return null;
            if (kong.Now == KongBoss.Phase.Fighting) return "bgm_kong_fight";
        }
        var final = combat.FinalBoss;
        if (final != null && map == 13)
        {
            if (final.Now == FinalBoss.Phase.Changing || final.Now == FinalBoss.Phase.Slain) return null;
            if (final.Now == FinalBoss.Phase.Fighting) return "bgm_final_fight";
        }
        switch (map)
        {
            case 0: case 1: return "bgm_field";               // 옛 비석 터, 망령의 들판
            case 2: return "bgm_village";                     // 주막 마을
            case 3:                                           // 도깨비 숲: 정화 전에는 광기, 정화 뒤에는 고요
                if (combat.BossPurified) return "bgm_dokkaebi_calm";
                return combat.EnemyHealth > 0 ? "bgm_dokkaebi_mad" : null;
            case 4: return "bgm_river";                       // 나루터
            case 5:                                           // 콩밭 마을: 콩쥐를 정화한 뒤에는 활기찬 첫 마을 음악
                return KongPurified ? "bgm_village" : "bgm_kong_village";
            case 6: return "bgm_pass";                        // 고개와 서낭당
            case 7: return KongResolved ? "bgm_pass" : "bgm_hill";   // 뒷산: 콩쥐 일이 끝나면 고개처럼 트인다
            case 8: return "bgm_fort";                        // 봉화 산성
            case 9: return "bgm_station";                     // 버려진 역참
            case 10: return "bgm_grave";                      // 북망산 무덤길
            case 11: return "bgm_city";                       // 한양 거리
            case 12: return "bgm_hall";                       // 과거장 앞
            case 13: return "bgm_manor";                      // 좌의정 댁
            default: return null;
        }
    }

    bool KongPurified => combat.Quest != null && combat.Quest.progress.KongOutcome == EncounterOutcome.Purified;
    bool KongResolved => combat.Quest != null && combat.Quest.progress.KongOutcome != EncounterOutcome.Unresolved;

    void Update()
    {
        if (combat == null) return;
        Listen();
        // 고개(6번 맵)에서 바위가 하나라도 굴러가는 동안 바위 소리를 되풀이한다.
        bool rocks = combat.Pass != null && combat.Pass.rocks.Count > 0 && combat.Maps != null && combat.Maps.Current == 6 && !combat.Paused && combat.PlayerHealth > 0;
        if (rocks && !rolling.isPlaying && rolling.clip != null) rolling.Play();
        else if (!rocks && rolling.isPlaying) rolling.Stop();
        PlayMusic();
    }

    // 기록된 시각과 상태가 바뀐 것을 듣고 효과음을 울린다. 처음 한 번은 지금 상태를 외우기만 한다.
    void Listen()
    {
        bool charm = combat.Quest != null && combat.Quest.progress.HasCharm;
        int level = combat.Level, items = ItemCount();
        foreach (var w in watches)
        {
            float v = w.at();
            if (v != w.seen) { w.seen = v; w.due = started && v >= 0 ? v + w.offset : -1; }
            if (w.due < 0 || Time.time < w.due) continue;
            if (Time.time - w.due < Stale && (w.when == null || w.when())) Play(w.sfx);
            w.due = -1;
        }
        if (started)
        {
            if (charm && !hadCharm) Play(Sfx.Charm);                       // 비석에서 정화 부적이 나옴
            if (level > lastLevel && lastLevel > 0) Play(Sfx.LevelUp);
            // 보따리에 물건이 늘었을 때(구입, 도깨비불의 선물 등). 은비녀·부적·무기는 제 소리가 따로 난다.
            if (lastItems >= 0 && items > lastItems && !special) Play(Sfx.Item);
        }
        hadCharm = charm; lastLevel = level; lastItems = items; started = true; special = false;
    }

    void PlayMusic()
    {
        string next = Music();
        if (next != wanted)
        {
            wanted = next;
            front = 1 - front;
            var clip = next == null ? null : Clip(next);
            music[front].clip = clip;
            music[front].volume = 0;
            if (clip != null) music[front].Play(); else music[front].Stop();
        }
        float step = Time.unscaledDeltaTime / CrossFade * MusicVolume;
        var on = music[front]; var off = music[1 - front];
        on.volume = Mathf.MoveTowards(on.volume, on.clip != null ? MusicVolume : 0, step);
        off.volume = Mathf.MoveTowards(off.volume, 0, step);
        if (off.volume <= 0 && off.isPlaying) off.Stop();
    }

    static int ItemCount()
    {
        var bag = JinaItems.Inventory;
        if (bag == null) return -1;
        int n = 0;
        foreach (var slot in bag.Slots) n += slot.Quantity;
        return n;
    }
}
