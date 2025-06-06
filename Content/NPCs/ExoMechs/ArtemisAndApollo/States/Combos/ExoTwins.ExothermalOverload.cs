﻿using System;
using CalamityMod.NPCs.ExoMechs.Artemis;
using Luminance.Assets;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using WoTM.Common.Utilities;
using WoTM.Content.NPCs.ExoMechs.FightManagers;
using WoTM.Content.NPCs.ExoMechs.Projectiles;
using WoTM.Content.NPCs.ExoMechs.SpecificManagers;
using WoTM.Content.Particles;

namespace WoTM.Content.NPCs.ExoMechs.ArtemisAndApollo;

public static partial class ExoTwinsStates
{
    /// <summary>
    /// The amount of damage the Exothermal Disintegration Ray from Artemis does.
    /// </summary>
    public static int ExothermalDisintegrationRayDamage => Main.expertMode ? 515 : 350;

    /// <summary>
    /// How many laser cycles should be performed during the Exothermal Overload attack before a new attack is selected.
    /// </summary>
    public static int ExothermalOverload_CycleCount => 2;

    /// <summary>
    /// AI update loop method for the ExothermalOverload attack.
    /// </summary>
    /// <param name="npc">The Twins' NPC instance.</param>
    /// <param name="twinAttributes">The Twins' designated generic attributes.</param>
    public static void DoBehavior_ExothermalOverload(NPC npc, IExoTwin twinAttributes)
    {
        if (npc.type == ExoMechNPCIDs.ArtemisID)
            DoBehavior_ExothermalOverload_ArtemisDeathrays(npc, twinAttributes);
        else
            DoBehavior_ExothermalOverload_ApolloHaloPlasmaDashes(npc, twinAttributes);
    }

    /// <summary>
    /// AI update loop method for the ExothermalOverload attack for Artemis specifically.
    /// </summary>
    /// <param name="npc">Artemis' NPC instance.</param>
    /// <param name="artemisAttributes">Artemis' designated generic attributes.</param>
    public static void DoBehavior_ExothermalOverload_ArtemisDeathrays(NPC npc, IExoTwin artemisAttributes)
    {
        int hoverRedirectTime = 45;
        int slowDownTime = 15;
        int chargeUpTime = 60;
        int energyGleamTime = 45;
        int laserShootTime = ExothermalDisintegrationRay.Lifetime;
        int wrapperAITimer = AITimer % (hoverRedirectTime + slowDownTime + chargeUpTime + energyGleamTime + laserShootTime);
        ref float hoverOffsetAngle = ref SharedState.Values[2];
        ref float moveDirection = ref SharedState.Values[3];

        if (LumUtils.WrapAngle360(hoverOffsetAngle) % MathHelper.PiOver4 >= 0.01f)
            hoverOffsetAngle = MathHelper.PiOver4;

        if (wrapperAITimer == 1)
        {
            hoverOffsetAngle = LumUtils.WrapAngle360(hoverOffsetAngle + Main.rand.NextFromList(-1f, 1f) * MathHelper.PiOver2);
            npc.netUpdate = true;

            int totalPerformedCycles = AITimer / (hoverRedirectTime + slowDownTime + chargeUpTime + energyGleamTime + laserShootTime);
            if (totalPerformedCycles >= ExothermalOverload_CycleCount)
                ExoTwinsStateManager.TransitionToNextState();
        }

        // Reposition in anticipation of the laser firing.
        if (wrapperAITimer <= hoverRedirectTime)
        {
            if (wrapperAITimer == (int)(hoverRedirectTime * 0.5f))
                SoundEngine.PlaySound(Artemis.ChargeTelegraphSound, npc.Center);

            float hoverRedirectInterpolant = wrapperAITimer / (float)hoverRedirectTime;
            npc.rotation = npc.rotation.AngleLerp(npc.AngleTo(Target.Center), hoverRedirectInterpolant * 0.75f);

            float hoverSpeedInterpolant = MathHelper.SmoothStep(0.04f, 0.3f, LumUtils.Convert01To010(hoverRedirectInterpolant.Cubed()).Squared());
            Vector2 hoverDestination = Target.Center + hoverOffsetAngle.ToRotationVector2() * new Vector2(700f, 500f);

            while (Collision.SolidCollision(hoverDestination - Vector2.One * 300f, 600, 600))
                hoverDestination.Y -= 50f;

            npc.Center = Vector2.Lerp(npc.Center, hoverDestination, 0.03f);
            npc.SmoothFlyNear(hoverDestination, hoverSpeedInterpolant, 1f - hoverSpeedInterpolant);

            artemisAttributes.Animation = ExoTwinAnimation.Idle;
        }

        // Slow down before charging up.
        else if (wrapperAITimer <= hoverRedirectTime + slowDownTime)
        {
            npc.velocity *= 0.86f;
            npc.rotation = npc.rotation.AngleLerp(hoverOffsetAngle + MathHelper.Pi, 0.3f);
        }

        // Charging up energy.
        else if (wrapperAITimer <= hoverRedirectTime + slowDownTime + chargeUpTime)
        {
            npc.velocity *= 0.7f;

            float energyChargeUpInterpolant = LumUtils.InverseLerp(0f, chargeUpTime, wrapperAITimer - hoverRedirectTime - slowDownTime);

            ScreenShakeSystem.StartShakeAtPoint(npc.Center, energyChargeUpInterpolant.Squared() * 5f);

            for (int i = 0; i < 2; i++)
            {
                if (Main.rand.NextBool(energyChargeUpInterpolant * 1.6f) && energyChargeUpInterpolant < 0.75f)
                {
                    Vector2 focusPoint = npc.Center + npc.rotation.ToRotationVector2() * 76f;
                    Vector2 spawnPosition = focusPoint + Main.rand.NextVector2Unit() * Main.rand.NextFloat(300f, 600f);
                    LineStreakParticle streak = new LineStreakParticle(spawnPosition, (focusPoint - spawnPosition) * 0.08f, Color.Orange, 15, spawnPosition.AngleTo(focusPoint) + MathHelper.PiOver2, new Vector2(0.04f, 1f), new Vector2(0.04f, 1f));
                    streak.Spawn();
                }
            }
            artemisAttributes.SpecificDrawAction = () =>
            {
                Vector2 start = npc.Center + npc.rotation.ToRotationVector2() * 76f;
                Vector2 end = start + npc.rotation.ToRotationVector2() * 3000f;
                DoBehavior_ExothermalOverload_ArtemisRenderTelegraphBeam(start, end, energyChargeUpInterpolant);
            };
        }

        // Create a massive visual glean.
        else if (wrapperAITimer <= hoverRedirectTime + slowDownTime + chargeUpTime + energyGleamTime)
        {
            float gleamAnimationCompletion = LumUtils.InverseLerp(0f, energyGleamTime, wrapperAITimer - hoverRedirectTime - slowDownTime - chargeUpTime);
            float gleamInterpolant = LumUtils.InverseLerp(0f, 0.7f, gleamAnimationCompletion);
            artemisAttributes.SpecificDrawAction = () =>
            {
                DoBehavior_ExothermalOverload_ArtemisRenderGleam(npc.Center + npc.rotation.ToRotationVector2() * 76f - Main.screenPosition, gleamInterpolant);
            };
        }

        // Shoot the disintegration ray.
        if (wrapperAITimer == hoverRedirectTime + slowDownTime + chargeUpTime + energyGleamTime)
        {
            SoundEngine.PlaySound(Artemis.SpinLaserbeamSound);

            for (int i = 0; i < 4; i++)
            {
                CustomExoMechsSky.LightningData? lightning = CustomExoMechsSky.CreateLightning();
                if (lightning is not null)
                    lightning.Brightness *= 1.81f;
            }

            ScreenShakeSystem.StartShakeAtPoint(npc.Center, 15f, shakeStrengthDissipationIncrement: 1.11f);
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 left = (hoverOffsetAngle - MathHelper.PiOver2).ToRotationVector2();
                Vector2 right = -left;
                Vector2 directionToTarget = npc.SafeDirectionTo(Target.Center);
                LumUtils.NewProjectileBetter(npc.GetSource_FromAI(), npc.Center, Vector2.Zero, ModContent.ProjectileType<ExothermalDisintegrationRay>(), ExothermalDisintegrationRayDamage, 0f);

                moveDirection = Vector2.Dot(left, directionToTarget) > Vector2.Dot(right, directionToTarget) ? left.ToRotation() : right.ToRotation();
            }
        }

        // Move the laser forward.
        else if (wrapperAITimer >= hoverRedirectTime + slowDownTime + chargeUpTime + energyGleamTime)
        {
            ScreenShakeSystem.SetUniversalRumble(4f, MathHelper.TwoPi, null, 0.2f);
            npc.velocity = Vector2.Lerp(npc.velocity, moveDirection.ToRotationVector2() * 80f, 0.016f);
        }

        artemisAttributes.Frame = artemisAttributes.Animation.CalculateFrame(AITimer / 30f % 1f, artemisAttributes.InPhase2);
    }

    /// <summary>
    /// AI update loop method for the ExothermalOverload attack for Apollo specifically.
    /// </summary>
    /// <param name="npc">Apollo's NPC instance.</param>
    /// <param name="apolloAttributes">Apollo's designated generic attributes.</param>
    public static void DoBehavior_ExothermalOverload_ApolloHaloPlasmaDashes(NPC npc, IExoTwin apolloAttributes)
    {
        int spinTime = 45;
        int dashDelay = 14;
        int dashRepositionTime = 5;
        int dashTime = 27;
        int wrappedAITimer = AITimer % (spinTime + dashDelay + dashRepositionTime + dashTime);
        ref float spinOffsetAngle = ref SharedState.Values[0];
        ref float spinRadius = ref SharedState.Values[1];

        if (!npc.TryGetBehavior(out ApolloBehavior apollo))
            return;

        if (wrappedAITimer == 1)
        {
            spinOffsetAngle = Main.rand.Next(8) * MathHelper.TwoPi / 8f;
            spinRadius = 200f;
            npc.netUpdate = true;
        }

        // ∫(0, 1) sin(pi * x^0.7)^13 dx. There's almost certainly some a/(b * pi) ratio that this corresponds to but it's easier asking the computer for a numerical approximation.
        float areaUnderBump = 0.20183f;

        float spinArc = MathHelper.PiOver2;
        float spinBump = MathF.Pow(LumUtils.Convert01To010(MathF.Pow(LumUtils.InverseLerp(0f, spinTime, wrappedAITimer), 0.7f)), 13f);
        spinOffsetAngle += spinBump / areaUnderBump * spinArc / spinTime;

        // Spin around the player.
        if (wrappedAITimer <= spinTime)
        {
            if (wrappedAITimer <= spinTime - 14)
            {
                float hoverSpeedInterpolant = LumUtils.InverseLerp(0f, 15f, wrappedAITimer);
                Vector2 hoverDestination = Target.Center + spinOffsetAngle.ToRotationVector2() * spinRadius;
                npc.SmoothFlyNear(hoverDestination, hoverSpeedInterpolant * 0.2f, 1f - hoverSpeedInterpolant * 0.32f);
            }
            else
                npc.velocity *= 0.7f;

            if (wrappedAITimer == 12)
                SoundEngine.PlaySound(Artemis.ChargeTelegraphSound, Target.Center);

            npc.rotation = npc.rotation.AngleLerp(npc.AngleTo(Target.Center), 0.5f);

            spinRadius += 14.5f;
        }

        // Slow down before dashing.
        else if (wrappedAITimer <= spinTime + dashDelay)
        {
            npc.velocity *= 0.8f;
            npc.rotation = npc.rotation.AngleLerp(npc.AngleTo(Target.Center), 0.5f);
        }

        // Begin the dash, repositioning the direction over the course of a few frames.
        else if (wrappedAITimer <= spinTime + dashDelay + dashRepositionTime)
        {
            apollo.FlameEngulfInterpolant = LumUtils.Saturate(apollo.FlameEngulfInterpolant + 0.2f);
            npc.dontTakeDamage = true;
            npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(Target.Center) * 67f, 0.2f);
            if (wrappedAITimer == spinTime + dashDelay + 1)
            {
                npc.velocity += npc.SafeDirectionTo(Target.Center) * 41f;
                SoundEngine.PlaySound(Artemis.ChargeSound, Target.Center);
                ScreenShakeSystem.StartShake(15f, shakeStrengthDissipationIncrement: 1.02f);
            }

            if (npc.velocity.Length() >= 8f)
                npc.rotation = npc.rotation.AngleLerp(npc.velocity.ToRotation(), 0.2f);
        }

        // Accelerate and handle post-dash behaviors.
        else if (wrappedAITimer <= spinTime + dashDelay + dashRepositionTime + dashTime)
        {
            apollo.FlameEngulfInterpolant = 1f;

            npc.velocity = (npc.velocity * 1.04f).ClampLength(0f, 100f);
            npc.rotation = npc.rotation.AngleLerp(npc.velocity.ToRotation(), 0.4f);
            npc.damage = npc.defDamage;
        }

        // Teleport back around after the dash.
        if (wrappedAITimer >= spinTime + dashDelay + dashRepositionTime + dashTime - 1)
        {
            npc.Center = Target.Center - Target.SafeDirectionTo(npc.Center) * 1320f;
            npc.velocity *= 0.1f;
            npc.oldPos = new Vector2[npc.oldPos.Length];
        }

        apolloAttributes.Animation = ExoTwinAnimation.Attacking;
        apolloAttributes.Frame = apolloAttributes.Animation.CalculateFrame(wrappedAITimer / 30f % 1f, apolloAttributes.InPhase2);

        if (apollo.FlameEngulfInterpolant >= 0.67f)
            DoBehavior_ExothermalOverload_ReleasePlasmaFireParticles(npc);
    }

    /// <summary>
    /// Draws a projected ray on Artemis' pupil/central exo crystal for her part in the Exothermal Overload attack.
    /// </summary>
    public static void DoBehavior_ExothermalOverload_ArtemisRenderTelegraphBeam(Vector2 start, Vector2 end, float energyChargeUpInterpolant)
    {
        Vector2[] telegraphPoints = new Vector2[]
        {
            start,
            Vector2.Lerp(start, end, 0.2f),
            Vector2.Lerp(start, end, 0.4f),
            Vector2.Lerp(start, end, 0.6f),
            Vector2.Lerp(start, end, 0.8f),
            end
        };

        float widthFunction(float _) => LumUtils.InverseLerpBump(0f, 0.24f, 0.9f, 1f, energyChargeUpInterpolant) * 5f;
        Color colorFunction(float _) => new Color(255, 82, 10) * LumUtils.InverseLerpBump(0f, 0.1f, 0.9f, 1f, energyChargeUpInterpolant);

        ManagedShader lineShader = ShaderManager.GetShader("WoTM.ArtemisPhotonRayTelegraphShader");
        lineShader.SetTexture(MiscTexturesRegistry.WavyBlotchNoise.Value, 1, SamplerState.LinearWrap);

        PrimitiveRenderer.RenderTrail(telegraphPoints, new PrimitiveSettings(widthFunction, colorFunction, Shader: lineShader), 41);
    }

    /// <summary>
    /// Draws a gleam on Artemis' pupil/central exo crystal as a telegraph for her laserbeam during the Exothermal Overload attack.
    /// </summary>
    /// <param name="drawPosition">The base draw position of the Exo Twin.</param>
    public static void DoBehavior_ExothermalOverload_ArtemisRenderGleam(Vector2 drawPosition, float glimmerInterpolant)
    {
        Texture2D flare = MiscTexturesRegistry.ShineFlareTexture.Value;
        Texture2D bloom = MiscTexturesRegistry.BloomCircleSmall.Value;

        float flareOpacity = LumUtils.InverseLerp(1f, 0.75f, glimmerInterpolant);
        float flareScale = MathF.Pow(LumUtils.Convert01To010(glimmerInterpolant), 1.4f) * 1.9f + 0.1f;
        float flareRotation = MathHelper.SmoothStep(0f, MathHelper.TwoPi, MathF.Pow(glimmerInterpolant, 0.2f)) + MathHelper.PiOver4;
        Color flareColorA = Color.OrangeRed;
        Color flareColorB = Color.Yellow;
        Color flareColorC = Color.Wheat;

        Main.spriteBatch.Draw(bloom, drawPosition, null, flareColorA with { A = 0 } * flareOpacity * 0.3f, 0f, bloom.Size() * 0.5f, flareScale * 1.9f, 0, 0f);
        Main.spriteBatch.Draw(bloom, drawPosition, null, flareColorB with { A = 0 } * flareOpacity * 0.54f, 0f, bloom.Size() * 0.5f, flareScale, 0, 0f);
        Main.spriteBatch.Draw(flare, drawPosition, null, flareColorC with { A = 0 } * flareOpacity, flareRotation, flare.Size() * 0.5f, flareScale, 0, 0f);
    }

    /// <summary>
    /// Releases plasma fire particles from Apollo as he flies.
    /// </summary>
    public static void DoBehavior_ExothermalOverload_ReleasePlasmaFireParticles(NPC npc)
    {
        for (int i = 0; i < 7; i++)
        {
            Vector2 fireSpawnPosition = npc.Center + (npc.rotation + Main.rand.NextFloatDirection() * MathHelper.PiOver2).ToRotationVector2() * 60f + Main.rand.NextVector2Circular(24f, 24f);
            Vector2 fireVelocity = -npc.rotation.ToRotationVector2() * Main.rand.NextFloat(7.5f, 27.5f) + Main.rand.NextVector2Circular(14f, 14f);
            Color fireGlowColor = LumUtils.MulticolorLerp(Main.rand.NextFloat(), Color.Yellow, Color.YellowGreen, Color.Lime) * Main.rand.NextFloat(0.42f, 0.68f);
            Vector2 fireGlowScaleFactor = Vector2.One * Main.rand.NextFloat(0.12f, 0.24f);
            BloomPixelParticle fire = new(fireSpawnPosition, fireVelocity, Color.White, fireGlowColor, Main.rand.Next(11, 30), Vector2.One * 2f, null, fireGlowScaleFactor);
            fire.Spawn();
        }

        for (int i = 0; i < 3; i++)
        {
            float smokeSizeInterpolant = Main.rand.NextFloat();
            float smokeScale = MathHelper.Lerp(0.7f, 1.45f, smokeSizeInterpolant);
            Color smokeColor = Color.Lerp(Color.DarkSlateGray, Color.LightGray, Main.rand.NextFloat()) * MathHelper.Lerp(1f, 0.2f, smokeSizeInterpolant);
            Vector2 smokeVelocity = -npc.rotation.ToRotationVector2() * Main.rand.NextFloat(7.5f, 27.5f) + Main.rand.NextVector2Circular(14f, 14f);
            SmokeParticle smoke = new(npc.Center, smokeVelocity, smokeColor, 25, smokeScale, 0.04f);
            smoke.Spawn();
        }
    }
}
