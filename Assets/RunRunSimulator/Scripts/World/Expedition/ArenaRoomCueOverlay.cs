using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
namespace MoriMonchiSimulator
{

public class ArenaRoomCueOverlay : MonoBehaviour
{
    [Required, SerializeField] private ArenaSandbox sandbox;
    [Required, SerializeField] private Material cueMaterial;
    [Required, SerializeField] private Material additiveMaterial;
    [Required, SerializeField] private CueStyleSO style;

    [SerializeField] private bool showMinerals = true;
    [SerializeField] private bool showExits = true;
    [SerializeField] private bool showBlackboards = true;

    private class MineralAnim
    {
        public float Alpha;
    }

    private readonly Dictionary<MaterialPickup, MineralAnim> mineralAnims = new();
    private readonly List<Perceivable> mineralQueryBuffer = new();
    private readonly Dictionary<Perceivable, MaterialPickup> mineralLookup = new();

    private void OnEnable()
    {
        CueDrawer.Configure(cueMaterial, additiveMaterial);
    }

    private void LateUpdate()
    {
        if (sandbox == null || style == null) return;

        if (showMinerals) DrawMinerals();
        if (showExits) DrawExits();
        if (showBlackboards) DrawBlackboards();
    }

    private void DrawMinerals()
    {
        PerceivableRegistry.QueryInRadius(sandbox.transform.position, 200f, null, mineralQueryBuffer);

        foreach (var p in mineralQueryBuffer)
        {
            if (p == null || p.Kind != PerceivableKind.Material) continue;

            var m = GetMineralPickup(p);
            if (m == null) continue;

            var anim = GetMineralAnim(m);
            float target = m.Taken ? 0f : 1f;
            anim.Alpha = style.AppearSeconds > 0f ? Mathf.MoveTowards(anim.Alpha, target, Time.deltaTime / style.AppearSeconds) : target;
            float alpha = anim.Alpha;
            if (alpha <= 0.01f) continue;

            Vector3 center = m.transform.position + Vector3.up * style.HeightOffset;
            float radiusScale = m.Value > 0 ? (float)m.Remaining / m.Value : 1f;
            float radius = style.MineralDiscRadius * (m.Value > 1 ? 1.6f : 1f) * Mathf.Lerp(0.5f, 1f, radiusScale);

            CueDrawer.Disc(center, radius, style.MineralColor, style.MineralInnerAlpha * alpha, style.MineralOuterAlpha * alpha);

            Color ringColor = style.MineralColor;
            ringColor.a = style.MineralRingAlpha * alpha;
            CueDrawer.Ring(center, radius, style.MineralRingThickness, ringColor);

            if (m.Value > 1)
                CueDrawer.DashedRing(center, radius, style.MineralRingThickness, style.RingDashCount, style.RingDashRatio, Time.time * -style.RingSpinSpeed, ringColor);
        }
    }

    private void DrawExits()
    {
        if (sandbox.Exits == null) return;

        foreach (var exit in sandbox.Exits)
        {
            if (exit == null) continue;

            Vector3 center = exit.transform.position + Vector3.up * style.HeightOffset;
            Color color = exit.Team == ExpeditionTeam.Player ? style.FriendColor : style.FoeColor;

            CueDrawer.Disc(center, exit.Radius, color, style.ExitAlpha, 0f);

            Color ringColor = color;
            ringColor.a = style.ExitAlpha * 2f;
            CueDrawer.Ring(center, exit.Radius, style.ExitRingThickness, ringColor);

            Color dashColor = color;
            dashColor.a = style.ExitAlpha;
            CueDrawer.DashedRing(center, exit.Radius, style.ExitRingThickness, style.RingDashCount, style.RingDashRatio, Time.time * style.RingSpinSpeed * 0.5f, dashColor);
        }
    }

    private void DrawBlackboards()
    {
        foreach (var team in new[] { ExpeditionTeam.Player, ExpeditionTeam.Rival })
        {
            var board = sandbox.BoardFor(team);
            if (board == null) continue;

            Color color = team == ExpeditionTeam.Player ? style.FriendColor : style.FoeColor;

            foreach (var k in board.KnownVeins)
            {
                if (k.Vein == null || k.Vein.Taken || !k.Vein.gameObject.activeInHierarchy) continue;

                Vector3 center = k.Vein.transform.position + Vector3.up * style.HeightOffset;
                float radiusScale = k.Vein.Value > 0 ? (float)k.Vein.Remaining / k.Vein.Value : 1f;
                float radius = style.MineralDiscRadius * (k.Vein.Value > 1 ? 1.6f : 1f) * Mathf.Lerp(0.5f, 1f, radiusScale)
                    + style.KnownVeinRingOffset + (team == ExpeditionTeam.Rival ? 0.12f : 0f);

                Color ringColor = color;
                ringColor.a = style.KnownVeinRingAlpha;
                CueDrawer.DashedRing(center, radius, style.KnownVeinRingThickness, style.RingDashCount, style.RingDashRatio, Time.time * style.RingSpinSpeed * (team == ExpeditionTeam.Player ? 1f : -1f), ringColor);
            }

            board.PrunePings(Time.time);
            foreach (var ping in board.Pings)
            {
                float t = style.PingSeconds > 0f ? (Time.time - ping.Time) / style.PingSeconds : 1f;
                if (t >= 1f) continue;

                float radius = Mathf.Lerp(0.4f, style.PingRadius, t);
                Color pingColor = color;
                pingColor.a = style.PingAlpha * (1f - t);
                CueDrawer.Ring(ping.Position + Vector3.up * style.HeightOffset, radius, style.PingThickness, pingColor);
            }
        }
    }

    private MineralAnim GetMineralAnim(MaterialPickup mineral)
    {
        if (mineralAnims.TryGetValue(mineral, out var anim)) return anim;
        anim = new MineralAnim();
        mineralAnims[mineral] = anim;
        return anim;
    }

    private MaterialPickup GetMineralPickup(Perceivable p)
    {
        if (mineralLookup.TryGetValue(p, out var pickup)) return pickup;
        pickup = p.GetComponent<MaterialPickup>();
        mineralLookup[p] = pickup;
        return pickup;
    }
}
}
