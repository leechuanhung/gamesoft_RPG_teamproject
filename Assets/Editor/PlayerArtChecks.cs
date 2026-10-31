using System;
using UnityEngine;

public static class PlayerArtChecks
{
    public static void Run()
    {
        var sprite = SeonbiVisual.LoadSprite();
        if (sprite == null || sprite.rect.width <= 0 || sprite.rect.height <= 0)
            throw new Exception("Player sprite failed to load");
        if (sprite.texture.filterMode != FilterMode.Point)
            throw new Exception("Player sprite must use point filtering");
        if (sprite.rect.height <= sprite.rect.width)
            throw new Exception("Expected a portrait character crop");
        TavernChecks.Run();
        Debug.Log("PLAYER_ART_CHECKS_PASSED: sprite load, alpha crop, point filter and gameplay regression");
    }
}
