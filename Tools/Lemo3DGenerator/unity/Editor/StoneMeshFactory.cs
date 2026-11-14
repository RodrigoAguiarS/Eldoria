using System.Collections.Generic;
using UnityEngine;

namespace DungeonCrawler.CharacterArt.Editor
{
    /// <summary>
    /// Malhas low-poly facetadas para o cenário (direção de arte: blocos grandes e pesados com chanfros).
    /// As malhas de piso/parede/portão são geradas no ESPAÇO DO CUBO UNITÁRIO (−0,5…0,5) do objeto original,
    /// para substituir o MeshFilter mantendo a mesma escala, BoxCollider, layer e renderer (CameraOcclusionHider).
    /// </summary>
    internal sealed class StoneMeshFactory
    {
        private readonly List<Vector3> verts = new List<Vector3>();
        private readonly List<Vector3> normals = new List<Vector3>();
        private readonly List<Vector2> uvs = new List<Vector2>();
        private readonly List<List<int>> subs = new List<List<int>>();
        private System.Random rng;

        private StoneMeshFactory(int submeshes, int seed)
        {
            for (int i = 0; i < submeshes; i++) subs.Add(new List<int>());
            rng = new System.Random(seed);
        }

        private float R(float a) => ((float)rng.NextDouble() - 0.5f) * 2f * a;

        private void Tri(int sub, Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 n = Vector3.Cross(b - a, c - a).normalized;
            int i = verts.Count;
            verts.Add(a); verts.Add(b); verts.Add(c);
            normals.Add(n); normals.Add(n); normals.Add(n);
            Vector3 ax = new Vector3(Mathf.Abs(n.x), Mathf.Abs(n.y), Mathf.Abs(n.z));
            foreach (Vector3 p in new[] { a, b, c })
                uvs.Add(ax.y >= ax.x && ax.y >= ax.z ? new Vector2(p.x, p.z) * 0.5f : ax.x >= ax.z ? new Vector2(p.z, p.y) * 0.5f : new Vector2(p.x, p.y) * 0.5f);
            subs[sub].Add(i); subs[sub].Add(i + 1); subs[sub].Add(i + 2);
        }

        private void Quad(int sub, Vector3 a, Vector3 b, Vector3 c, Vector3 d) { Tri(sub, a, b, c); Tri(sub, a, c, d); }

        /// <summary>Bloco chanfrado: base (min..max) com topo recuado por 'bevel' e cantos superiores tremidos.</summary>
        private void Block(int sub, Vector3 min, Vector3 max, float bevel, float jitter)
        {
            float b = bevel;
            Vector3 b0 = new Vector3(min.x, min.y, min.z), b1 = new Vector3(max.x, min.y, min.z), b2 = new Vector3(max.x, min.y, max.z), b3 = new Vector3(min.x, min.y, max.z);
            Vector3 t0 = new Vector3(min.x + b + R(jitter), max.y + R(jitter), min.z + b + R(jitter));
            Vector3 t1 = new Vector3(max.x - b + R(jitter), max.y + R(jitter), min.z + b + R(jitter));
            Vector3 t2 = new Vector3(max.x - b + R(jitter), max.y + R(jitter), max.z - b + R(jitter));
            Vector3 t3 = new Vector3(min.x + b + R(jitter), max.y + R(jitter), max.z - b + R(jitter));
            // ordem horária vista de fora (convenção de face frontal da Unity)
            Quad(sub, t0, t3, t2, t1);            // topo
            Quad(sub, t0, t1, b1, b0);            // −z
            Quad(sub, t1, t2, b2, b1);            // +x
            Quad(sub, t2, t3, b3, b2);            // +z
            Quad(sub, t3, t0, b0, b3);            // −x
            Quad(sub, b1, b2, b3, b0);            // base
        }

        private Mesh Build(string name, Vector3 toUnit)
        {
            var m = new Mesh { name = name };
            var v = new List<Vector3>(verts.Count);
            var n = new List<Vector3>(normals.Count);
            for (int i = 0; i < verts.Count; i++)
            {
                v.Add(Vector3.Scale(verts[i], toUnit));
                // normal correta após escala não uniforme do objeto: inversa-transposta (aqui escala inversa)
                n.Add(Vector3.Scale(normals[i], new Vector3(1f / toUnit.x, 1f / toUnit.y, 1f / toUnit.z)).normalized);
            }

            if (v.Count > 65000) m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            m.SetVertices(v);
            m.SetNormals(n);
            m.SetUVs(0, uvs);
            m.subMeshCount = subs.Count;
            for (int s = 0; s < subs.Count; s++) m.SetTriangles(subs[s], s);
            m.RecalculateBounds();
            return m;
        }

        // ———————————————————— peças ————————————————————

        /// <summary>Piso: placas de ~2 m sobre uma laje contínua (sem buracos). Submalhas: 0..2 variações de pedra.</summary>
        public static Mesh Floor(Vector3 size, int seed)
        {
            var f = new StoneMeshFactory(3, seed);
            float h = size.y;
            f.Block(0, new Vector3(-size.x / 2, -h / 2, -size.z / 2), new Vector3(size.x / 2, h / 2 - 0.03f, size.z / 2), 0f, 0f);   // laje
            int nx = Mathf.Max(1, Mathf.RoundToInt(size.x / 2f)), nz = Mathf.Max(1, Mathf.RoundToInt(size.z / 2f));
            float tw = size.x / nx, td = size.z / nz;
            for (int i = 0; i < nx; i++)
                for (int k = 0; k < nz; k++)
                {
                    float x0 = -size.x / 2 + i * tw, z0 = -size.z / 2 + k * td;
                    float top = h / 2 - f.R(0.012f) * 0.5f - 0.006f;
                    int sub = f.rng.Next(3);
                    f.Block(sub, new Vector3(x0 + 0.02f, h / 2 - 0.05f, z0 + 0.02f), new Vector3(x0 + tw - 0.02f, top, z0 + td - 0.02f), 0.05f, 0.008f);
                }

            return f.Build($"GA_Floor_{size.x:0.##}x{size.z:0.##}", new Vector3(1f / size.x, 1f / size.y, 1f / size.z));
        }

        /// <summary>Parede: duas fiadas de blocos desencontrados ao longo do eixo maior + capa. Submalhas: 0,1 pedra; 2 capa.</summary>
        public static Mesh Wall(Vector3 size, int seed)
        {
            var f = new StoneMeshFactory(3, seed);
            bool alongX = size.x >= size.z;
            float len = alongX ? size.x : size.z, th = alongX ? size.z : size.x, hgt = size.y;
            float capH = Mathf.Min(0.16f, hgt * 0.15f), courseH = (hgt - capH) / 2f;
            for (int course = 0; course < 2; course++)
            {
                int n = Mathf.Max(1, Mathf.RoundToInt(len / 2f)) + course;
                float start = -len / 2, step = len / Mathf.Max(1, Mathf.RoundToInt(len / 2f));
                float y0 = -hgt / 2 + course * courseH;
                float cursor = start;
                for (int i = 0; i < n; i++)
                {
                    float w = course == 1 && (i == 0 || i == n - 1) ? step / 2f : step;
                    float a = cursor, b = Mathf.Min(len / 2, cursor + w);
                    cursor = b;
                    if (b - a < 0.05f) continue;
                    Vector3 min = alongX ? new Vector3(a + 0.015f, y0, -th / 2) : new Vector3(-th / 2, y0, a + 0.015f);
                    Vector3 max = alongX ? new Vector3(b - 0.015f, y0 + courseH - 0.015f, th / 2) : new Vector3(th / 2, y0 + courseH - 0.015f, b - 0.015f);
                    f.Block(f.rng.Next(2), min, max, 0.05f, 0.015f);
                }
            }

            Vector3 cmin = alongX ? new Vector3(-len / 2, hgt / 2 - capH, -th / 2 - 0.05f) : new Vector3(-th / 2 - 0.05f, hgt / 2 - capH, -len / 2);
            Vector3 cmax = alongX ? new Vector3(len / 2, hgt / 2, th / 2 + 0.05f) : new Vector3(th / 2 + 0.05f, hgt / 2, len / 2);
            f.Block(2, cmin, cmax, 0.04f, 0.01f);
            return f.Build($"GA_Wall_{size.x:0.##}x{size.y:0.##}x{size.z:0.##}", new Vector3(1f / size.x, 1f / size.y, 1f / size.z));
        }

        /// <summary>Portão de ferro (grade levadiça) para o Blocker das portas. Submalhas: 0 ferro, 1 ferrugem.</summary>
        public static Mesh Gate(Vector3 size, int seed)
        {
            var f = new StoneMeshFactory(2, seed);
            float w = size.x, h = size.y, d = Mathf.Min(size.z, 0.3f);
            f.Block(0, new Vector3(-w / 2, -h / 2, -d / 2), new Vector3(-w / 2 + 0.18f, h / 2, d / 2), 0.02f, 0f);
            f.Block(0, new Vector3(w / 2 - 0.18f, -h / 2, -d / 2), new Vector3(w / 2, h / 2, d / 2), 0.02f, 0f);
            f.Block(0, new Vector3(-w / 2, h / 2 - 0.16f, -d / 2), new Vector3(w / 2, h / 2, d / 2), 0.02f, 0f);
            int bars = Mathf.Max(3, Mathf.RoundToInt(w / 0.45f));
            for (int i = 1; i < bars; i++)
            {
                float x = -w / 2 + i * w / bars;
                f.Block(0, new Vector3(x - 0.04f, -h / 2, -0.04f), new Vector3(x + 0.04f, h / 2 - 0.16f, 0.04f), 0.01f, 0f);
                f.Block(1, new Vector3(x - 0.06f, -h / 2, -0.06f), new Vector3(x + 0.06f, -h / 2 + 0.08f, 0.06f), 0.02f, 0f);   // pontas enferrujadas
            }

            foreach (float y in new[] { -h / 2 + h * 0.3f, -h / 2 + h * 0.65f })
                f.Block(1, new Vector3(-w / 2 + 0.18f, y, -0.05f), new Vector3(w / 2 - 0.18f, y + 0.07f, 0.05f), 0.01f, 0f);
            return f.Build($"GA_Gate_{size.x:0.##}x{size.y:0.##}", new Vector3(1f / size.x, 1f / size.y, 1f / size.z));
        }

        /// <summary>Tocha de parede (espaço real, metros; frente = +z). Submalhas: 0 ferro, 1 chama.</summary>
        public static Mesh Torch()
        {
            var f = new StoneMeshFactory(2, 11);
            f.Block(0, new Vector3(-0.08f, -0.25f, 0f), new Vector3(0.08f, 0.1f, 0.1f), 0.02f, 0f);
            f.Block(0, new Vector3(-0.04f, 0.0f, 0.1f), new Vector3(0.04f, 0.06f, 0.32f), 0.01f, 0f);
            f.Block(0, new Vector3(-0.12f, 0.06f, 0.24f), new Vector3(0.12f, 0.18f, 0.44f), 0.04f, 0f);
            // chama: pirâmide facetada
            Vector3 c = new Vector3(0f, 0.18f, 0.34f), top = new Vector3(0.01f, 0.58f, 0.35f);
            Vector3[] ring = { c + new Vector3(-0.1f, 0, -0.08f), c + new Vector3(0.1f, 0, -0.08f), c + new Vector3(0.12f, 0, 0.06f), c + new Vector3(0, 0, 0.12f), c + new Vector3(-0.12f, 0, 0.06f) };
            for (int i = 0; i < ring.Length; i++) f.Tri(1, ring[i], top, ring[(i + 1) % ring.Length]);
            return f.Build("GA_Torch", Vector3.one);
        }

        /// <summary>Runa no piso (anel facetado + traços), muito fina. Submalha única (emissiva).</summary>
        public static Mesh Rune()
        {
            var f = new StoneMeshFactory(1, 21);
            const int seg = 10; const float r0 = 0.42f, r1 = 0.5f, y = 0.012f;
            for (int i = 0; i < seg; i++)
            {
                float a = i * Mathf.PI * 2 / seg, b = (i + 1) * Mathf.PI * 2 / seg;
                Vector3 p0 = new Vector3(Mathf.Cos(a) * r0, y, Mathf.Sin(a) * r0), p1 = new Vector3(Mathf.Cos(a) * r1, y, Mathf.Sin(a) * r1);
                Vector3 q0 = new Vector3(Mathf.Cos(b) * r0, y, Mathf.Sin(b) * r0), q1 = new Vector3(Mathf.Cos(b) * r1, y, Mathf.Sin(b) * r1);
                f.Quad(0, p0, q0, q1, p1);
            }

            for (int k = 0; k < 3; k++)
            {
                float a = k * Mathf.PI / 3 + 0.2f; Vector3 d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 0.36f, s = new Vector3(-Mathf.Sin(a), 0, Mathf.Cos(a)) * 0.035f;
                f.Quad(0, -d - s + Vector3.up * y, -d + s + Vector3.up * y, d + s + Vector3.up * y, d - s + Vector3.up * y);
            }

            return f.Build("GA_Rune", Vector3.one);
        }

        /// <summary>Mancha de musgo irregular, rente ao chão.</summary>
        public static Mesh Moss(int seed)
        {
            var f = new StoneMeshFactory(1, seed);
            const int seg = 7; Vector3 c = new Vector3(0, 0.008f, 0);
            var pts = new Vector3[seg];
            for (int i = 0; i < seg; i++) { float a = i * Mathf.PI * 2 / seg; float r = 0.45f + f.R(0.2f); pts[i] = new Vector3(Mathf.Cos(a) * r, 0.008f, Mathf.Sin(a) * r * 0.75f); }
            for (int i = 0; i < seg; i++) f.Tri(0, c, pts[(i + 1) % seg], pts[i]);
            return f.Build($"GA_Moss_{seed}", Vector3.one);
        }
    }
}
