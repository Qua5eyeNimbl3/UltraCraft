using BepInEx.Configuration;
using UltraCraft.Link;
using UltraCraft.World;
using UnityEngine;
using UnityEngine.Rendering;

namespace UltraCraft.Render;

/// <summary>
/// Consumes Minecraft's render ring: the block atlas, section meshes, entity textures, the
/// player's own model, and the scene of other entities and particles. Lights, solids and dug
/// cells are not used; their messages are skipped by length like any unknown type.
/// Mirrors the message handling in skse/src/WorldRender.cpp.
/// </summary>
internal sealed unsafe class RenderRing
{
    private const long BytesPerFrame = 24L << 20;

    private readonly UltraLink link;
    private readonly Atlas atlas;
    private readonly BlockSections sections;
    private readonly TerrainCollision collision = new();
    private readonly WaterVolumes water = new();
    public int SectionCount => sections.Count;
    public int CollisionSectionCount => collision.Count;
    public int WaterSectionCount => water.Count;
    internal Atlas Atlas=>atlas;
    private readonly CapturedModel avatar;
    private readonly CapturedModel body;
    private readonly CapturedModel scene;
    private readonly WorldEntities entities;
    private Vector3 sceneOrigin;
    private readonly Rings.MessageHandler handler;
    private Mesh? outline;
    private Material? outlineMaterial;
    private int unknownLogged;
    private float lastLog;
    private float lastEvict;
    private int messages;

    public RenderRing(UltraLink link, ConfigFile config)
    {
        this.link = link;
        atlas = new Atlas(config);
        sections = new BlockSections(atlas);
        avatar = new CapturedModel(atlas, "avatar");
        body = new CapturedModel(atlas, "mirror body");
        scene = new CapturedModel(atlas, "scene");
        entities = new WorldEntities(link, atlas);
        handler = Handle;
    }

    /// <summary>Drains the ring every frame (Minecraft blocks when it fills) and draws when <paramref name="visible"/>.</summary>
    public void Update(bool visible, Vector3 feet, float mcYaw, bool firstPerson, bool showSelection, bool anchored)
    {
        atlas.ResolveShaders();
        link.DrainRender(BytesPerFrame, handler);
        sections.Update();
        water.Update(feet);
        if (anchored && Time.unscaledTime - lastEvict > 2f)
        {
            // Java unloads chunks behind the player but never says so: forget far terrain here.
            lastEvict = Time.unscaledTime;
            Coords.ToMc(feet, out double mx, out _, out double mz);
            int cx = Mathf.FloorToInt((float)mx / 16f), cz = Mathf.FloorToInt((float)mz / 16f), keep = Generated.Design.World_Fresh.KeepRadiusChunks;
            sections.Evict(cx, cz, keep);
            collision.Evict(cx, cz, keep);
            water.Evict(cx, cz, keep);
        }
        sections.SetVisible(visible);
        if (!visible)
        {
            return;
        }
        if (!firstPerson)
        {
            avatar.Draw(Matrix4x4.Translate(feet)); // third person: every camera sees the player
        }

        scene.Draw(Matrix4x4.Translate(sceneOrigin));
        entities.Draw();
        if (showSelection)
        {
            DrawSelection();
        }
        if (Time.unscaledTime - lastLog > 30f && messages > 0)
        {
            Plugin.Log.LogInfo($"blocks: {sections.Count} sections live, {messages} render messages in the last {Time.unscaledTime - lastLog:F0} s");
            lastLog = Time.unscaledTime;
            messages = 0;
        }
    }

    /// <summary>Link lost: nothing of Minecraft's stays on screen, and a returning Minecraft resends everything.</summary>
    public void Reset()
    {
        sections.Clear();
        collision.Clear();
        water.Clear();
        avatar.Hide();
        body.Hide();
        scene.Hide();
    }

    /// <summary>
    /// In first person the player must not see their own model, but PEAK's mirrors should: PEAK's
    /// own body is hidden, so without this they would show nobody. The animated model is drawn for
    /// the mirror cameras only. A Minecraft mod that sends no model in first person (SkyCraft's
    /// default) still sends the standing body it keeps for a death ragdoll; that is the fallback.
    /// </summary>
    private void Handle(uint type, byte* payload, uint bytes)
    {
        messages++;
        switch (type)
        {
            case Proto.RenClearAll:
                sections.Clear();
        collision.Clear();
                water.Clear();
                avatar.Hide();
                scene.Hide();
                break;
            case Proto.RenAtlas when bytes >= sizeof(Proto.RenAtlasHdr):
            {
                var header = (Proto.RenAtlasHdr*)payload;
                if (PayloadChecks.Texture(header->width, header->height, bytes, (uint)sizeof(Proto.RenAtlasHdr)))
                {
                    atlas.SetTexture(0, (int)header->width, (int)header->height, payload + sizeof(Proto.RenAtlasHdr));
                }
                break;
            }
            case Proto.RenAtlasRegion when bytes >= sizeof(Proto.RenAtlasRegionHdr):
            {
                var header = (Proto.RenAtlasRegionHdr*)payload;
                if (PayloadChecks.Texture(header->width, header->height, bytes, (uint)sizeof(Proto.RenAtlasRegionHdr)))
                {
                    atlas.SetRegion((int)header->x, (int)header->y, (int)header->width, (int)header->height, payload + sizeof(Proto.RenAtlasRegionHdr));
                }
                break;
            }
            case Proto.RenTexture when bytes >= sizeof(Proto.RenTextureHdr):
            {
                var header = (Proto.RenTextureHdr*)payload;
                if (header->id != 0 && PayloadChecks.Texture(header->width, header->height, bytes, (uint)sizeof(Proto.RenTextureHdr)))
                {
                    atlas.SetTexture(header->id, (int)header->width, (int)header->height, payload + sizeof(Proto.RenTextureHdr));
                }
                break;
            }
            case Proto.RenSection when bytes >= sizeof(Proto.RenSectionHdr):
            {
                var header = (Proto.RenSectionHdr*)payload;
                if (bytes >= (ulong)sizeof(Proto.RenSectionHdr) + (ulong)header->vertexCount * (ulong)sizeof(Proto.RenVertex))
                {
                    sections.SetSection(*header, (Proto.RenVertex*)(payload + sizeof(Proto.RenSectionHdr)));
                }
                break;
            }
            case Proto.RenAvatar when bytes >= sizeof(Proto.RenAvatarHdr):
            case Proto.RenRagdoll when bytes >= sizeof(Proto.RenAvatarHdr):
            {
                var header = (Proto.RenAvatarHdr*)payload;
                ulong need = (ulong)sizeof(Proto.RenAvatarHdr) + (ulong)header->batchCount * (ulong)sizeof(Proto.RenBatch) + (ulong)header->vertexCount * (ulong)sizeof(Proto.RenVertex);
                if (bytes >= need)
                {
                    var batches = (Proto.RenBatch*)(payload + sizeof(Proto.RenAvatarHdr));
                    if (!PayloadChecks.Model(header->batchCount, header->vertexCount, batches)) break;
                    (type == Proto.RenAvatar ? avatar : body).Set(header->batchCount, header->vertexCount, batches, (Proto.RenVertex*)(batches + header->batchCount));
                }
                break;
            }
            case Proto.RenScene when bytes >= sizeof(Proto.RenSceneHdr):
            {
                // Every other entity (TNT, boats, chests, ...) and all particles, relative to a block near the camera.
                var header = (Proto.RenSceneHdr*)payload;
                ulong need = (ulong)sizeof(Proto.RenSceneHdr) + (ulong)header->batchCount * (ulong)sizeof(Proto.RenBatch) + (ulong)header->vertexCount * (ulong)sizeof(Proto.RenVertex);
                if (bytes >= need)
                {
                    var batches = (Proto.RenBatch*)(payload + sizeof(Proto.RenSceneHdr));
                    if (!PayloadChecks.Model(header->batchCount, header->vertexCount, batches)) break;
                    sceneOrigin = Coords.ToUnity(header->originX, header->originY, header->originZ);
                    scene.Set(header->batchCount, header->vertexCount, batches, (Proto.RenVertex*)(batches + header->batchCount));
                }
                break;
            }
            case 8:  // lights
                break;
            case 10: // solids
                collision.Solids(payload, bytes);
                break;
            case 14:
                collision.Partials(payload, bytes);
                break;
            case Generated.Design.Fluids_Water.MessageType:
                water.Receive(payload,bytes);
                break;
            case 15:
            case 11: // dug
                break;
            default:
                if (unknownLogged++ < 5)
                {
                    Plugin.Log.LogWarning($"blocks: skipping unknown render message type {type} ({bytes} bytes)");
                }
                break;
        }
    }

    /// <summary>The outline around the block Minecraft's crosshair is on, from the world-entities table.</summary>
    private void DrawSelection()
    {
        if (!link.ReadSelection(out Vector3 min, out Vector3 max))
        {
            return;
        }
        if (outline == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                return;
            }
            outlineMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            // Minecraft's outline: thin, black, 40% opaque.
            outlineMaterial.SetColor("_Color", new Color(0f, 0f, 0f, 0.4f));
            outlineMaterial.SetColor("_BaseColor", new Color(0f, 0f, 0f, 0.4f));
            outline = new Mesh { hideFlags = HideFlags.HideAndDontSave };
            var corners = new Vector3[8];
            for (int i = 0; i < 8; i++)
            {
                corners[i] = new Vector3(i & 1, (i >> 1) & 1, (i >> 2) & 1);
            }
            outline.vertices = corners;
            outline.SetIndices(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 0, 2, 1, 3, 4, 6, 5, 7, 0, 4, 1, 5, 2, 6, 3, 7 }, MeshTopology.Lines, 0);
        }
        // Minecraft box to Unity box: Z flips, so the far Z becomes the near one.
        Vector3 a = Coords.ToUnity(min.x, min.y, max.z);
        Vector3 b = Coords.ToUnity(max.x, max.y, min.z);
        const float grow = 0.004f;
        Matrix4x4 matrix = Matrix4x4.TRS(a - Vector3.one * grow, Quaternion.identity, b - a + Vector3.one * (2f * grow));
        Graphics.DrawMesh(outline, matrix, outlineMaterial, 0, null, 0, null, ShadowCastingMode.Off, receiveShadows: false);
    }
}
