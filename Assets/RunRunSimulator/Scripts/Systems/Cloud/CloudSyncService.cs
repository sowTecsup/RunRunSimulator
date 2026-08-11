using System;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
namespace MoriMonchiSimulator
{

// Dashboard requirements:
//   Authentication → enable Anonymous + Unity Player Accounts
//   Cloud Save     → enable
// Attach to same GameObject as GameManager. Assign CreatureRegistrySO in Setup.
public class CloudSyncService : MonoBehaviour
{
    private CreatureRegistrySO  registry;
    private FurnitureRegistrySO furnitureRegistry;
    private PlayerInventorySO   inventory;

    private CloudAuth    auth;
    private CloudSyncOps syncOps;

    [ShowInInspector, ReadOnly, BoxGroup("Status")]
    private string status = "Not initialized";

    [BoxGroup("Account"), LabelText("New Name"), EnableIf("isSignedIn")]
    [FormerlySerializedAs("_newNameInput")]
    [SerializeField] private string newNameInput = "";

    // ── Inspector Displays ────────────────────────────────────────

    [ShowInInspector, ReadOnly, BoxGroup("Status"), LabelText("Player ID")]
    private string PlayerIDDisplay => auth?.PlayerID ?? "---";

    [ShowInInspector, ReadOnly, BoxGroup("Status"), LabelText("Player Name")]
    private string PlayerNameDisplay => auth?.PlayerName ?? "---";

    [ShowInInspector, ReadOnly, BoxGroup("Status"), LabelText("Signed In")]
    private bool SignedInDisplay => auth?.IsSignedIn ?? false;

    [ShowInInspector, ReadOnly, BoxGroup("Status"), LabelText("Auth Method")]
    private string AuthMethodDisplay => auth?.AuthMethod ?? "---";

    [ShowInInspector, ReadOnly, BoxGroup("Security"), LabelText("Last Pull")]
    private string LastPullDisplay => syncOps?.LastPullDisplay ?? "---";

    [ShowInInspector, ReadOnly, BoxGroup("Security"), LabelText("Last Known Cloud Push")]
    private string LastKnownCloudDisplay => syncOps?.LastKnownCloudDisplay ?? "---";

    [ShowInInspector, ReadOnly, BoxGroup("Security"), LabelText("Security Status")]
    private string SecurityStatusDisplay => syncOps?.SecurityStatus ?? "---";

    [ShowInInspector, ReadOnly, BoxGroup("Security"), LabelText("Server Time Offset")]
    private string ServerOffsetDisplay => auth?.ServerOffsetDisplay ?? "---";

    private bool isSignedIn => auth != null && auth.IsSignedIn;

    // ── Public Facade ─────────────────────────────────────────────

    public TimeSpan ServerOffset => auth?.ServerOffset ?? TimeSpan.Zero;

    public Task InitializeAsync() => auth.InitializeAsync();
    public Task PushAsync() => syncOps.PushAsync();
    public Task PullAsync() => syncOps.PullAsync();
    public Task ResetProgressAsync() => syncOps.ResetProgressAsync();
    public Task UpdatePlayerNameAsync(string newName) => auth.UpdatePlayerNameAsync(newName);

    // ── Lifecycle ─────────────────────────────────────────────────

    private async void Start()
    {
        registry          = GameManager.Instance.Registry;
        furnitureRegistry = GameManager.Instance.FurnitureRegistry;
        inventory         = GameManager.Instance.Inventory;
        auth    = new CloudAuth(s => status = s, HandleSignedInAsync);
        syncOps = new CloudSyncOps(auth, registry, furnitureRegistry, inventory, s => status = s);
        await auth.InitializeAsync();
    }

    private void OnDestroy() => auth?.Teardown();

    private async Task HandleSignedInAsync(string method)
    {
        syncOps.RefreshSecurityDisplay();

        // Scope local save by player + auto-sync from cloud
        SaveSystem.SetUserScope(auth.PlayerID);
        SaveSystem.LoadInto(registry);
        SaveSystem.LoadSocialGraph(registry);
        // Reflect local data in the UI immediately: the cloud pull below raises its
        // own reload only when the cloud actually has data, so a local-only player
        // (fresh/anon/offline, or after a reset) would otherwise see an empty grid.
        GameEvents.RegistryReloaded(registry);

        // Pre-warm from local cache so the player sees their data instantly before the
        // cloud pull completes. PullAsync below will override with the authoritative cloud
        // copy if it exists. Reload (not Changed) → UI rebuilds, no re-save.
        if (furnitureRegistry != null)
        {
            SaveSystem.LoadFurniture(furnitureRegistry);
            GameEvents.FurnitureReloaded(furnitureRegistry);
        }
        if (inventory != null)
        {
            SaveSystem.LoadInventory(inventory);
            GameEvents.InventoryReloaded(inventory);
        }

        await auth.FetchServerTimeAsync();
        await syncOps.PullAsync();
    }

    // ── Buttons ───────────────────────────────────────────────────

    [Button("Sign In Anonymous (DEV)", ButtonSizes.Medium), GUIColor(0.6f, 0.6f, 0.6f)]
    [BoxGroup("Cloud Actions"), EnableIf("@!isSignedIn")]
    private async void SignInAnonButton() => await auth.SignInAnonymouslyAsync();

    [Button("Sign In with Unity Account", ButtonSizes.Large), GUIColor(0.4f, 0.6f, 1f)]
    [BoxGroup("Cloud Actions"), EnableIf("@!isSignedIn")]
    private async void SignInButton() => await auth.StartUnitySignInAsync();

    [Button("Sign Out", ButtonSizes.Medium), GUIColor(1f, 0.5f, 0.5f)]
    [BoxGroup("Cloud Actions"), EnableIf("isSignedIn")]
    private void SignOut() => auth.SignOut();

    [Button("Update Name"), GUIColor(0.9f, 0.9f, 0.4f)]
    [BoxGroup("Account"), EnableIf("isSignedIn")]
    private async void UpdateNameButton()
    {
        if (string.IsNullOrWhiteSpace(newNameInput)) return;
        await auth.UpdatePlayerNameAsync(newNameInput);
        newNameInput = "";
    }

    [Button("Reset All Progress (DEV)", ButtonSizes.Medium), GUIColor(0.9f, 0.2f, 0.2f)]
    [BoxGroup("Cloud Actions"), EnableIf("isSignedIn")]
    private async void ResetProgressButton() => await syncOps.ResetProgressAsync();

    [Button("Push to Cloud", ButtonSizes.Large), GUIColor(1f, 0.85f, 0.3f)]
    [BoxGroup("Cloud Actions"), EnableIf("isSignedIn")]
    private async void PushButton() => await syncOps.PushAsync();

    [Button("Pull from Cloud", ButtonSizes.Large), GUIColor(0.5f, 0.85f, 1f)]
    [BoxGroup("Cloud Actions"), EnableIf("isSignedIn")]
    private async void PullButton() => await syncOps.PullAsync();
}
}
