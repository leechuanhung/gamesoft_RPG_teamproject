using System;
using UnityEngine;

public static class TavernChecks
{
    public static void Run()
    {
        var wallet = new TavernWallet();
        int heal;
        Require(!wallet.Buy(0, 5, out heal) && wallet.Coins == 30, "No charge at full health");
        Require(wallet.Buy(0, 4, out heal) && heal == 1 && wallet.Coins == 25, "Clamp healing and deduct price");
        Require(wallet.Buy(1, 1, out heal) && heal == 4 && wallet.Coins == 15, "Soup recovery");
        wallet.Buy(1, 1, out heal);
        Require(!wallet.Buy(1, 1, out heal) && wallet.Coins == 5, "Insufficient funds");
        Require(!wallet.Buy(0, 0, out heal) && wallet.Coins == 5, "Cannot revive by buying");
        Require(!wallet.Buy(-1, 1, out heal) && wallet.Coins == 5, "Reject invalid menu");
        wallet.Reset();
        Require(wallet.Coins == 30, "Reset money");
        foreach (var name in new[] { "rat-brown", "wolf-black", "slime-blue", "ooze-green" })
            Require(MonsterVisual.Load(name) != null, "Monster sprite loads: " + name);
        MoralityChecks.Run();
        Debug.Log("TAVERN_CHECKS_PASSED: purchases, health cap, funds, reset, monster art");
    }
    static void Require(bool value, string text) { if (!value) throw new Exception(text); }
}
