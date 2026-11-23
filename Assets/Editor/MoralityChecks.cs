using System;
using UnityEditor;
using UnityEngine;

public static class MoralityChecks
{
    public static void Run()
    {
        foreach (bool sacrifice in new[] { false, true })
            foreach (bool purify in new[] { false, true })
            {
                var q = new QuestProgress();
                Require(q.Honor == 0 && !q.ElderDead, "Neutral start");
                q.HearClue(); q.ChooseSymbol(2); q.ChooseSymbol(1); q.ChooseSymbol(3);
                if (sacrifice)
                {
                    Require(q.SacrificeElder(), "First sacrifice");
                    Require(!q.SacrificeElder() && q.Honor == -40, "No duplicate sacrifice");
                }
                Require(q.Resolve(purify), "Resolve route");
                Require(q.Honor == (purify ? 30 : 10) - (sacrifice ? 40 : 0), "Correct honor for route");
                Require(!q.Resolve(purify), "No repeat reward");
                q.Reset();
                Require(q.Honor == 0 && !q.ElderDead && !q.HasCharm, "Full restart");
            }
        var camp = new GameObject("Camp verification").AddComponent<Camp>();
        camp.Initialize(null, AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Square.png"));
        var gate = camp.GetComponentInChildren<BoxCollider2D>();
        Require(camp.Remaining == 3 && gate.gameObject.activeSelf, "Gate starts closed");
        for (int i = 0; i < 2; i++) for (int hp = 0; hp < Camp.GhostHealth; hp++) camp.Hit(camp.mobs[i]);
        Require(camp.Remaining == 1 && gate.gameObject.activeSelf, "Last monster still blocks passage");
        for (int hp = 0; hp < Camp.GhostHealth; hp++) camp.Hit(camp.mobs[2]);
        Require(camp.Remaining == 0 && gate.gameObject.activeSelf, "One round of ghosts does not open the gate");
        while (camp.Kills < Camp.SealKills) { var m = camp.mobs[0]; m.hp = Camp.GhostHealth; for (int hp = 0; hp < Camp.GhostHealth; hp++) camp.Hit(m); }
        Require(!gate.gameObject.activeSelf && camp.Cleared && camp.Kills == Camp.SealKills, "Enough kills open the gate");
        camp.ResetCamp();
        Require(camp.Remaining == 3 && gate.gameObject.activeSelf, "Restart restores gate and monsters");
        camp.Hit(camp.mobs[0], Combat.SwordDamage);
        Require(camp.mobs[0].hp == Camp.GhostHealth - Combat.SwordDamage, "A sword hit deals its damage");
        for (int i = 1; i * Combat.SwordDamage < Camp.GhostHealth; i++) camp.Hit(camp.mobs[0], Combat.SwordDamage);
        Require(camp.mobs[0].hp == 0 && camp.Remaining == 2, "Enough sword hits fell a monster");
        camp.ResetCamp();
        UnityEngine.Object.DestroyImmediate(camp.gameObject);
        WorldChecks.Run();
        Debug.Log("MORALITY_CHECKS_PASSED: four honor outcomes, duplicate prevention, reset and gate progression");
    }
    static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
