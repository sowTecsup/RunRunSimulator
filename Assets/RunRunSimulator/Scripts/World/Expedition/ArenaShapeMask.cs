using System.Collections.Generic;
using UnityEngine;

namespace MoriMonchiSimulator
{
    public static class ArenaShapeMask
    {
        public struct BlobParams
        {
            public float CenterRadius;
            public int Count;
            public Vector2 Radius;
            public float Spread;
            public float Overlap;
        }

        public static Vector2 CellCenter(int i, int j, float cell, Vector2 origin)
        {
            return origin + new Vector2((i + 0.5f) * cell, (j + 0.5f) * cell);
        }

        public static bool Get(byte[] mask, int size, int i, int j)
        {
            if (i < 0 || i >= size || j < 0 || j >= size)
            {
                return false;
            }

            return mask[j * size + i] != 0;
        }

        public static void Clear(byte[] mask)
        {
            for (int k = 0; k < mask.Length; k++)
            {
                mask[k] = 0;
            }
        }

        public static void Paint(byte[] mask, int size, float cell, Vector2 origin, Vector2 center, float radius, bool value)
        {
            byte v = value ? (byte)1 : (byte)0;
            float radiusSq = radius * radius;

            int iMin = Mathf.Max(0, Mathf.FloorToInt((center.x - radius - origin.x) / cell - 0.5f));
            int iMax = Mathf.Min(size - 1, Mathf.CeilToInt((center.x + radius - origin.x) / cell - 0.5f));
            int jMin = Mathf.Max(0, Mathf.FloorToInt((center.y - radius - origin.y) / cell - 0.5f));
            int jMax = Mathf.Min(size - 1, Mathf.CeilToInt((center.y + radius - origin.y) / cell - 0.5f));

            for (int j = jMin; j <= jMax; j++)
            {
                for (int i = iMin; i <= iMax; i++)
                {
                    Vector2 c = CellCenter(i, j, cell, origin);
                    if ((c - center).sqrMagnitude <= radiusSq)
                    {
                        mask[j * size + i] = v;
                    }
                }
            }
        }

        public static void Symmetrize(byte[] mask, int size)
        {
            byte[] copy = (byte[])mask.Clone();

            for (int j = 0; j < size; j++)
            {
                for (int i = 0; i < size; i++)
                {
                    int idx = j * size + i;
                    int oppIdx = (size - 1 - j) * size + (size - 1 - i);
                    if (copy[oppIdx] != 0)
                    {
                        mask[idx] = 1;
                    }
                }
            }
        }

        public static void Blobs(byte[] mask, int size, float cell, Vector2 origin, System.Random rng, BlobParams p)
        {
            Clear(mask);

            Vector2 c = origin + new Vector2(size * cell * 0.5f, size * cell * 0.5f);
            Paint(mask, size, cell, origin, c, p.CenterRadius, true);

            Vector2 prevCenter = c;
            float prevRadius = p.CenterRadius;

            for (int n = 0; n < p.Count; n++)
            {
                float r = Mathf.Lerp(p.Radius.x, p.Radius.y, (float)rng.NextDouble());
                float angle = (float)(rng.NextDouble() * Mathf.PI * 2.0);
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 newCenter = prevCenter + dir * ((prevRadius + r) * p.Overlap);

                if ((newCenter - c).magnitude > p.Spread)
                {
                    newCenter = c + (newCenter - c).normalized * p.Spread;
                }

                Paint(mask, size, cell, origin, newCenter, r, true);
                prevCenter = newCenter;
                prevRadius = r;
            }
        }

        public static List<List<Vector2>> Contours(byte[] mask, int size, float cell, Vector2 origin)
        {
            var segments = new List<(Vector2 a, Vector2 b)>();

            for (int j = -1; j < size; j++)
            {
                for (int i = -1; i < size; i++)
                {
                    bool bl = Get(mask, size, i, j);
                    bool br = Get(mask, size, i + 1, j);
                    bool tr = Get(mask, size, i + 1, j + 1);
                    bool tl = Get(mask, size, i, j + 1);

                    int caseIndex = (bl ? 1 : 0) | (br ? 2 : 0) | (tr ? 4 : 0) | (tl ? 8 : 0);
                    if (caseIndex == 0 || caseIndex == 15)
                    {
                        continue;
                    }

                    Vector2 pBl = CellCenter(i, j, cell, origin);
                    Vector2 pBr = CellCenter(i + 1, j, cell, origin);
                    Vector2 pTr = CellCenter(i + 1, j + 1, cell, origin);
                    Vector2 pTl = CellCenter(i, j + 1, cell, origin);

                    Vector2 bottomMid = (pBl + pBr) * 0.5f;
                    Vector2 rightMid = (pBr + pTr) * 0.5f;
                    Vector2 topMid = (pTr + pTl) * 0.5f;
                    Vector2 leftMid = (pTl + pBl) * 0.5f;

                    switch (caseIndex)
                    {
                        case 1:
                            segments.Add((bottomMid, leftMid));
                            break;
                        case 2:
                            segments.Add((bottomMid, rightMid));
                            break;
                        case 3:
                            segments.Add((leftMid, rightMid));
                            break;
                        case 4:
                            segments.Add((rightMid, topMid));
                            break;
                        case 5:
                            segments.Add((bottomMid, leftMid));
                            segments.Add((rightMid, topMid));
                            break;
                        case 6:
                            segments.Add((bottomMid, topMid));
                            break;
                        case 7:
                            segments.Add((topMid, leftMid));
                            break;
                        case 8:
                            segments.Add((topMid, leftMid));
                            break;
                        case 9:
                            segments.Add((bottomMid, topMid));
                            break;
                        case 10:
                            segments.Add((bottomMid, rightMid));
                            segments.Add((topMid, leftMid));
                            break;
                        case 11:
                            segments.Add((rightMid, topMid));
                            break;
                        case 12:
                            segments.Add((rightMid, leftMid));
                            break;
                        case 13:
                            segments.Add((bottomMid, rightMid));
                            break;
                        case 14:
                            segments.Add((bottomMid, leftMid));
                            break;
                    }
                }
            }

            return LinkSegments(segments, cell);
        }

        private static List<List<Vector2>> LinkSegments(List<(Vector2 a, Vector2 b)> segments, float cell)
        {
            var loops = new List<List<Vector2>>();
            int n = segments.Count;
            if (n == 0)
            {
                return loops;
            }

            float q = cell * 0.001f;
            var keyA = new (long, long)[n];
            var keyB = new (long, long)[n];
            var keyToPoint = new Dictionary<(long, long), Vector2>();
            var keyToSegments = new Dictionary<(long, long), List<int>>();

            for (int s = 0; s < n; s++)
            {
                keyA[s] = ((long)Mathf.Round(segments[s].a.x / q), (long)Mathf.Round(segments[s].a.y / q));
                keyB[s] = ((long)Mathf.Round(segments[s].b.x / q), (long)Mathf.Round(segments[s].b.y / q));

                if (!keyToPoint.ContainsKey(keyA[s]))
                {
                    keyToPoint[keyA[s]] = segments[s].a;
                }

                if (!keyToPoint.ContainsKey(keyB[s]))
                {
                    keyToPoint[keyB[s]] = segments[s].b;
                }

                if (!keyToSegments.TryGetValue(keyA[s], out var listA))
                {
                    listA = new List<int>();
                    keyToSegments[keyA[s]] = listA;
                }
                listA.Add(s);

                if (!keyToSegments.TryGetValue(keyB[s], out var listB))
                {
                    listB = new List<int>();
                    keyToSegments[keyB[s]] = listB;
                }
                listB.Add(s);
            }

            var visited = new bool[n];

            for (int start = 0; start < n; start++)
            {
                if (visited[start])
                {
                    continue;
                }

                var loop = new List<Vector2>();
                var startKey = keyA[start];
                var currentKey = keyB[start];
                loop.Add(keyToPoint[startKey]);
                loop.Add(keyToPoint[currentKey]);
                visited[start] = true;

                while (!currentKey.Equals(startKey))
                {
                    if (!keyToSegments.TryGetValue(currentKey, out var candidates))
                    {
                        break;
                    }

                    int next = -1;
                    foreach (var candidate in candidates)
                    {
                        if (!visited[candidate])
                        {
                            next = candidate;
                            break;
                        }
                    }

                    if (next == -1)
                    {
                        break;
                    }

                    visited[next] = true;
                    currentKey = keyA[next].Equals(currentKey) ? keyB[next] : keyA[next];

                    if (!currentKey.Equals(startKey))
                    {
                        loop.Add(keyToPoint[currentKey]);
                    }
                }

                if (loop.Count >= 3)
                {
                    loops.Add(loop);
                }
            }

            return loops;
        }

        public static List<Vector2> Simplify(IReadOnlyList<Vector2> loop, float tolerance)
        {
            int originalCount = loop.Count;
            if (originalCount < 3)
            {
                return new List<Vector2>(loop);
            }

            float currentTolerance = tolerance;
            List<Vector2> result = SimplifyOnce(loop, currentTolerance);

            if (originalCount < 6)
            {
                return result;
            }

            int attempts = 0;
            while (result.Count < 6 && attempts < 4)
            {
                currentTolerance *= 0.5f;
                result = SimplifyOnce(loop, currentTolerance);
                attempts++;
            }

            if (result.Count < 6)
            {
                return new List<Vector2>(loop);
            }

            return result;
        }

        private static List<Vector2> SimplifyOnce(IReadOnlyList<Vector2> loop, float tolerance)
        {
            int count = loop.Count;
            int idxA = 0;
            int idxB = 0;
            float best = -1f;

            for (int a = 0; a < count; a++)
            {
                for (int b = a + 1; b < count; b++)
                {
                    float d = (loop[a] - loop[b]).sqrMagnitude;
                    if (d > best)
                    {
                        best = d;
                        idxA = a;
                        idxB = b;
                    }
                }
            }

            var chain1 = new List<Vector2>();
            for (int k = idxA; k != idxB; k = (k + 1) % count)
            {
                chain1.Add(loop[k]);
            }
            chain1.Add(loop[idxB]);

            var chain2 = new List<Vector2>();
            for (int k = idxB; k != idxA; k = (k + 1) % count)
            {
                chain2.Add(loop[k]);
            }
            chain2.Add(loop[idxA]);

            var simplified1 = DouglasPeucker(chain1, tolerance);
            var simplified2 = DouglasPeucker(chain2, tolerance);

            var merged = new List<Vector2>(simplified1);
            for (int k = 1; k < simplified2.Count - 1; k++)
            {
                merged.Add(simplified2[k]);
            }

            return merged;
        }

        private static List<Vector2> DouglasPeucker(List<Vector2> points, float tolerance)
        {
            if (points.Count < 3)
            {
                return new List<Vector2>(points);
            }

            float maxDist = 0f;
            int index = 0;
            Vector2 start = points[0];
            Vector2 end = points[points.Count - 1];

            for (int i = 1; i < points.Count - 1; i++)
            {
                float d = PerpendicularDistance(points[i], start, end);
                if (d > maxDist)
                {
                    maxDist = d;
                    index = i;
                }
            }

            if (maxDist <= tolerance)
            {
                return new List<Vector2> { start, end };
            }

            var left = DouglasPeucker(points.GetRange(0, index + 1), tolerance);
            var right = DouglasPeucker(points.GetRange(index, points.Count - index), tolerance);

            var result = new List<Vector2>(left);
            result.RemoveAt(result.Count - 1);
            result.AddRange(right);
            return result;
        }

        private static float PerpendicularDistance(Vector2 point, Vector2 lineStart, Vector2 lineEnd)
        {
            Vector2 line = lineEnd - lineStart;
            float lenSq = line.sqrMagnitude;
            if (lenSq < 1e-8f)
            {
                return (point - lineStart).magnitude;
            }

            float t = Vector2.Dot(point - lineStart, line) / lenSq;
            Vector2 projection = lineStart + line * t;
            return (point - projection).magnitude;
        }

        public static float SignedArea(IReadOnlyList<Vector2> polygon)
        {
            int n = polygon.Count;
            float sum = 0f;

            for (int i = 0; i < n; i++)
            {
                Vector2 a = polygon[i];
                Vector2 b = polygon[(i + 1) % n];
                sum += a.x * b.y - b.x * a.y;
            }

            return sum * 0.5f;
        }

        public static bool Contains(IReadOnlyList<Vector2> polygon, Vector2 point)
        {
            int n = polygon.Count;
            bool inside = false;

            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                Vector2 pi = polygon[i];
                Vector2 pj = polygon[j];
                bool intersects = ((pi.y > point.y) != (pj.y > point.y)) &&
                    (point.x < (pj.x - pi.x) * (point.y - pi.y) / (pj.y - pi.y) + pi.x);

                if (intersects)
                {
                    inside = !inside;
                }
            }

            return inside;
        }
    }
}
