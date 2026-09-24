using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Stage
{
    // The effect sheets: the roster's spells on plants, the hostile ones again on stones, and every delivery style,
    // each on its own cell with the healer in the last. Every spell and delivery is applied at once and captured at
    // two moments, at both cameras.
    public class LookSheetEffectPass
    {
        public static readonly float[] Moments = { 0.3f, 2.5f };
        // The label mark of a row that drives the sink or a delivery source directly, not through the game's buffs
        // and projectiles
        public static readonly string FixtureMark = " (FIXTURE)";
        static readonly float settleSeconds = 1.2f;
        static readonly float takeBackSeconds = 1.5f;
        // Where a delivery aims from its source, and how far along that path its stand-in projectile sits
        static readonly Vector3 deliveryReach = new Vector3(1.2f, 0.4f, 1.2f);
        static readonly float deliveryProgress = 0.6f;
        static readonly DeliveryStyle[] styles =
        {
            DeliveryStyle.Direct, DeliveryStyle.Arc, DeliveryStyle.Rigid, DeliveryStyle.Swarm, DeliveryStyle.Bounce,
            DeliveryStyle.ChainSync, DeliveryStyle.Thrown
        };

        // What one effect cell shows, and what the round must take back
        class EffectCell
        {
            public string label;
            public string spell;
            public bool isOnStone;
            public bool isDelivery;
            public DeliveryStyle style;
            public ABuffHandlerFactory handler;
            public GameObject giver;
            public GameObject projectile;
            public int token;
        }

        LookSheetRun _run;

        public LookSheetEffectPass(LookSheetRun run)
        {
            _run = run;
        }

        public IEnumerator Run()
        {
            List<EffectCell> plants = new List<EffectCell>();
            foreach (string spell in LookSheetSpells.Spells)
            {
                plants.Add(new EffectCell { spell = spell, label = LookSheetSpells.Label(spell, false) });
            }

            List<EffectCell> stones = new List<EffectCell>();
            foreach (string spell in LookSheetSpells.OnStone)
            {
                string label = LookSheetSpells.Label(spell, true);
                stones.Add(new EffectCell { spell = spell, isOnStone = true, label = label });
            }

            foreach (DeliveryStyle style in styles)
            {
                stones.Add(new EffectCell { isDelivery = true, style = style, label = style.ToString() });
            }

            List<List<EffectCell>> batches = new List<List<EffectCell>> { plants, stones };
            for (int b = 0; b < batches.Count; b++)
            {
                List<EffectCell> batch = batches[b];
                List<Vector3> cells = _run.Cells(batch.Count + 1);
                if (!SpawnCells(batch, cells))
                {
                    _run.ClearBatch();
                    continue;
                }

                // The healer stands in the last cell, so its heal links start clear of the other cells
                _run.PlaceHealer(cells[cells.Count - 1], false);
                _run.HideHud();
                yield return AStageRun.Wait(settleSeconds);
                foreach (string camera in LookSheetRun.Cameras)
                {
                    yield return _run.Aim(camera);
                    float start = Time.time;
                    Apply(batch);
                    foreach (float moment in Moments)
                    {
                        while (Time.time < start + moment)
                        {
                            yield return _run.NextFrame();
                        }

                        string time = "t" + Mathf.RoundToInt(moment * 10f).ToString("00");
                        _run.CaptureSheet("effects", "effects-b" + (b + 1), camera, time);
                    }

                    TakeBack(batch);
                    yield return AStageRun.Wait(takeBackSeconds);
                }

                _run.ClearBatch();
                yield return _run.NextFrame();
            }
        }

        bool SpawnCells(List<EffectCell> batch, List<Vector3> cells)
        {
            RenderManager manager = _run.manager;
            EntityData plant = LookSheetData.LoadEntity("NormalEntity");
            EntityData stone = LookSheetData.LoadEntity("SoldierEntity");
            float size = manager.player.grid.size;
            for (int i = 0; i < batch.Count; i++)
            {
                EffectCell cell = batch[i];
                EntityData data = cell.isOnStone ? stone : plant;
                if (cell.spell == "Sprout")
                {
                    data = LookSheetSpells.Sapling(_run.created);
                }

                if (cell.spell != null)
                {
                    cell.handler = LookSheetSpells.Handler(cell.spell, _run.created);
                }

                if (IsFixture(cell))
                {
                    cell.label += FixtureMark;
                }

                Entity.EntityType side = cell.isOnStone ? Entity.EntityType.Computer : Entity.EntityType.Player;
                if (_run.Spawn(data, side, cells[i], cell.label, true) == null)
                {
                    return false;
                }

                if (cell.spell == "Transfusion")
                {
                    // The giver stands one cell to the left, inside the receiver's crop
                    Vector3 left = manager.player.grid.GetNearestWalkablePosition(cells[i] + Vector3.left * size);
                    cell.giver = _run.Spawn(plant, Entity.EntityType.Player, left, "GIVER", false);
                }
            }
            return true;
        }

        // A status or an outcome set on the sink, or a delivery begun on the source, skips the game's own path
        static bool IsFixture(EffectCell cell)
        {
            if (cell.isDelivery || cell.handler != null)
            {
                return true;
            }

            return cell.spell != null && LookSheetSpells.Impact(cell.spell) != 0f;
        }

        // Everything lands at once, so the two moments are measured from the same start
        void Apply(List<EffectCell> batch)
        {
            ISpellVisualSink sink = _run.manager.spellSink;
            GameObject healerGo = HealerGo();
            for (int i = 0; i < batch.Count; i++)
            {
                EffectCell cell = batch[i];
                GameObject target = _run.cells[i].gameObject;
                if (cell.isDelivery)
                {
                    BeginDelivery(cell, _run.cells[i]);
                    continue;
                }

                if (cell.handler != null)
                {
                    float duration = float.PositiveInfinity;
                    if (cell.handler.durationType == DurationType.Duration)
                    {
                        duration = cell.handler.duration;
                    }

                    sink.SetStatus(healerGo, target, cell.handler, LookSheetSpells.Stacks(cell.spell), 0f, duration);
                }

                float impact = LookSheetSpells.Impact(cell.spell);
                if (impact != 0f)
                {
                    sink.ShowImpact(healerGo, target, ResourceKind.Health, impact, false);
                }

                if (cell.giver != null)
                {
                    sink.ShowImpact(healerGo, cell.giver, ResourceKind.Health, -impact, false);
                }
            }
        }

        void BeginDelivery(EffectCell cell, Transform target)
        {
            IDeliverySource source = target.GetComponentInChildren<IDeliverySource>();
            GameObject projectileGo = new GameObject("LookSheetProjectile");
            _run.created.Add(projectileGo);
            Vector3 end = target.position + deliveryReach;
            projectileGo.transform.position = Vector3.Lerp(target.position + Vector3.up, end, deliveryProgress);
            cell.projectile = projectileGo;
            cell.token = _run.manager.NextDeliveryToken();
            bool isBegun = source != null && source.BeginDelivery(cell.token, cell.style, projectileGo.transform, end);
            Debug.Log($"[LookSheetEffectPass] Delivery {cell.style} begun {isBegun}");
        }

        void TakeBack(List<EffectCell> batch)
        {
            ISpellVisualSink sink = _run.manager.spellSink;
            GameObject healerGo = HealerGo();
            for (int i = 0; i < batch.Count; i++)
            {
                EffectCell cell = batch[i];
                if (cell.handler != null)
                {
                    sink.RemoveStatus(healerGo, _run.cells[i].gameObject, cell.handler);
                }

                if (cell.isDelivery)
                {
                    IDeliverySource source = _run.cells[i].GetComponentInChildren<IDeliverySource>();
                    if (source != null)
                    {
                        source.EndDelivery(cell.token);
                    }
                }
            }
        }

        GameObject HealerGo()
        {
            Character character = _run.manager.player.character;
            if (character == null)
            {
                return null;
            }
            return character.gameObject;
        }
    }
}
