public enum EncounterOutcome { Unresolved, Slain, Purified }

// Progress is kept separate from the display so restart and branching stay consistent.
public class QuestProgress
{
    public bool HeardClue { get; private set; }
    public bool HasCharm { get; private set; }
    public int PuzzleStep { get; private set; }
    public EncounterOutcome Outcome { get; private set; }
    public int Honor { get; private set; }
    public bool ElderDead { get; private set; }
    public bool SacrificeElder()
    {
        if (ElderDead || Outcome != EncounterOutcome.Unresolved) return false;
        ElderDead = true;
        Honor -= 40;
        return true;
    }
    private readonly int[] sequence = { 2, 1, 3 };

    public void HearClue() { HeardClue = true; }
    public bool ChooseSymbol(int symbol)
    {
        if (!HeardClue || HasCharm || Outcome != EncounterOutcome.Unresolved) return false;
        if (symbol != sequence[PuzzleStep]) { PuzzleStep = 0; return false; }
        PuzzleStep++;
        if (PuzzleStep == sequence.Length) HasCharm = true;
        return true;
    }
    public bool Resolve(bool purify)
    {
        if (Outcome != EncounterOutcome.Unresolved || (purify && !HasCharm)) return false;
        Outcome = purify ? EncounterOutcome.Purified : EncounterOutcome.Slain;
        Honor += purify ? 30 : 10;
        if (purify) HasCharm = false;
        return true;
    }
    public void Reset()
    {
        HeardClue = HasCharm = false;
        PuzzleStep = 0;
        Outcome = EncounterOutcome.Unresolved;
        Honor = 0;
        ElderDead = false;
    }
}
