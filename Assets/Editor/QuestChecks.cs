using System;
using UnityEngine;

public static class QuestChecks
{
    public static void Run()
    {
        var q = new QuestProgress();
        Require(!q.Resolve(true), "Cannot purify without charm");
        Require(!q.ChooseSymbol(2) && q.PuzzleStep == 0, "Clue gates puzzle");
        q.HearClue();
        Require(q.ChooseSymbol(2), "First symbol");
        Require(!q.ChooseSymbol(3) && q.PuzzleStep == 0, "Mistake resets puzzle");
        q.ChooseSymbol(2); q.ChooseSymbol(1); q.ChooseSymbol(3);
        Require(q.HasCharm && !q.ChooseSymbol(1), "Charm awarded once");
        Require(q.Resolve(true) && !q.HasCharm && q.Outcome == EncounterOutcome.Purified, "Purify consumes charm");
        Require(!q.Resolve(false), "Outcome cannot be overwritten");
        q.Reset();
        Require(!q.HeardClue && !q.HasCharm && q.PuzzleStep == 0 && q.Outcome == EncounterOutcome.Unresolved, "Full reset");
        Require(q.Resolve(false) && q.Outcome == EncounterOutcome.Slain, "Combat route needs no clue");
        Require(!q.Resolve(true), "Cannot purify dead enemy");
        Debug.Log("QUEST_CHECKS_PASSED (10 checks)");
    }
    static void Require(bool result, string label)
    {
        if (!result) throw new Exception("Quest check failed: " + label);
    }
}
