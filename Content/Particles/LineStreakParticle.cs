﻿using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace WoTM.Content.Particles;

public class LineStreakParticle : Particle
{
    /// <summary>
    /// The starting scale for the streak.
    /// </summary>
    public Vector2 StartingScale;

    /// <summary>
    /// The ending scale for the streak.
    /// </summary>
    public Vector2 EndingScale;

    public override string AtlasTextureName => "WoTM.BasicMetaballCircle.png";

    public override BlendState BlendState => BlendState.Additive;

    public LineStreakParticle(Vector2 position, Vector2 velocity, Color color, int lifetime, float rotation, Vector2 scale, Vector2 endingScale)
    {
        Position = position;
        Velocity = velocity;
        DrawColor = color;
        Scale = scale;
        Lifetime = lifetime;
        Rotation = rotation;
        StartingScale = scale;
        EndingScale = endingScale;
    }

    public override void Update()
    {
        Scale = Vector2.Lerp(StartingScale, EndingScale, LifetimeRatio);
        DrawColor *= Utils.Remap(LifetimeRatio, 0.7f, 1f, 1f, 0.85f);
        Velocity *= 0.965f;
    }
}
