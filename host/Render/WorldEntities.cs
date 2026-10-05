using System.Collections.Generic;
using UltraCraft.Link;
using UltraCraft.World;
using UnityEngine;
using UnityEngine.Rendering;

namespace UltraCraft.Render;

/// <summary>
/// The light entities Minecraft lists in the world-entities table instead of capturing as
/// geometry: arrows in flight and stuck, tridents, dropped and thrown items. Each is a few quads
/// built here from its atlas rectangles, all in one mesh per frame. Block cracks and contact
/// shadows in the same table are not drawn.
/// </summary>
internal sealed class WorldEntities
{
    private const uint Arrow = 1, Item = 2, Trident = 3, Block = 4, Crack = 5;
    // Minecraft's arrow model: 16 x 5 texels at 0.9 / 16 blocks per texel, shifted 4 texels back.
    private const float Texel = 0.05625f;

    private readonly UltraLink link;
    private readonly Atlas atlas;
    private readonly List<UltraLink.WorldEntity> entities = new();
    private readonly List<Vector3> positions = new();
    private readonly List<Vector2> uvs = new();
    private readonly List<Vector2> lightUvs = new();
    private readonly List<Color32> colours = new();
    private readonly List<int> indices = new();
    private Mesh? mesh;
    private bool logged;

    public WorldEntities(UltraLink link, Atlas atlas)
    {
        this.link = link;
        this.atlas = atlas;
    }

    public void Draw()
    {
        if (!atlas.Usable || !link.ReadWorldEntities(entities) || entities.Count == 0)
        {
            return;
        }
        positions.Clear();
        uvs.Clear();
        colours.Clear();
        indices.Clear();
        foreach (UltraLink.WorldEntity e in entities)
        {
            Vector3 at = Coords.ToUnity(e.x, e.y, e.z);
            switch (e.kind)
            {
                case Arrow:
                    AddArrow(at, e);
                    break;
                case Item:
                case Trident:
                    AddSprite(at, e);
                    break;
                case Block:
                    AddCube(at, e);
                    break;
                case Crack:
                    AddCrack(e);
                    break;
            }
        }
        if (indices.Count == 0)
        {
            return;
        }
        if (mesh == null)
        {
            mesh = new Mesh { hideFlags = HideFlags.HideAndDontSave, indexFormat = IndexFormat.UInt32 };
            mesh.MarkDynamic();
        }
        mesh.Clear();
        mesh.SetVertices(positions);
        mesh.SetUVs(0, uvs);
        lightUvs.Clear();
        for(int i=0;i<positions.Count;i++)lightUvs.Add(Atlas.LightUv(0xF0F));
        mesh.SetUVs(1,lightUvs);
        mesh.SetColors(colours);
        mesh.SetTriangles(indices, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        Material? material = atlas.Material(0, Surface.Cutout, twoSided: true); // sprites: holes all over
        if (material != null)
        {
            Graphics.DrawMesh(mesh, Matrix4x4.identity, material, 0, null, 0, null, ShadowCastingMode.On, receiveShadows: true);
        }
        if (!logged)
        {
            logged = true;
            Plugin.Log.LogInfo($"entities: drawing Minecraft's arrows and items ({entities.Count} this frame)");
        }
    }

    private void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float u0, float v0, float u1, float v1, Color32 colour)
    {
        int first = positions.Count;
        positions.Add(a);
        positions.Add(b);
        positions.Add(c);
        positions.Add(d);
        uvs.Add(new Vector2(u0, v1));
        uvs.Add(new Vector2(u1, v1));
        uvs.Add(new Vector2(u1, v0));
        uvs.Add(new Vector2(u0, v0));
        for (int i = 0; i < 4; i++)
        {
            colours.Add(colour);
        }
        indices.Add(first);
        indices.Add(first + 1);
        indices.Add(first + 2);
        indices.Add(first);
        indices.Add(first + 2);
        indices.Add(first + 3);
    }

    // Two crossed side views along the flight direction, plus the square back plate.
    private void AddArrow(Vector3 at, in UltraLink.WorldEntity e)
    {
        float yaw = e.yaw * Mathf.Deg2Rad, pitch = e.pitch * Mathf.Deg2Rad;
        // Minecraft's arrow heading (x = sin yaw, z = cos yaw), with Z flipped for Unity.
        var forward = new Vector3(Mathf.Sin(yaw) * Mathf.Cos(pitch), Mathf.Sin(pitch), -Mathf.Cos(yaw) * Mathf.Cos(pitch));
        Vector3 side = Vector3.Cross(Mathf.Abs(forward.y) > 0.99f ? Vector3.right : Vector3.up, forward).normalized;
        Vector3 up = Vector3.Cross(forward, side);
        Vector3 tail = at - forward * (12f * Texel), tip = at + forward * (4f * Texel);
        var white = new Color32(255, 255, 255, 255);
        for (int k = 0; k < 2; k++)
        {
            Vector3 across = (k == 0 ? side + up : side - up).normalized * (2.5f * Texel);
            Quad(tail - across, tip - across, tip + across, tail + across, e.uv0, e.uv1, e.uv2, e.uv3, white);
        }
        Vector3 back = at - forward * (11f * Texel);
        Vector3 s = side * (2.5f * Texel), u = up * (2.5f * Texel);
        Quad(back - s - u, back + s - u, back + s + u, back - s + u, e.uv4, e.uv5, e.uv6, e.uv7, white);
    }

    // A dropped item, thrown item or trident: its icon as a flat sprite turning about the vertical.
    private void AddSprite(Vector3 at, in UltraLink.WorldEntity e)
    {
        float size = e.scale > 0f ? e.scale : 0.5f;
        Vector3 right = Quaternion.Euler(0f, e.yaw, 0f) * Vector3.right * (size * 0.5f);
        Vector3 up = Vector3.up * size;
        Quad(at - right, at + right, at + right + up, at - right + up, e.uv0, e.uv1, e.uv2, e.uv3, new Color32(255, 255, 255, 255));
    }

    // Minecraft's block-breaking cracks: the destroy-stage sprite on every face of the block's box
    // (x, y, z = its minimum corner, ext = its size, in blocks), pushed out slightly against z-fighting.
    private void AddCrack(in UltraLink.WorldEntity e)
    {
        const float grow = 0.01f;
        Vector3 p0 = Coords.ToUnity(e.x - grow, e.y - grow, e.z + e.ext2 + grow);
        Vector3 p1 = Coords.ToUnity(e.x + e.ext0 + grow, e.y + e.ext1 + grow, e.z - grow);
        var lo = Vector3.Min(p0, p1);
        var hi = Vector3.Max(p0, p1);
        var c = new Color32(255, 255, 255, 255);
        Vector3 C(int x, int y, int z) => new Vector3(x == 0 ? lo.x : hi.x, y == 0 ? lo.y : hi.y, z == 0 ? lo.z : hi.z);
        Quad(C(0, 0, 0), C(1, 0, 0), C(1, 1, 0), C(0, 1, 0), e.uv0, e.uv1, e.uv2, e.uv3, c);
        Quad(C(1, 0, 1), C(0, 0, 1), C(0, 1, 1), C(1, 1, 1), e.uv0, e.uv1, e.uv2, e.uv3, c);
        Quad(C(0, 0, 1), C(0, 0, 0), C(0, 1, 0), C(0, 1, 1), e.uv0, e.uv1, e.uv2, e.uv3, c);
        Quad(C(1, 0, 0), C(1, 0, 1), C(1, 1, 1), C(1, 1, 0), e.uv0, e.uv1, e.uv2, e.uv3, c);
        Quad(C(0, 1, 0), C(1, 1, 0), C(1, 1, 1), C(0, 1, 1), e.uv0, e.uv1, e.uv2, e.uv3, c);
        Quad(C(0, 0, 1), C(1, 0, 1), C(1, 0, 0), C(0, 0, 0), e.uv0, e.uv1, e.uv2, e.uv3, c);
    }

    // A dropped block: a small spinning cube with the block's side, top and bottom textures.
    private void AddCube(Vector3 at, in UltraLink.WorldEntity e)
    {
        float size = e.scale > 0f ? e.scale : 0.25f;
        Quaternion spin = Quaternion.Euler(0f, e.yaw, 0f);
        Vector3 x = spin * Vector3.right * (size * 0.5f), z = spin * Vector3.forward * (size * 0.5f), y = Vector3.up * size;
        var white = new Color32(255, 255, 255, 255);
        Color32 tint = e.tint != 0 ? new Color32((byte)e.tint, (byte)(e.tint >> 8), (byte)(e.tint >> 16), 255) : white;
        Quad(at - x - z, at + x - z, at + x - z + y, at - x - z + y, e.uv0, e.uv1, e.uv2, e.uv3, white);
        Quad(at + x - z, at + x + z, at + x + z + y, at + x - z + y, e.uv0, e.uv1, e.uv2, e.uv3, white);
        Quad(at + x + z, at - x + z, at - x + z + y, at + x + z + y, e.uv0, e.uv1, e.uv2, e.uv3, white);
        Quad(at - x + z, at - x - z, at - x - z + y, at - x + z + y, e.uv0, e.uv1, e.uv2, e.uv3, white);
        Quad(at - x - z + y, at + x - z + y, at + x + z + y, at - x + z + y, e.uv4, e.uv5, e.uv6, e.uv7, tint);
        Quad(at - x + z, at + x + z, at + x - z, at - x - z, e.uv8, e.uv9, e.uv10, e.uv11, white);
    }
}
