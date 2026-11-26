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
    // 한양 좌의정 댁의 어둑시니 좌의정: 쓰러뜨리면 +10.
    public EncounterOutcome FinalOutcome { get; private set; }
    public bool ResolveFinal()
    {
        if (FinalOutcome != EncounterOutcome.Unresolved) return false;
        FinalOutcome = EncounterOutcome.Slain;
        Honor += 10;
        return true;
    }
    // 한양 가는 길의 체크포인트로 되돌린다.
    public void RewindRoad(int honor) { Honor = honor; FinalOutcome = EncounterOutcome.Unresolved; }
    // 선악을 `delta`만큼 바꾼다(퀘스트 보상).
    public void AddHonor(int delta) { Honor += delta; }
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
    // 기존 부적 퍼즐 호환용: 표식 하나를 선택한다. 순서가 틀리면 처음부터 다시 시작한다.
    public bool ChooseSymbol(int symbol)
    {
        if (!HeardClue || HasCharm || Outcome != EncounterOutcome.Unresolved) return false;
        if (symbol != sequence[PuzzleStep]) { PuzzleStep = 0; return false; }
        PuzzleStep++;
        if (PuzzleStep == sequence.Length) HasCharm = true;
        return true;
    }
    // 옛 비석의 격자 퍼즐을 풀면 정화 부적을 얻는다.
    public bool CompleteStelePuzzle()
    {
        if (!HeardClue || HasCharm || Outcome != EncounterOutcome.Unresolved) return false;
        HasCharm = true;
        PuzzleStep = sequence.Length;
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
    // 저장 파일에서 진행을 그대로 되살린다(PlayerSave).
    public void Restore(bool heardClue, bool hasCharm, int puzzleStep, EncounterOutcome outcome, int honor, bool elderDead,
        EncounterOutcome kongOutcome, int villagersLost, EncounterOutcome finalOutcome)
    {
        HeardClue = heardClue; HasCharm = hasCharm; PuzzleStep = System.Math.Max(0, System.Math.Min(puzzleStep, sequence.Length));
        Outcome = outcome; Honor = honor; ElderDead = elderDead;
        KongOutcome = kongOutcome; VillagersLost = villagersLost; FinalOutcome = finalOutcome;
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
        FinalOutcome = EncounterOutcome.Unresolved;
    }
}
