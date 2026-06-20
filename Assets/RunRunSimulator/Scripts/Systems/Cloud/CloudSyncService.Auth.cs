using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using Unity.Services.Authentication;
using Unity.Services.Authentication.PlayerAccounts;
using Unity.Services.CloudCode;
using Unity.Services.CloudSave;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.Serialization;

public partial class CloudSyncService
{
    // ── Private Methods ───────────────────────────────────────────

    private void SetupAuthEvents()
    {
        AuthenticationService.Instance.SignedIn += () =>
           Debug.Log($"[CloudSync] Auth signed in — ID: {AuthenticationService.Instance.PlayerId}");
        AuthenticationService.Instance.SignInFailed += err =>
            Debug.LogError($"[CloudSync] Sign-in failed: {err}");
        AuthenticationService.Instance.SignedOut += () =>
        {
            isSignedIn = false;
            authMethod = "---";
            status     = "Signed out";
        };
        AuthenticationService.Instance.Expired += () =>
        {
            isSignedIn = false;
            authMethod = "---";
            status     = "Session expired — sign in again";
            Debug.LogWarning("[CloudSync] Session expired.");
        };
    }

    private async void OnPlayerAccountSignedIn()
    {
        try
        {
            status = "Authenticating...";
            await AuthenticationService.Instance.SignInWithUnityAsync(
                PlayerAccountService.Instance.AccessToken);
            await OnSignedInComplete("Unity Account");
        }
        catch (AuthenticationException ex)
        {
            status = $"Auth error: {ex.Message}";
            Debug.LogException(ex);
        }
        catch (RequestFailedException ex)
        {
            status = $"Request failed: {ex.Message}";
            Debug.LogException(ex);
        }
    }

    private async Task OnSignedInComplete(string method)
    {
        playerID   = AuthenticationService.Instance.PlayerId;
        isSignedIn = true;
        authMethod = method;
        playerName = await SafeGetPlayerName();
        status     = $"Signed in ({method})";
        RefreshSecurityDisplay();
      //  Debug.Log($"[CloudSync] Signed in via '{method}' — ID: {playerID}, Name: {playerName}");

        // Scope local save by player + auto-sync from cloud
        SaveSystem.SetUserScope(playerID);
        SaveSystem.LoadInto(registry);
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
        await FetchServerTimeAsync();
        await PullAsync();
        await NotifyPendingCombatResultsAsync();
    }
    private async Task NotifyPendingCombatResultsAsync()
    {
        try
        {
            var data = await CloudSaveService.Instance.Data.Player.LoadAsync(
                new HashSet<string> { "combat_results" });
            if (!data.ContainsKey("combat_results")) return;

            var json    = data["combat_results"].Value.GetAs<string>();
            var results = JsonConvert.DeserializeObject<List<object>>(json);
            if (results == null || results.Count == 0) return;

            Debug.Log($"[CloudSync] ¡Bienvenido, {playerName}! Tienes {results.Count} MoriMochi(s) con resultado de combate pendiente. Presiona 'Check Pending Results' para aplicarlos.");
        }
        catch { /* silent — non-critical notification */ }
    }

    // Calls the minimal get-server-time Cloud Code function and caches the offset.
    // Non-critical: if it fails the game falls back to local time gracefully.
    private async Task FetchServerTimeAsync()
    {
        try
        {
            var raw = await CloudCodeService.Instance.CallEndpointAsync<string>(
                "get-server-time", new Dictionary<string, object>());

            var resp = JsonConvert.DeserializeObject<Dictionary<string, string>>(raw);
            if (resp != null && resp.TryGetValue("utc", out string utcStr) &&
                DateTime.TryParse(utcStr, null,
                    System.Globalization.DateTimeStyles.AdjustToUniversal |
                    System.Globalization.DateTimeStyles.AssumeUniversal, out DateTime serverUtc))
            {
                _serverOffset      = serverUtc - DateTime.UtcNow;
                serverOffsetDisplay = $"{_serverOffset.TotalSeconds:+0.#;-0.#;0} s";
                Debug.Log($"[CloudSync] Server time offset: {_serverOffset.TotalSeconds:+0.#;-0.#;0} s");
            }
        }
        catch (Exception e)
        {
            serverOffsetDisplay = "fetch failed (using local)";
            Debug.LogWarning($"[CloudSync] Could not fetch server time: {e.Message}");
        }
    }
    // ── Public Methods ────────────────────────────────────────────

    public async Task InitializeAsync()
    {
        try
        {
            status = "Initializing...";

            if (UnityServices.State == ServicesInitializationState.Uninitialized)
                await UnityServices.InitializeAsync();

            SetupAuthEvents();
            PlayerAccountService.Instance.SignedIn += OnPlayerAccountSignedIn;

            if (AuthenticationService.Instance.IsSignedIn)
            {
                await OnSignedInComplete("Already signed in");
                return;
            }

            // Resume cached session silently — works for any prior auth method
            if (AuthenticationService.Instance.SessionTokenExists)
            {
                status = "Resuming session...";
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                await OnSignedInComplete("Session resumed");
                return;
            }

            status = "Ready — press 'Sign In with Unity Account'";
        }
        catch (Exception e)
        {
            status = $"Init error: {e.Message}";
            Debug.LogError($"[CloudSync] Init failed: {e}");
        }
    }

    [Button("Sign In Anonymous (DEV)", ButtonSizes.Medium), GUIColor(0.6f, 0.6f, 0.6f)]
    [BoxGroup("Cloud Actions"), EnableIf("@!isSignedIn")]
    private async void SignInAnonButton()
    {
        try
        {
            status = "Signing in anonymously...";
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            await OnSignedInComplete("Anonymous");
        }
        catch (Exception e)
        {
            status = $"Anon sign-in error: {e.Message}";
            Debug.LogError($"[CloudSync] SignInAnonymously failed: {e}");
        }
    }

    [Button("Sign In with Unity Account", ButtonSizes.Large), GUIColor(0.4f, 0.6f, 1f)]
    [BoxGroup("Cloud Actions"), EnableIf("@!isSignedIn")]
    private async void SignInButton()
    {
        try
        {
            status = "Opening sign-in...";
            await PlayerAccountService.Instance.StartSignInAsync();
            // Flow continues in OnPlayerAccountSignedIn (event-driven by the browser callback)
        }
        catch (Exception e)
        {
            status = $"Sign-in error: {e.Message}";
            Debug.LogError($"[CloudSync] StartSignInAsync failed: {e}");
        }
    }

    [Button("Sign Out", ButtonSizes.Medium), GUIColor(1f, 0.5f, 0.5f)]
    [BoxGroup("Cloud Actions"), EnableIf("isSignedIn")]
    private void SignOut()
    {
        AuthenticationService.Instance.SignOut();
        PlayerAccountService.Instance.SignOut();
        playerID   = "---";
        playerName = "---";
        // isSignedIn + status updated by the SignedOut event handler above
    }

    [Button("Update Name"), GUIColor(0.9f, 0.9f, 0.4f)]
    [BoxGroup("Account"), EnableIf("isSignedIn")]
    private async void UpdateNameButton()
    {
        if (string.IsNullOrWhiteSpace(newNameInput)) return;
        await UpdatePlayerNameAsync(newNameInput);
    }

    public async Task UpdatePlayerNameAsync(string newName)
    {
        try
        {
            await AuthenticationService.Instance.UpdatePlayerNameAsync(newName);
            playerName   = await SafeGetPlayerName();
            newNameInput = "";
            status       = $"Name updated: {playerName}";
            Debug.Log($"[CloudSync] Player name updated → {playerName}");
        }
        catch (Exception e)
        {
            status = $"Name update error: {e.Message}";
            Debug.LogError($"[CloudSync] UpdatePlayerName failed: {e}");
        }
    }
}
