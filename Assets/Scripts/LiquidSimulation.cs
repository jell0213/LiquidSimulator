using System.Collections.Generic;
using UnityEngine;

namespace LiquidSimulator
{
    // Position based double-density relaxation: particles carry mass, not individual rigid bodies.
    public sealed class LiquidSimulation : MonoBehaviour
    {
        public const float HalfSize = 2.5f;
        public const float Radius = 0.075f;
        const float Support = 0.31f;
        const int Columns = 29, Rows = 19;
        public float Viscosity { get; set; } = 0.25f;
        public Vector2[] Positions { get; private set; }
        public Vector2 ContainerPosition { get; private set; }
        public float ContainerAngle { get; private set; }
        public bool Paused { get; set; }
        public float FillFraction => (float)(Columns * Rows) / (29 * 29);
        Vector2[] velocities, previous;
        readonly Dictionary<Vector2Int, List<int>> grid = new();
        readonly Stack<List<int>> pool = new();
        readonly List<int> neighbors = new(64);
        Vector2 target, oldCenter, wallVelocity;
        float targetAngle, oldAngle, angularVelocity, accumulator;
        public void SetTarget(Vector2 position, float angle) { target = position; targetAngle = angle; }
        public void ResetLiquid()
        {
            Positions = new Vector2[Columns * Rows];
            velocities = new Vector2[Positions.Length];
            previous = new Vector2[Positions.Length];
            for (int y = 0; y < Rows; y++)
                for (int x = 0; x < Columns; x++)
                    Positions[y * Columns + x] = ToWorld(new Vector2((x - 14) * 0.155f, -2.25f + y * 0.155f));
            accumulator = 0;
        }
        void Awake() => ResetLiquid();
        public Vector2 ToLocal(Vector2 p) => Rotate(p - ContainerPosition, -ContainerAngle);
        public Vector2 ToWorld(Vector2 p) => Rotate(p, ContainerAngle) + ContainerPosition;
        static Vector2 Rotate(Vector2 v, float degrees)
        {
            float a = degrees * Mathf.Deg2Rad, c = Mathf.Cos(a), s = Mathf.Sin(a);
            return new Vector2(c * v.x - s * v.y, s * v.x + c * v.y);
        }
        void Update()
        {
            if (Paused) return;
            accumulator = Mathf.Min(accumulator + Time.deltaTime, 0.05f);
            const float dt = 1f / 120f;
            while (accumulator >= dt) { Step(dt); accumulator -= dt; }
        }
        void BuildGrid()
        {
            foreach (var bucket in grid.Values) { bucket.Clear(); pool.Push(bucket); }
            grid.Clear();
            for (int i = 0; i < Positions.Length; i++)
            {
                Vector2Int cell = Cell(Positions[i]);
                if (!grid.TryGetValue(cell, out var list)) grid[cell] = list = pool.Count > 0 ? pool.Pop() : new List<int>(16);
                list.Add(i);
            }
        }
        static Vector2Int Cell(Vector2 p) => new(Mathf.FloorToInt(p.x / Support), Mathf.FloorToInt(p.y / Support));
        void FindNeighbors(int i)
        {
            neighbors.Clear();
            Vector2Int cell = Cell(Positions[i]);
            for (int y = -1; y <= 1; y++) for (int x = -1; x <= 1; x++)
                if (grid.TryGetValue(cell + new Vector2Int(x, y), out var list))
                    foreach (int j in list)
                        if (j != i && (Positions[j] - Positions[i]).sqrMagnitude < Support * Support) neighbors.Add(j);
        }
        void Step(float dt)
        {
            oldCenter = ContainerPosition; oldAngle = ContainerAngle;
            ContainerPosition = Vector2.MoveTowards(ContainerPosition, target, 9f * dt);
            ContainerAngle = Mathf.MoveTowardsAngle(ContainerAngle, targetAngle, 150f * dt);
            wallVelocity = (ContainerPosition - oldCenter) / dt;
            angularVelocity = Mathf.DeltaAngle(oldAngle, ContainerAngle) * Mathf.Deg2Rad / dt;
            BuildGrid();
            // Pairwise velocity diffusion conserves linear momentum; bounded impulses avoid overshoot.
            for (int i = 0; i < Positions.Length; i++)
            {
                FindNeighbors(i);
                foreach (int j in neighbors)
                {
                    if (j <= i) continue;
                    float q = 1f - Vector2.Distance(Positions[i], Positions[j]) / Support;
                    Vector2 impulse = (velocities[j] - velocities[i]) * Mathf.Min(0.12f, Viscosity * q * dt * 12f);
                    velocities[i] += impulse; velocities[j] -= impulse;
                }
            }
            for (int i = 0; i < Positions.Length; i++)
            {
                previous[i] = Positions[i];
                velocities[i] += Vector2.down * (9.81f * dt);
                Positions[i] += velocities[i] * dt;
            }
            for (int iteration = 0; iteration < 3; iteration++)
            {
                BuildGrid();
                for (int i = 0; i < Positions.Length; i++)
                {
                    FindNeighbors(i);
                    float density = 0, nearDensity = 0;
                    foreach (int j in neighbors)
                    {
                        float q = 1f - Vector2.Distance(Positions[j], Positions[i]) / Support;
                        density += q * q; nearDensity += q * q * q;
                    }
                    float pressure = Mathf.Max(-0.3f, (density - 2.8f) * 0.018f);
                    float nearPressure = nearDensity * 0.045f;
                    Vector2 correction = Vector2.zero;
                    foreach (int j in neighbors)
                    {
                        Vector2 delta = Positions[j] - Positions[i]; float distance = delta.magnitude;
                        if (distance < 0.00001f || distance >= Support) continue;
                        float q = 1f - distance / Support;
                        Vector2 move = delta / distance * Mathf.Clamp((pressure * q + nearPressure * q * q) * 0.5f, -0.005f, 0.025f);
                        Positions[j] += move; correction -= move;
                    }
                    Positions[i] += correction;
                }
                for (int i = 0; i < Positions.Length; i++) Constrain(i, false);
            }
            for (int i = 0; i < Positions.Length; i++)
            {
                velocities[i] = Vector2.ClampMagnitude((Positions[i] - previous[i]) / dt, 18f);
                Constrain(i, true);
            }
        }
        void Constrain(int i, bool velocityOnly)
        {
            float limit = HalfSize - Radius;
            Vector2 p = ToLocal(Positions[i]);
            Vector2 clamped = new(Mathf.Clamp(p.x, -limit, limit), Mathf.Clamp(p.y, -limit, limit));
            if (!velocityOnly) { Positions[i] = ToWorld(clamped); return; }
            Vector2 offset = Positions[i] - ContainerPosition;
            Vector2 wall = wallVelocity + new Vector2(-offset.y, offset.x) * angularVelocity;
            Vector2 relative = Rotate(velocities[i] - wall, -ContainerAngle);
            if (Mathf.Abs(p.x) >= limit - 0.001f && relative.x * p.x > 0) relative.x *= -0.04f;
            if (Mathf.Abs(p.y) >= limit - 0.001f && relative.y * p.y > 0) relative.y *= -0.04f;
            // Tangential drag lets viscous fluid adhere to moving walls.
            if (Mathf.Max(Mathf.Abs(p.x), Mathf.Abs(p.y)) >= limit - 0.015f)
                relative *= 1f - Viscosity * 0.08f;
            velocities[i] = wall + Rotate(relative, ContainerAngle);
        }
    }
}
