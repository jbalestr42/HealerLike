using System.Collections;
using System.IO;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Deliveries;
using HealerLike.Render.Grass;
using HealerLike.Render.Zones;

namespace HealerLike.Render.Stage
{
    // A real wave seen through the game camera while the player casts every skill in turn on allies and enemies:
    // a still every second beside the ground's motion and state from above, and three more the first times a
    // liana reaches out, to grass-battle/ under the capture folder. The ground's own area and the board are outlined on the maps.
    public class GrassBattleRun : AStageRun
    {
        static readonly int shots = 10;
        static readonly float shotEvery = 1f;
        static readonly float castEvery = 0.7f;
        static readonly int mapSize = 360;

        protected override IEnumerator Run()
        {
            string folder = Path.Combine(StagePlay.CaptureFolder, "grass-battle");
            Directory.CreateDirectory(folder);
            _manager.SetLandscape(false);
            yield return Wait(0.5f);
            _player.PlaceAllies(_manager, StagePlayer.LoadAllies());
            _hud.nextWaveButton.onClick.Invoke();
            float started = Time.time;
            float nextCast = started + 1f;
            int shot = 0;
            int casts = 0;
            int armFrames = 0;
            int mostArmCapsules = 0;
            int reached = 0;
            int lastReach = -100;
            while (shot < shots)
            {
                if (Time.time >= nextCast)
                {
                    nextCast = Time.time + castEvery;
                    Entity.EntityType side = casts % 2 == 0 ? Entity.EntityType.Player : Entity.EntityType.Computer;
                    GameObject targetGo = First(side);
                    _player.CastOn(_manager, targetGo != null ? targetGo.GetComponent<Entity>() : null);
                    casts++;
                }

                if (Time.time - started >= (shot + 1) * shotEvery)
                {
                    Save(folder, shot);
                    shot++;
                }

                // The first moments a liana is out, caught between the regular stills
                int reaching = ArmCapsules();
                armFrames += reaching > 0 ? 1 : 0;
                mostArmCapsules = Mathf.Max(mostArmCapsules, reaching);
                if (reaching > 0 && reached < 3 && Time.frameCount - lastReach > 10)
                {
                    lastReach = Time.frameCount;
                    Save(folder, 90 + reached);
                    reached++;
                }

                yield return NextFrame();
            }

            Debug.Log($"[GrassBattleRun] {shots} shots, attacks {_attacks} heals {_heals} zones {_maxZones} in {folder}; "
                      + $"a liana was out on {armFrames} frames, pressing at most {mostArmCapsules} capsules");
            StagePlay.Finish(this, true);
        }

        GameObject First(Entity.EntityType side)
        {
            foreach (GameObject entityGo in _manager.entityManager.GetEntities(side))
            {
                if (entityGo != null)
                {
                    return entityGo;
                }
            }

            return null;
        }

        void Save(string folder, int shot)
        {
            Texture2D still = StageReadback.Render(_manager.gameCamera, StageCalibration.PortraitWidth / 2,
                                                   StageCalibration.PortraitHeight / 2);
            File.WriteAllBytes(Path.Combine(folder, $"shot-{shot:D2}.png"), still.EncodeToPNG());
            Object.Destroy(still);
            GroundSimulation ground = _manager.grass.simulation;
            if (ground == null)
            {
                return;
            }

            File.WriteAllBytes(Path.Combine(folder, $"motion-{shot:D2}.png"), Map(ground, ground.motion, false).EncodeToPNG());
            File.WriteAllBytes(Path.Combine(folder, $"state-{shot:D2}.png"), Map(ground, ground.state, true).EncodeToPNG());
        }

        // The capsules the lianas out of their rest add to every body this frame
        static int ArmCapsules()
        {
            BodyCapsule[] capsules = new BodyCapsule[TrampleZone.MaxCapsules];
            int total = 0;
            foreach (TrampleZone body in Object.FindObjectsByType<TrampleZone>())
            {
                ARigHost host = body.GetComponent<ARigHost>();
                bool isOut = false;
                for (int i = 0; host != null && i < host.armCount; i++)
                {
                    LianaArm arm = host.GetArm(i);
                    isOut |= arm != null && arm.phase != GesturePhase.Rest;
                }

                if (isOut)
                {
                    total += body.AppendCapsules(capsules, 0);
                }
            }

            return total;
        }

        // The ground from above, north up. Motion: lean east in red and north in green around grey. State: ash in
        // red, vitality around mid green, glow in blue. White outlines the board, grey the ground's own edge.
        Texture2D Map(GroundSimulation ground, RenderTexture source, bool isState)
        {
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = source;
            Texture2D copy = new Texture2D(source.width, source.height, TextureFormat.RGBAFloat, false, true);
            copy.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            copy.Apply();
            RenderTexture.active = previous;
            Texture2D map = new Texture2D(mapSize, mapSize, TextureFormat.RGB24, false);
            Bounds board = _manager.board;
            Rect area = ground.volume.area;
            for (int y = 0; y < mapSize; y++)
            {
                for (int x = 0; x < mapSize; x++)
                {
                    Color value = copy.GetPixelBilinear((x + 0.5f) / mapSize, (y + 0.5f) / mapSize);
                    Color colour = isState
                        ? new Color(value.r, 0.5f + 0.5f * value.g, value.b)
                        : new Color(0.5f + value.r, 0.5f + value.g, 0.5f);
                    Vector2 world = ground.volume.ToWorld(new Vector2((x + 0.5f) / mapSize, (y + 0.5f) / mapSize));
                    float line = area.width / mapSize;
                    bool onBoard = Mathf.Abs(world.x - board.min.x) < line || Mathf.Abs(world.x - board.max.x) < line
                                   || Mathf.Abs(world.y - board.min.z) < line || Mathf.Abs(world.y - board.max.z) < line;
                    bool inBoard = world.x > board.min.x - line && world.x < board.max.x + line
                                   && world.y > board.min.z - line && world.y < board.max.z + line;
                    if (onBoard && inBoard)
                    {
                        colour = Color.white;
                    }

                    map.SetPixel(x, y, colour);
                }
            }

            map.Apply();
            Object.Destroy(copy);
            return map;
        }
    }
}
