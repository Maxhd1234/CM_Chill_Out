using System;
using HarmonyLib;
using UnityEngine;
using Verse;

public class ChillOutMod : Mod
{
    private static ChillOutMod _instance;

    public static ChillOutMod Instance => _instance;

    private readonly ChillOut settings;

    public ChillOut Settings => settings;

    public ChillOutMod(ModContentPack content) : base(content)
    {
        settings = GetSettings<ChillOut>();
        var harmony = new Harmony("KT.ChillOut");
        harmony.PatchAll();
        _instance = this;
    }

    public override void DoSettingsWindowContents(Rect inRect)
    {
        var listingStandard = new Listing_Standard();
        listingStandard.Begin(inRect);

        float fckICE2 = settings.fckICE;
        fckICE2 = listingStandard.SliderLabeled(
            "Joy threshold: " + fckICE2.ToString("P0"),
            fckICE2,
            0f,
            1f
        );

        settings.fckICE = (float)Math.Round(fckICE2, 2);
        
        if (!Mathf.Approximately(fckICE2, settings.fckICE))
        {
            settings.fckICE = fckICE2;
        }
        
        listingStandard.End();
    }

    public override string SettingsCategory()
    {
        return "Chill the Fork Out";
    }
}


