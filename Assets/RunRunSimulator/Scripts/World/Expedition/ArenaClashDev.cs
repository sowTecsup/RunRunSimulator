using Sirenix.OdinInspector;
using UnityEngine;
namespace MoriMonchiSimulator
{

public class ArenaClashDev : MonoBehaviour
{
    [Required, SerializeField] private ArenaSandbox sandbox;
    [Required, SerializeField] private ClashTuningSO tuning;
    [SerializeField, Min(0)] private int attackerIndex = 0;

    [ShowInInspector, ReadOnly]
    private string Attacker
    {
        get
        {
            if (!Application.isPlaying || sandbox == null) return "—";
            if (attackerIndex < 0 || attackerIndex >= sandbox.Spawned.Count) return "—";

            var agent = sandbox.Spawned[attackerIndex]?.Agent;
            if (agent == null) return "—";

            return agent.DNA != null ? agent.DNA.CustomName : agent.name;
        }
    }

    [Button("Embestida")] public void Embestida() => Fire(tuning != null ? tuning.Horn : null);
    [Button("Picada")] public void Picada() => Fire(tuning != null ? tuning.Wings : null);
    [Button("Coletazo")] public void Coletazo() => Fire(tuning != null ? tuning.Back : null);
    [Button("Par más cercano: Embestida")] public void ClosestEmbestida() => FireClosestPair(tuning != null ? tuning.Horn : null, 7f);
    [Button("Par más cercano: Picada")] public void ClosestPicada() => FireClosestPair(tuning != null ? tuning.Wings : null, 9f);
    [Button("Par más cercano: Coletazo")] public void ClosestColetazo() => FireClosestPair(tuning != null ? tuning.Back : null, 3.5f);

    public bool Fire(ClashMoveSO move) => Fire(move, attackerIndex);

    public bool FireClosestPair(ClashMoveSO move, float maxDistance)
    {
        if (!Application.isPlaying || move == null || sandbox == null) return false;

        int bestIndex = -1;
        float bestSqrDist = maxDistance * maxDistance;
        var list = sandbox.Spawned;
        for (int i = 0; i < list.Count; i++)
        {
            var a = list[i] != null ? list[i].Agent : null;
            if (a == null || a.IsHeld || a.IsAirborne || a.IsRecovering) continue;
            for (int j = 0; j < list.Count; j++)
            {
                var b = list[j] != null ? list[j].Agent : null;
                if (b == null || i == j || !ExpeditionTeams.AreRivals(a.Team, b.Team)) continue;
                if (b.IsHeld || b.IsAirborne || b.IsRecovering || !b.IsClashTargetable) continue;

                Vector3 delta = b.transform.position - a.transform.position;
                float sqrDist = delta.x * delta.x + delta.z * delta.z;
                if (sqrDist <= bestSqrDist)
                {
                    bestSqrDist = sqrDist;
                    bestIndex = i;
                }
            }
        }

        return bestIndex >= 0 && Fire(move, bestIndex);
    }

    public bool Fire(ClashMoveSO move, int index)
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[ArenaClashDev] Enter Play mode first.");
            return false;
        }

        if (move == null || sandbox == null)
        {
            Debug.LogWarning("[ArenaClashDev] Falta el movimiento o el sandbox.");
            return false;
        }

        if (index < 0 || index >= sandbox.Spawned.Count)
        {
            Debug.LogWarning($"[ArenaClashDev] Índice {index} fuera de rango.");
            return false;
        }

        var attacker = sandbox.Spawned[index]?.Agent;
        if (attacker == null)
        {
            Debug.LogWarning("[ArenaClashDev] El atacante no tiene Agent.");
            return false;
        }

        MoriMochiAgent rival = null;
        float bestSqrDist = float.MaxValue;

        foreach (var controller in sandbox.Spawned)
        {
            var other = controller != null ? controller.Agent : null;
            if (other == null || other == attacker) continue;
            if (!ExpeditionTeams.AreRivals(attacker.Team, other.Team)) continue;
            if (other.IsHeld || other.IsAirborne || other.IsRecovering) continue;

            Vector3 delta = other.transform.position - attacker.transform.position;
            float sqrDist = delta.x * delta.x + delta.z * delta.z;
            if (sqrDist < bestSqrDist)
            {
                bestSqrDist = sqrDist;
                rival = other;
            }
        }

        if (rival == null)
        {
            Debug.LogWarning("[ArenaClashDev] No hay rival disponible.");
            return false;
        }

        string attackerName = attacker.DNA != null ? attacker.DNA.CustomName : attacker.name;
        string rivalName = rival.DNA != null ? rival.DNA.CustomName : rival.name;

        bool ok = attacker.ForceClash(move, rival);
        Debug.Log($"[ArenaClashDev] {attackerName} → {move.Summary()} contra {rivalName}: {(ok ? "ok" : "rechazado")}");
        return ok;
    }
}
}
