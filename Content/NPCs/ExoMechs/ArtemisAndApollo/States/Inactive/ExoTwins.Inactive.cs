﻿using CalamityMod.NPCs.ExoMechs.Apollo;
using CalamityMod.NPCs.ExoMechs.Artemis;
using Luminance.Common.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using WoTM.Content.NPCs.ExoMechs.FightManagers;

namespace WoTM.Content.NPCs.ExoMechs.ArtemisAndApollo;

public static partial class ExoTwinsStates
{
    /// <summary>
    /// AI update loop method for the Inactive state.
    /// </summary>
    /// <param name="npc">The Exo Twin's NPC instance.</param>
    /// <param name="twinAttributes">The Exo Twin's designated generic attributes.</param>
    public static void DoBehavior_Inactive(NPC npc, IExoTwin twinAttributes)
    {
        bool isApollo = npc.type == ExoMechNPCIDs.ApolloID;
        Vector2 hoverDestination = Target.Center + new Vector2(isApollo.ToDirectionInt() * 900f, -1050f);

        npc.dontTakeDamage = true;
        npc.SmoothFlyNear(hoverDestination, 0.09f, 0.9f);
        npc.rotation = npc.rotation.AngleLerp(npc.AngleTo(Target.Center), 0.15f);
        npc.Opacity = LumUtils.Saturate(npc.Opacity - 0.08f);

        twinAttributes.Animation = ExoTwinAnimation.Idle;
        twinAttributes.Frame = twinAttributes.Animation.CalculateFrame(AITimer / 40f % 1f, twinAttributes.InPhase2);

        // This is necessary to ensure that the map icon goes away.
        if (isApollo)
            npc.As<Apollo>().SecondaryAIState = (int)Apollo.SecondaryPhase.PassiveAndImmune;
        else
            npc.As<Artemis>().SecondaryAIState = (int)Artemis.SecondaryPhase.PassiveAndImmune;
    }
}
