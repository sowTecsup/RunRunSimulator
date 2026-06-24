using UnityEngine;
namespace MoriMonchiSimulator
{

public static class NpcDialogueBank
{
    private static readonly string[] wandering =
    {
        "¿Qué tendrán hoy?",
        "A ver qué me encuentro…",
        "Vengo a darme una vuelta.",
        "Mmm, huele a criaturas nuevas.",
        "¿Habrá algo bonito?",
        "Vamos a chismear los estantes.",
    };

    private static readonly string[] inspecting =
    {
        "Mmm, déjame ver…",
        "¿Será este el indicado?",
        "Qué ojitos tiene…",
        "A ver, a ver…",
        "No sé, no sé…",
        "Interesante…",
    };

    private static readonly string[] approaching =
    {
        "¡Me llevo a {0}!",
        "¡{0} es perfecto!",
        "¡Lo decidí: {0}!",
        "¡A la caja con {0}!",
        "¡{0} se viene conmigo… si me alcanza!",
    };

    private static readonly string[] queueing =
    {
        "Esperaré mi turno…",
        "Uf, qué fila.",
        "Ahí voy, ahí voy.",
        "Paciencia…",
        "¿Falta mucho?",
    };

    private static readonly string[] waiting =
    {
        "¿Hay alguien en la caja?",
        "¿Hola? ¿Atienden?",
        "Tum, tum, tum…",
        "¿Me escuchan?",
        "Aquí espero…",
    };

    private static readonly string[] negotiating =
    {
        "¿Cuánto por {0}?",
        "¿Me hace precio por {0}?",
        "¿Lo deja más barato?",
        "¿Hacemos trato por {0}?",
        "Mmm… ¿es negociable?",
    };

    private static readonly string[] purchased =
    {
        "¡{0} se viene conmigo!",
        "¡Mío, mío! ¡{0}!",
        "¡Qué felicidad, {0}!",
        "¡Gracias! ¡Cuidaré a {0}!",
        "¡{0} y yo seremos felices!",
    };

    private static readonly string[] outbid =
    {
        "¡Me ganaron a {0}!",
        "¡No! ¡Quería a {0}…!",
        "Llegué tarde por {0}.",
        "Ash, se llevaron a {0}.",
        "¡{0} ya tenía dueño! Qué rabia.",
    };

    private static readonly string[] queueFull =
    {
        "¡Está muy lleno!",
        "Uf, cuánta gente.",
        "Mejor vuelvo luego.",
        "No aguanto esta multitud.",
        "¡Ni de chiste hago esa fila!",
    };

    private static readonly string[] noDeal =
    {
        "Será en otra ocasión…",
        "Ni modo.",
        "Quizá la próxima.",
        "No era mi día.",
        "Bah, me voy.",
    };

    private static string One(string[] bank, string targetName) =>
        string.Format(bank[Random.Range(0, bank.Length)], targetName);

    public static string Pick(NpcAgent.NpcState state, NpcAgent.LeaveReason reason, string targetName) =>
        state switch
        {
            NpcAgent.NpcState.Wandering           => One(wandering,   targetName),
            NpcAgent.NpcState.InspectingDisplay   => One(inspecting,  targetName),
            NpcAgent.NpcState.ApproachingRegister => One(approaching, targetName),
            NpcAgent.NpcState.Queueing            => One(queueing,    targetName),
            NpcAgent.NpcState.WaitingAtRegister   => One(waiting,     targetName),
            NpcAgent.NpcState.Negotiating         => One(negotiating, targetName),
            NpcAgent.NpcState.Leaving             => reason switch
            {
                NpcAgent.LeaveReason.Purchased => One(purchased, targetName),
                NpcAgent.LeaveReason.Outbid    => One(outbid,    targetName),
                NpcAgent.LeaveReason.QueueFull => One(queueFull, targetName),
                _                              => One(noDeal,    targetName),
            },
            _                                     => "",
        };
}
}
