using Sirenix.OdinInspector;
using UnityEngine;
namespace MoriMonchiSimulator
{

public class GameClock : MonoBehaviour
{
    public static GameClock Instance { get; private set; }

    [Required, AssetsOnly]
    [SerializeField] private DayScheduleSO schedule;

    private WorldStateSO state;
    private int blockIndex = -1;
    private bool loaded;

    public int        Day                      => state != null ? state.Day : 1;
    public float      MinuteOfDay              => state != null ? state.MinuteOfDay : 0f;
    public long        TotalMinutes             => Day * 1440L + (long)MinuteOfDay;
    public DayBlockDef Block                    => schedule != null ? schedule.BlockAt(MinuteOfDay) : null;
    public float       GameMinutesPerRealSecond => schedule != null ? schedule.GameMinutesPerRealSecond : 1f;
    public float       NeedsTimeScale           => (!loaded || Paused) ? 0f : GameMinutesPerRealSecond;
    public bool        Paused                   { get; set; }
    public bool        Loaded                   => loaded;

    private void Awake()
    {
        Instance = this;
        state = GameManager.Instance != null ? GameManager.Instance.WorldState : null;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnEnable()
    {
        GameEvents.OnWorldStateReloaded += HandleWorldStateReloaded;
        GameEvents.OnExpeditionReturned += HandleExpeditionReturned;
    }

    private void OnDisable()
    {
        GameEvents.OnWorldStateReloaded -= HandleWorldStateReloaded;
        GameEvents.OnExpeditionReturned -= HandleExpeditionReturned;
    }

    private void HandleWorldStateReloaded(WorldStateSO reloaded)
    {
        state = reloaded;
        blockIndex = schedule != null ? schedule.BlockIndexAt(MinuteOfDay) : -1;
        loaded = true;
        GameEvents.DayBlockChanged(Block);
    }

    private void HandleExpeditionReturned(ExpeditionReturn r) => AdvanceToNextDay();

    private void Update()
    {
        if (!loaded || Paused || state == null || schedule == null) return;

        state.MinuteOfDay += Time.deltaTime * GameMinutesPerRealSecond;
        if (state.MinuteOfDay >= 1440f)
        {
            state.Day++;
            state.MinuteOfDay -= 1440f;
            GameEvents.DayStarted(state.Day);
        }

        int newIndex = schedule.BlockIndexAt(state.MinuteOfDay);
        if (newIndex == blockIndex) return;

        blockIndex = newIndex;
        var block = Block;
        Debug.Log($"[GameClock] Dia {state.Day} · bloque {(block != null ? block.NameKey : "?")}");
        GameEvents.DayBlockChanged(block);
        GameEvents.WorldStateChanged(state);
    }

    public void AdvanceToNextBlock()
    {
        if (state == null || schedule == null || schedule.Blocks.Count == 0) return;

        int nextIndex = (blockIndex + 1) % schedule.Blocks.Count;
        float nextMinute = schedule.Blocks[nextIndex].StartHour * 60f;
        bool wrapsToNextDay = nextMinute <= state.MinuteOfDay;
        if (wrapsToNextDay) state.Day++;
        state.MinuteOfDay = nextMinute;
        blockIndex = nextIndex;

        if (wrapsToNextDay) GameEvents.DayStarted(state.Day);
        GameEvents.DayBlockChanged(Block);
        GameEvents.WorldStateChanged(state);
    }

    public void AdvanceToNextDay()
    {
        if (state == null) return;

        float firstBlockHour = schedule != null && schedule.Blocks.Count > 0 ? schedule.Blocks[0].StartHour : 6f;
        float dawn = firstBlockHour * 60f;
        bool newDay = state.MinuteOfDay >= dawn;
        if (newDay) state.Day++;
        state.MinuteOfDay = dawn;
        blockIndex = schedule != null ? schedule.BlockIndexAt(state.MinuteOfDay) : -1;

        if (newDay) GameEvents.DayStarted(state.Day);
        GameEvents.DayBlockChanged(Block);
        GameEvents.WorldStateChanged(state);
    }
}
}
