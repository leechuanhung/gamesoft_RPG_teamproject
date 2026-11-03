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
        ShopChecks();
        foreach (var name in new[] { "rat-brown", "wolf-black", "slime-blue", "ooze-green" })
            Require(MonsterVisual.Load(name) != null, "Monster sprite loads: " + name);
        MoralityChecks.Run();
        Debug.Log("TAVERN_CHECKS_PASSED: purchases, health cap, funds, reset, monster art");
    }
    static void ShopChecks()
    {
        var neutral = new QuestProgress();
        var slain = new QuestProgress(); slain.Resolve(false);
        var purified = new QuestProgress(); purified.HearClue(); purified.ChooseSymbol(2); purified.ChooseSymbol(1); purified.ChooseSymbol(3); purified.Resolve(true);
        var mourning = new QuestProgress(); mourning.SacrificeElder();
        var mournSlain = new QuestProgress(); mournSlain.SacrificeElder(); mournSlain.Resolve(false);
        Require(ShopRules.Discount(neutral) == 0 && ShopRules.Discount(slain) == 10 && ShopRules.Discount(purified) == 20, "Discount by honor");
        Require(ShopRules.Discount(mourning) == 0 && ShopRules.Discount(mournSlain) == 0, "No discount after the elder dies");
        Require(ShopRules.Price(5, neutral) == 5 && ShopRules.Price(10, neutral) == 10 && ShopRules.Price(9, neutral) == 9, "Full prices");
        Require(ShopRules.Price(5, slain) == 4 && ShopRules.Price(10, slain) == 9 && ShopRules.Price(9, slain) == 8, "10% prices");
        Require(ShopRules.Price(5, purified) == 4 && ShopRules.Price(10, purified) == 8 && ShopRules.Price(15, purified) == 12 && ShopRules.Price(9, purified) == 7, "20% prices");
        Require(ShopRules.Price(1, purified) == 1, "Single arrow never free");
        Require(!ShopRules.MenuOpen(2, neutral) && !ShopRules.MenuOpen(2, slain) && ShopRules.MenuOpen(2, purified), "Samgyetang only on purify route");
        Require(ShopRules.MenuOpen(1, neutral) && !ShopRules.MenuOpen(1, mourning) && ShopRules.MenuOpen(0, mourning), "Gukbap banned while mourning");
        int heal, bought;
        var wallet = new TavernWallet();
        Require(!wallet.Buy(1, 1, out heal, mourning) && wallet.Coins == 30, "Banned menu cannot be bought");
        Require(wallet.Buy(2, 1, out heal, purified) && heal == 4 && wallet.Coins == 18, "Discounted samgyetang");
        wallet.Reset();
        Require(!wallet.BuyArrows(false, 20, out bought) && wallet.Coins == 30, "Full quiver");
        Require(!wallet.BuyArrows(true, 11, out bought) && wallet.Coins == 30, "Bundle must fit the quiver");
        Require(wallet.BuyArrows(true, 10, out bought) && bought == 10 && wallet.Coins == 21, "Bundle costs 9");
        Require(wallet.BuyArrows(false, 0, out bought) && bought == 1 && wallet.Coins == 20, "Single costs 1");
        Require(!wallet.BuyArrows(true, 0, out bought, mourning) && wallet.BuyArrows(false, 0, out bought, mourning), "Singles only while mourning");
        Require(wallet.BuyArrows(true, 0, out bought, slain) && wallet.Coins == 11, "Discounted bundle");
    }
    static void Require(bool value, string text) { if (!value) throw new Exception(text); }
}
