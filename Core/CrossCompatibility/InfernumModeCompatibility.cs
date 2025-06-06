﻿using InfernumMode.Core.GlobalInstances.Systems;
using Terraria.ModLoader;

namespace WoTM.Core.CrossCompatibility;

public class InfernumModeCompatibility : ModSystem
{
    /// <summary>
    /// The Infernum mod.
    /// </summary>
    public static Mod? Infernum
    {
        get;
        private set;
    }

    /// <summary>
    /// Whether Infernum Mode is active or not.
    /// </summary>
    public static bool InfernumModeIsActive
    {
        get => (bool)(Infernum?.Call("GetInfernumActive") ?? false);
        set
        {
            if (Infernum is not null)
                SetInfernumActiveBecauseTheModCallIsntWorking(value);
        }
    }

    public override void PostSetupContent()
    {
        if (ModLoader.TryGetMod("InfernumMode", out Mod inf))
            Infernum = inf;
    }

    [JITWhenModsEnabled("InfernumMode")]
    private static void SetInfernumActiveBecauseTheModCallIsntWorking(bool value)
    {
        WorldSaveSystem.InfernumModeEnabled = value;
    }
}
