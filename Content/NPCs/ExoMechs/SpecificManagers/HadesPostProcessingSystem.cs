﻿using System;
using CalamityMod.NPCs;
using CalamityMod.NPCs.ExoMechs.Thanatos;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WoTM.Common.Utilities;
using WoTM.Content.NPCs.ExoMechs.Hades;

namespace WoTM.Content.NPCs.ExoMechs.SpecificManagers;

public class HadesPostProcessingSystem : ModSystem
{
    /// <summary>
    /// The render target that holds all render information of Hades, for the purposes of allowing all of him to render with post-processing.
    /// </summary>
    public static ManagedRenderTarget HadesTarget
    {
        get;
        private set;
    }

    /// <summary>
    /// An optional post-processing action to perform on all of Hades.
    /// </summary>
    public static Action? PostProcessingAction
    {
        get;
        set;
    }

    /// <summary>
    /// The scale correction factor for rendering with this system.
    /// </summary>
    public static Vector2 ScaleCorrection => HadesTarget.Size() / new Vector2(Main.screenWidth, Main.screenHeight);

    public override void OnModLoad()
    {
        if (Main.netMode == NetmodeID.Server)
            return;

        HadesTarget = new(true, (width, height) =>
        {
            return new RenderTarget2D(Main.instance.GraphicsDevice, width / 2, height / 2);
        });
        RenderTargetManager.RenderTargetUpdateLoopEvent += RenderHadesToTarget;

        On_Main.DrawNPCs += DrawHadesTarget;
    }

    private static void RenderHadesToTarget()
    {
        if (CalamityGlobalNPC.draedonExoMechWorm == -1)
            return;

        GraphicsDevice graphicsDevice = Main.instance.GraphicsDevice;
        graphicsDevice.SetRenderTarget(HadesTarget);
        graphicsDevice.Clear(Color.Transparent);

        Main.spriteBatch.Begin();

        for (int i = Main.maxNPCs - 1; i >= 0; i--)
        {
            NPC npc = Main.npc[i];
            if (npc.realLife == CalamityGlobalNPC.draedonExoMechWorm || npc.whoAmI == CalamityGlobalNPC.draedonExoMechWorm)
            {
                npc.scale *= ScaleCorrection.X;

                Color lightColor = Lighting.GetColor(npc.Center.ToTileCoordinates());
                if (npc.TryGetBehavior(out HadesHeadBehavior head))
                    head.DrawSelf(Main.screenPosition, lightColor);
                else if (npc.TryGetBehavior(out HadesBodyBehavior body))
                    body.DrawSelf(Main.screenPosition, lightColor);

                npc.scale /= ScaleCorrection.X;
            }
        }

        Main.spriteBatch.End();
    }

    private static void DrawHadesTarget(On_Main.orig_DrawNPCs orig, Main self, bool behindTiles)
    {
        orig(self, behindTiles);

        bool hadesExists = NPC.AnyNPCs(ModContent.NPCType<ThanatosHead>());
        if (hadesExists && !behindTiles)
        {
            Main.spriteBatch.PrepareForShaders();

            PostProcessingAction?.Invoke();
            Main.spriteBatch.Draw(HadesTarget, Main.screenLastPosition - Main.screenPosition, null, Color.White, 0f, Vector2.Zero, 2f, 0, 0f);

            Main.spriteBatch.ResetToDefault();
        }
    }
}
