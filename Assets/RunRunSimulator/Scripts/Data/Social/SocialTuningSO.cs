using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
namespace MoriMonchiSimulator
{

[CreateAssetMenu(fileName = "SocialTuning", menuName = "RunRunSimulator/Social/Social Tuning")]
public class SocialTuningSO : SerializedScriptableObject
{
    public static SocialTuningSO Current { get; private set; }

    private void OnEnable()
    {
        Current = this;
    }

    [Title("Percepción")]
    [Tooltip("Segundos mínimos entre escaneos sociales de un agente.")]
    public float ScanIntervalMin = 2f;

    [Tooltip("Segundos máximos entre escaneos sociales de un agente.")]
    public float ScanIntervalMax = 4f;

    [Tooltip("Radio en el que un agente puede percibir a otros MoriMonchis.")]
    public float PerceptionRadius = 6f;

    [Tooltip("Cantidad máxima de percepts que un agente procesa por escaneo.")]
    public int MaxPercepts = 8;

    [Title("Afinidad (semilla estática)")]
    [Tooltip("Bonus de afinidad si ambos comparten el mismo Element.")]
    public float SameElementBonus = 0.35f;

    [Tooltip("Bonus de afinidad si hay parentesco directo (padre/madre/hermanos).")]
    public float KinshipBonus = 0.4f;

    [Tooltip("Amplitud de la 'química de par' determinista (hash de ambos UniqueID).")]
    public float PairChemistrySpread = 0.25f;

    [Tooltip("Sesgo social por Role del que percibe. Si falta una key, se usa 0.")]
    [OdinSerialize]
    [DictionaryDrawerSettings(KeyLabel = "Role", ValueLabel = "Bias")]
    public Dictionary<Role, float> RoleSocialBias = new Dictionary<Role, float>
    {
        { Role.Protector, 0f },
        { Role.Agresivo, -0.15f },
        { Role.Empatico, 0.25f },
    };

    public float GetRoleBias(Role r) =>
        RoleSocialBias != null && RoleSocialBias.TryGetValue(r, out var bias) ? bias : 0f;

    [Title("Economía social")]
    [Tooltip("Cuánto afecto (Needs) otorga una interacción social positiva.")]
    public float SocialAffectBoost = 8f;

    [Tooltip("Energía consumida por segundo mientras se persigue en un juego social.")]
    public float ChaseEnergyPerSecond = 0.6f;

    [Tooltip("Energía mínima requerida para iniciar un juego social.")]
    public float MinEnergyToPlay = 30f;

    [Title("Persecución de juego")]
    [Tooltip("Duración total en segundos de una persecución de juego.")]
    public float ChaseDuration = 8f;

    [Tooltip("Fracción de la persecución tras la cual se intercambian los roles (perseguidor/perseguido).")]
    [Range(0f, 1f)]
    public float ChaseSwapFraction = 0.5f;

    [Tooltip("Segundos de enfriamiento antes de poder iniciar otro juego social con el mismo par.")]
    public float SocialCooldown = 25f;

    [Tooltip("Distancia del paso de huida del que es perseguido en el juego.")]
    public float ChaseFleeStep = 4f;

    [Tooltip("Cada cuántos segundos se recalcula el camino durante la persecución.")]
    public float ChaseRepath = 0.25f;

    [Title("Acercamiento")]
    [Tooltip("Duración en segundos del acercamiento social antes del juego.")]
    public float ApproachDuration = 4f;

    [Tooltip("Distancia de detención al acercarse a otro MoriMochi.")]
    public float ApproachStopDistance = 1.2f;

    [Title("Evitación")]
    [Tooltip("Distancia mínima que un agente intenta mantener al evitar a otro.")]
    public float AvoidClearance = 2.5f;

    [Title("Dormir juntos")]
    [Tooltip("Energía máxima de AMBOS para que acepten dormir juntos.")]
    public float MaxEnergyToSleep = 45f;

    [Tooltip("Duración total en segundos de la siesta compartida.")]
    public float SleepDuration = 12f;

    [Tooltip("Energía recuperada por segundo mientras duermen.")]
    public float SleepEnergyPerSecond = 4f;

    [Tooltip("Afecto otorgado a ambos al completar la siesta.")]
    public float SleepAffectBoost = 5f;

    [Tooltip("Distancia al punto de dormir a la que se detienen.")]
    public float SleepStopDistance = 1f;

    [Title("Pelea de gremlins")]
    [Tooltip("Duración total en segundos de la pelea de juego.")]
    public float FightDuration = 6f;

    [Tooltip("Afecto perdido por ambos al terminar la pelea.")]
    public float FightAffectLoss = 4f;

    [Tooltip("Cada cuántos segundos un peleador se abalanza sobre el otro.")]
    public float FightLungeInterval = 0.8f;

    [Tooltip("Distancia a la que frena cada abalanzada.")]
    public float FightStopDistance = 1f;

    [Tooltip("Fuerza del empujón final con el que se separan.")]
    public float FightKnockForce = 5f;

    [Title("Historia (SocialGraph)")]
    [Tooltip("Afinidad ganada por el par al completar un juego de persecución.")]
    public float PlayAffinityGain = 0.06f;

    [Tooltip("Afinidad ganada por el par al dormir juntos.")]
    public float SleepAffinityGain = 0.08f;

    [Tooltip("Afinidad perdida por el par al pelear.")]
    public float FightAffinityLoss = 0.1f;

    [Tooltip("Tope absoluto del delta de historia acumulado por par.")]
    public float HistoryDeltaClamp = 0.5f;

    [Title("Pestaña Relaciones")]
    [Tooltip("Afinidad mínima para que otro MoriMochi aparezca en 'Le caen bien'.")]
    public float RelationsFriendThreshold = 0.25f;

    [Tooltip("Afinidad máxima para que otro MoriMochi aparezca en 'Le caen mal'.")]
    public float RelationsFoeThreshold = 0.05f;

    [Title("Diales genéticos (V3)")]
    [Tooltip("Cuánto desplaza la Sociabilidad los umbrales de afinidad de Approach/PlayChase/SleepTogether. Sociable alto = umbral más bajo (interactúa más). 0 = diales sin efecto en umbrales.")]
    public float SociabilityAffinityShift = 0.15f;

    [Tooltip("Cuánto escala la Sociabilidad el cooldown social. Sociable alto = menos espera entre interacciones. 0 = cooldown plano para todos.")]
    [Range(0f, 1f)]
    public float SociabilityCooldownScale = 0.4f;

    [Tooltip("Cuánto desplaza la Osadía el umbral de la pelea de gremlins. Osado alto = pelea con menos motivo. 0 = sin efecto.")]
    public float BoldnessFightShift = 0.15f;

    [Tooltip("Cuánto desplaza la Osadía el umbral de evitación. Osado alto = evita menos; tímido = evita más. 0 = sin efecto.")]
    public float BoldnessAvoidShift = 0.15f;

    public static float DialShift(float dial, float shift) => (Mathf.Clamp01(dial) - 0.5f) * 2f * shift;

    public float ScaledSocialCooldown(float sociability) =>
        SocialCooldown * Mathf.Lerp(1f + SociabilityCooldownScale, 1f - SociabilityCooldownScale, Mathf.Clamp01(sociability));
}
}
