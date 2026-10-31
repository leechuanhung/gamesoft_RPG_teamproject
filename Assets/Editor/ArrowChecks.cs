using System;
using UnityEngine;

public static class ArrowChecks
{
    public static void Run()
    {
        var target = new Bounds(new Vector3(5, 0, 0), new Vector3(1, 2, 1));
        Require(!float.IsInfinity(PrototypeCombat.ArrowHitDistance(target, Vector2.zero, 1, 10)), "Fast arrow must hit");
        Require(float.IsInfinity(PrototypeCombat.ArrowHitDistance(target, Vector2.zero, -1, 10)), "Wrong direction must miss");
        Require(float.IsInfinity(PrototypeCombat.ArrowHitDistance(target, Vector2.zero, 1, 4)), "Out of range must miss");
        Require(float.IsInfinity(PrototypeCombat.ArrowHitDistance(target, new Vector2(0, 3), 1, 10)), "Arrow above target must miss");
        Require(PrototypeCombat.ArrowHitDistance(target, new Vector2(5, 0), 1, 1) == 0, "Overlapping origin must hit");
        Require(!float.IsInfinity(PrototypeCombat.ArrowHitDistance(target, new Vector2(10, 0), -1, 10)), "Leftward arrow must hit");
        QuestChecks.Run();
        KoreanUIChecks.Run();
        Debug.Log("ARROW_CHECKS_PASSED (6 checks)");
    }
    static void Require(bool result, string label)
    {
        if (!result) throw new Exception(label);
    }
}
