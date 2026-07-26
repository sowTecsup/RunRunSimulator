using UnityEngine;
namespace MoriMonchiSimulator
{

public static class NpcDialogueBank
{
    private static readonly string[] wandering =
    {
        "npc.wandering.01",
        "npc.wandering.02",
        "npc.wandering.03",
        "npc.wandering.04",
        "npc.wandering.05",
        "npc.wandering.06",
    };

    private static readonly string[] inspecting =
    {
        "npc.inspecting.01",
        "npc.inspecting.02",
        "npc.inspecting.03",
        "npc.inspecting.04",
        "npc.inspecting.05",
        "npc.inspecting.06",
    };

    private static readonly string[] approaching =
    {
        "npc.approaching.01",
        "npc.approaching.02",
        "npc.approaching.03",
        "npc.approaching.04",
        "npc.approaching.05",
    };

    private static readonly string[] queueing =
    {
        "npc.queueing.01",
        "npc.queueing.02",
        "npc.queueing.03",
        "npc.queueing.04",
        "npc.queueing.05",
    };

    private static readonly string[] waiting =
    {
        "npc.waiting.01",
        "npc.waiting.02",
        "npc.waiting.03",
        "npc.waiting.04",
        "npc.waiting.05",
    };

    private static readonly string[] negotiating =
    {
        "npc.negotiating.01",
        "npc.negotiating.02",
        "npc.negotiating.03",
        "npc.negotiating.04",
        "npc.negotiating.05",
    };

    private static readonly string[] purchased =
    {
        "npc.purchased.01",
        "npc.purchased.02",
        "npc.purchased.03",
        "npc.purchased.04",
        "npc.purchased.05",
    };

    private static readonly string[] outbid =
    {
        "npc.outbid.01",
        "npc.outbid.02",
        "npc.outbid.03",
        "npc.outbid.04",
        "npc.outbid.05",
    };

    private static readonly string[] queueFull =
    {
        "npc.queuefull.01",
        "npc.queuefull.02",
        "npc.queuefull.03",
        "npc.queuefull.04",
        "npc.queuefull.05",
    };

    private static readonly string[] noDeal =
    {
        "npc.nodeal.01",
        "npc.nodeal.02",
        "npc.nodeal.03",
        "npc.nodeal.04",
        "npc.nodeal.05",
    };

    private static string One(string[] bank, string targetName) =>
        Loc.Tr(bank[Random.Range(0, bank.Length)], targetName);

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
