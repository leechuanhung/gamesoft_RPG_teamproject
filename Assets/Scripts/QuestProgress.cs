public enum EncounterOutcome { Unresolved, Slain, Purified }

// 진행 상태를 화면과 분리해서 재시작과 분기가 일관되게 한다.
public class QuestProgress
{
    public bool HeardClue { get; private set; }
    public bool HasCharm { get; private set; }
    public int PuzzleStep { get; private set; }
    public EncounterOutcome Outcome { get; private set; }
    public int Honor { get; private set; }
    public bool ElderDead { get; private set; }
    // 뒷산의 콩쥐 요괴: 토벌 +10, 은비녀로 정화 +50, 요괴가 죽인 마을 사람 하나당 −10.
    public EncounterOutcome KongOutcome { get; private set; }
    public int VillagersLost { get; private set; }
    // 콩쥐 요괴를 해결한다(`purify`: 정화). 처음이면 true.
    public bool ResolveKong(bool purify)
    {
        if (KongOutcome != EncounterOutcome.Unresolved) return false;
        KongOutcome = purify ? EncounterOutcome.Purified : EncounterOutcome.Slain;
        Honor += purify ? 50 : 10;
        return true;
    }
    public void LoseVillager() { VillagersLost++; Honor -= 10; }
    // 뒷산에서 콩쥐를 만났을 때로 되돌린다(체크포인트).
    public void RewindKong(int honor, int lost) { Honor = honor; VillagersLost = lost; KongOutcome = EncounterOutcome.Unresolved; }
    // 노인이 죽었음을 기록한다(−40). 처음이면 true.
    public bool SacrificeElder()
    {
        if (ElderDead || Outcome != EncounterOutcome.Unresolved) return false;
        ElderDead = true;
        Honor -= 40;
        return true;
    }
    private readonly int[] sequence = { 2, 1, 3 };

    public void HearClue() { HeardClue = true; }
    // 부적 퍼즐에서 표식 하나를 고른다. 순서가 맞으면 true(틀리면 처음부터).
    public bool ChooseSymbol(int symbol)
    {
        if (!HeardClue || HasCharm || Outcome != EncounterOutcome.Unresolved) return false;
        if (symbol != sequence[PuzzleStep]) { PuzzleStep = 0; return false; }
        PuzzleStep++;
        if (PuzzleStep == sequence.Length) HasCharm = true;
        return true;
    }
    // 도깨비를 해결한다(`purify`: 정화). 처음이면 true.
    public bool Resolve(bool purify)
    {
        if (Outcome != EncounterOutcome.Unresolved || (purify && !HasCharm)) return false;
        Outcome = purify ? EncounterOutcome.Purified : EncounterOutcome.Slain;
        Honor += purify ? 30 : 10;
        if (purify) HasCharm = false;
        return true;
    }
    // 재시작: 진행 상태를 모두 처음으로.
    public void Reset()
    {
        HeardClue = HasCharm = false;
        PuzzleStep = 0;
        Outcome = EncounterOutcome.Unresolved;
        Honor = 0;
        ElderDead = false;
        KongOutcome = EncounterOutcome.Unresolved;
        VillagersLost = 0;
    }
}
