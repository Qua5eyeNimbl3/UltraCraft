using System.Collections.Generic;
using UltraCraft.Link;
using UltraCraft.World;
using UnityEngine;
using UnityEngine.Rendering;

namespace UltraCraft.Render;

/// <summary>
/// Minecraft's placed blocks as Unity meshes. Java sends one 16-block section at a time; sections are
/// kept as data and drawn as one mesh per chunk column (up to ~24 sections), so the renderer count and
/// draw calls (times every shadow cascade) stay low as more of the world loads.
/// </summary>
internal sealed unsafe class BlockSections
{
    private sealed class Section
    {
        public Vector3[] Positions = null!;
        public Vector3[] Normals = null!;
        public Vector2[] Uvs = null!;
        public Vector2[] Light = null!;
        public Color32[] Colours = null!;
        public int[] Solid = null!, Cutout = null!, Blended = null!;
    }

    private sealed class Column
    {
        public readonly SortedDictionary<int, Section> Sections = new();
        public GameObject? Object;
        public Mesh? Mesh;
        public bool Queued;
    }

    private const int ColumnsPerFrame = 6;
    private const float RebuildBudgetMs = 4f;

    private readonly Atlas atlas;
    private readonly Dictionary<(int, int), Column> columns = new();
    private readonly LinkedList<(int, int)> dirty = new();
    private readonly List<Vector3> positions = new();
    private readonly List<Vector3> normals = new();
    private readonly List<Vector2> uvs = new();
    private readonly List<Vector2> lightUvs = new();
    private readonly List<Color32> colours = new();
    private readonly List<int> solid = new();
    private readonly List<int> cutout = new();
    private readonly List<int> blended = new();
    private readonly List<Material> materials = new(3);
    private GameObject? root;
    private int count;

    public BlockSections(Atlas atlas)
    {
        this.atlas = atlas;
    }

    public int Count => count;

    public void SetVisible(bool visible)
    {
        if (root != null && root.activeSelf != visible)
        {
            root.SetActive(visible);
        }
    }

    /// <summary>kRenClearAll: Minecraft's world changed (or it restarted); everything goes.</summary>
    public void Clear()
    {
        foreach (Column column in columns.Values)
        {
            Destroy(column);
        }
        columns.Clear();
        dirty.Clear();
        count = 0;
    }

    private static void Destroy(Column column)
    {
        if (column.Mesh != null)
        {
            Object.Destroy(column.Mesh);
        }
        if (column.Object != null)
        {
            Object.Destroy(column.Object);
        }
    }

    /// <summary>Drops columns more than <paramref name="radius"/> chunks away; Java re-sends them when they load again.</summary>
    public void Evict(int cx, int cz, int radius)
    {
        var far = new List<(int, int)>();
        foreach (var pair in columns)
        {
            if (System.Math.Abs(pair.Key.Item1 - cx) > radius || System.Math.Abs(pair.Key.Item2 - cz) > radius)
            {
                far.Add(pair.Key);
            }
        }
        foreach (var key in far)
        {
            count -= columns[key].Sections.Count;
            Destroy(columns[key]);
            columns.Remove(key);
        }
    }

    /// <summary>kRenSection: a section's whole mesh as a triangle list; no vertices removes it.</summary>
    public void SetSection(in Proto.RenSectionHdr header, Proto.RenVertex* vertices)
    {
        var key = (header.sx, header.sz);
        if (!columns.TryGetValue(key, out Column? column))
        {
            column = new Column();
            columns[key] = column;
        }
        if (column.Sections.Remove(header.sy))
        {
            count--;
        }
        Section? section = Build(header, vertices);
        if (section != null)
        {
            column.Sections[header.sy] = section;
            count++;
        }
        if (!column.Queued)
        {
            column.Queued = true;
            dirty.AddLast(key);
        }
    }

    private Section? Build(in Proto.RenSectionHdr header, Proto.RenVertex* vertices)
    {
        int vertexCount = (int)header.vertexCount / 3 * 3;
        if (vertexCount == 0 || !atlas.Usable)
        {
            return null;
        }
        float scale = (float)UltraCraft.Generated.Design.World_Fresh.HostScale;
        float lift = header.sy * 16f * scale; // section origin within its column
        var s = new Section
        {
            Positions = new Vector3[vertexCount],
            Normals = new Vector3[vertexCount],
            Uvs = new Vector2[vertexCount],
            Light = new Vector2[vertexCount],
            Colours = new Color32[vertexCount],
        };
        solid.Clear();
        cutout.Clear();
        blended.Clear();
        for (int i = 0; i < vertexCount; i += 3)
        {
            for (int k = 0; k < 3; k++)
            {
                Proto.RenVertex* v = &vertices[i + k];
                s.Positions[i + k] = new Vector3(v->x * scale, v->y * scale + lift, -v->z * scale); // Minecraft Z is Unity -Z
                s.Uvs[i + k] = new Vector2(v->u, v->v);
                s.Light[i + k] = Atlas.LightUv(v->light);
                uint c = v->color;
                s.Colours[i + k] = new Color32((byte)c, (byte)(c >> 8), (byte)(c >> 16), (byte)(c >> 24));
            }
            // The Z flip mirrors the triangle, so two corners swap to keep it facing outwards.
            Vector3 normal = Vector3.Cross(s.Positions[i + 2] - s.Positions[i], s.Positions[i + 1] - s.Positions[i]).normalized;
            s.Normals[i] = s.Normals[i + 1] = s.Normals[i + 2] = normal;
            // Minecraft marks every non-translucent quad "cutout", so the texture decides: only faces
            // that really have holes (leaves, torches, plants) take the alpha-tested path.
            Proto.RenVertex* t = &vertices[i];
            Surface surface = (t->flags & 2) != 0 ? Surface.Translucent : atlas.Classify(0, t[0].u, t[0].v, t[1].u, t[1].v, t[2].u, t[2].v);
            if (surface == Surface.Empty)
            {
                continue;
            }
            List<int> indices = surface == Surface.Solid ? solid : surface == Surface.Cutout ? cutout : blended;
            indices.Add(i);
            indices.Add(i + 2);
            indices.Add(i + 1);
        }
        if (solid.Count + cutout.Count + blended.Count == 0)
        {
            return null;
        }
        s.Solid = solid.ToArray();
        s.Cutout = cutout.ToArray();
        s.Blended = blended.ToArray();
        return s;
    }

    /// <summary>Rebuilds changed columns, a few per frame, oldest change first.</summary>
    public void Update()
    {
        float start = Time.realtimeSinceStartup;
        int built = 0;
        while (dirty.Count > 0 && built < ColumnsPerFrame && (Time.realtimeSinceStartup - start) * 1000f < RebuildBudgetMs)
        {
            var key = dirty.First!.Value;
            dirty.RemoveFirst();
            if (!columns.TryGetValue(key, out Column? column))
            {
                continue;
            }
            column.Queued = false;
            Rebuild(key, column);
            built++;
        }
    }

    private void Rebuild((int x, int z) key, Column column)
    {
        if (column.Sections.Count == 0)
        {
            Destroy(column);
            columns.Remove(key);
            return;
        }
        positions.Clear();
        normals.Clear();
        uvs.Clear();
        lightUvs.Clear();
        colours.Clear();
        solid.Clear();
        cutout.Clear();
        blended.Clear();
        foreach (Section s in column.Sections.Values)
        {
            int offset = positions.Count;
            positions.AddRange(s.Positions);
            normals.AddRange(s.Normals);
            uvs.AddRange(s.Uvs);
            lightUvs.AddRange(s.Light);
            colours.AddRange(s.Colours);
            foreach (int i in s.Solid) solid.Add(i + offset);
            foreach (int i in s.Cutout) cutout.Add(i + offset);
            foreach (int i in s.Blended) blended.Add(i + offset);
        }
        if (column.Mesh == null)
        {
            column.Mesh = new Mesh { hideFlags = HideFlags.HideAndDontSave };
        }
        Mesh mesh = column.Mesh;
        mesh.Clear();
        mesh.indexFormat = positions.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
        mesh.SetVertices(positions);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetUVs(1, lightUvs);
        mesh.SetColors(colours);
        materials.Clear();
        mesh.subMeshCount = (solid.Count > 0 ? 1 : 0) + (cutout.Count > 0 ? 1 : 0) + (blended.Count > 0 ? 1 : 0);
        int sub = 0;
        if (solid.Count > 0)
        {
            mesh.SetTriangles(solid, sub++, calculateBounds: false);
            materials.Add(atlas.Material(0, Surface.Solid, twoSided: false)!);
        }
        if (cutout.Count > 0)
        {
            mesh.SetTriangles(cutout, sub++, calculateBounds: false);
            // Culled like Minecraft's terrain: plants, flowers and leaf litter arrive as back-to-back quad
            // pairs, and drawing both sides of each made the pair z-fight (flicker) as the camera moved.
            materials.Add(atlas.Material(0, Surface.Cutout, twoSided: false)!);
        }
        if (blended.Count > 0)
        {
            mesh.SetTriangles(blended, sub, calculateBounds: false);
            materials.Add(atlas.Material(0, Surface.Translucent, twoSided: false)!);
        }
        mesh.RecalculateBounds();

        if (column.Object == null)
        {
            if (root == null)
            {
                root = new GameObject("UltraCraft blocks") { hideFlags = HideFlags.DontSave };
                Object.DontDestroyOnLoad(root);
            }
            column.Object = new GameObject($"chunk {key.x} {key.z}");
            column.Object.transform.SetParent(root.transform, worldPositionStays: false);
            column.Object.transform.position = Coords.ToUnity(key.x * 16.0, 0, key.z * 16.0);
            column.Object.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = column.Object.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }
        column.Object.GetComponent<MeshRenderer>().sharedMaterials = materials.ToArray();
    }
}
