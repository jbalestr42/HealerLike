using System;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using UnityEngine;

namespace HealerLike.Render.Spells
{
    // Compact grammar marks are geometry too. A circular backing keeps the icon readable on any UI panel.
    public sealed class SpellIconSeal : IDisposable
    {
        static readonly Color Backing = new Color(.025f, .085f, .09f, 1);
        static readonly Color Rim = new Color(.66f, .81f, .73f, 1);
        readonly Transform _root;
        readonly Material _material;
        readonly ShapeMeshCache _meshes = new ShapeMeshCache();
        bool _disposed;

        public SpellIconSeal(Transform root, Material material)
        {
            _root = root;
            _material = material;
        }

        public void Build(SpellIconRecipe recipe)
        {
            Part("Seal backing", ShapeProfile.Bulb(), new Vector3(0, 0, 1),
                new Vector3(2.72f, 2.72f, .12f), Background(recipe));
            Ring("Seal rim", Vector3.forward * .85f, 2.75f, Rim);
            bool periodic = false;
            bool lasting = false;
            foreach (EffectRecipe layer in recipe.Entries())
            {
                periodic |= layer.tempo == EffectTempo.PerPeriod;
                lasting |= layer.tempo == EffectTempo.ForDuration;
            }
            if (periodic || lasting)
            {
                Ring("Tempo inner rim", Vector3.forward * .8f, 2.5f, Accent(recipe));
            }
            if (periodic)
            {
                for (int i = 0; i < 3; i++)
                {
                    float angle = (55 + i * 35) * Mathf.Deg2Rad;
                    Pearl("Periodic beat " + i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), -.35f) * 1.23f,
                        .13f, Rim);
                }
            }
            Reach(recipe.reach);
            Trigger(recipe.trigger);
            if (recipe.origin == EffectOrigin.Item)
            {
                Part("Item origin", ShapeProfile.Block(), new Vector3(1.04f, .65f, -.4f),
                    Vector3.one * .17f, Rim, new Vector3(0, 0, 45));
            }
            if (recipe.layers.Count > SpellIconSubject.MaxGlyphs)
            {
                Overflow(recipe.layers.Count - SpellIconSubject.MaxGlyphs);
            }
        }

        static Color Background(SpellIconRecipe recipe)
        {
            float brightness = 0f;
            foreach (EffectRecipe layer in recipe.layers)
            {
                brightness += layer.colour.grayscale;
            }
            return brightness / recipe.layers.Count < .3f ? new Color(.48f, .6f, .62f, 1) : Backing;
        }

        static Color Accent(SpellIconRecipe recipe)
        {
            return recipe.layers.Count > 0 ? Color.Lerp(recipe.layers[0].colour, Rim, .35f) : Rim;
        }

        void Reach(EffectReach reach)
        {
            if (reach == EffectReach.Single)
            {
                return;
            }
            if (reach == EffectReach.Area)
            {
                Ring("Area footprint", new Vector3(0, -1.02f, -.45f), .36f, Rim);
                return;
            }
            int count = reach == EffectReach.All ? 5 : 3;
            if (reach == EffectReach.Chain)
            {
                Part("Chain reach", ShapeProfile.Segment(0), new Vector3(0, -1.02f, -.4f),
                    new Vector3(.045f, .65f, .045f), Rim, new Vector3(0, 0, 90));
            }
            for (int i = 0; i < count; i++)
            {
                Pearl("Reach target " + i, new Vector3((i - (count - 1) * .5f) * .22f, -1.02f, -.45f),
                    .135f, Rim);
            }
        }

        void Trigger(EffectTrigger trigger)
        {
            Vector3 at = new Vector3(-1.02f, .63f, -.4f);
            if (trigger == EffectTrigger.Cast)
            {
                return;
            }
            if (trigger == EffectTrigger.RoundEnd)
            {
                Ring("Round trigger", at, .22f, Rim);
                return;
            }
            if (trigger == EffectTrigger.Equip)
            {
                Part("Equip trigger", ShapeProfile.Block(), at, Vector3.one * .17f, Rim);
                return;
            }
            Part("Impact trigger", ShapeProfile.Shard(), at, new Vector3(.12f, .3f, .08f), Rim,
                new Vector3(0, 0, -35));
            if (trigger == EffectTrigger.OnDeath)
            {
                Part("Death trigger", ShapeProfile.Shard(), at, new Vector3(.12f, .3f, .08f), Rim,
                    new Vector3(0, 0, 35));
            }
        }

        void Overflow(int remaining)
        {
            // A plus flags extra layers; up to six count dots stay legible beside the four main glyphs.
            Vector3 at = new Vector3(.9f, -.72f, -.5f);
            Part("More effects horizontal", ShapeProfile.Segment(0), at, new Vector3(.04f, .2f, .04f), Rim,
                new Vector3(0, 0, 90));
            Part("More effects vertical", ShapeProfile.Segment(0), at, new Vector3(.04f, .2f, .04f), Rim);
            for (int i = 0; i < Mathf.Min(remaining, 6); i++)
            {
                Pearl("Additional effect " + i,
                    new Vector3(.64f + i * .07f, -.92f, -.5f), .055f, Rim);
            }
        }

        void Pearl(string name, Vector3 at, float size, Color colour)
        {
            Part(name, ShapeProfile.Bulb(), at, Vector3.one * size, colour);
        }

        void Ring(string name, Vector3 at, float diameter, Color colour)
        {
            Part(name, ShapeProfile.Ring(.06f), at, new Vector3(diameter, .05f, diameter), colour,
                new Vector3(90, 0, 0));
        }

        void Part(string name, ShapeProfile shape, Vector3 at, Vector3 size, Color colour,
            Vector3 rotation = default)
        {
            Transform part = PrimitiveMeshes.Geometry(name, _root, _meshes.Get(shape), _material, colour, .12f);
            part.localPosition = at;
            part.localScale = size;
            part.localRotation = Quaternion.Euler(rotation);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
            _meshes.Dispose();
        }
    }
}
