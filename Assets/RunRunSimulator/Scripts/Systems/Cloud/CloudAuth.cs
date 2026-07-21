using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Unity.Services.Authentication;
using Unity.Services.Authentication.PlayerAccounts;
using Unity.Services.CloudCode;
using Unity.Services.Core;
using UnityEngine;
namespace MoriMonchiSimulator
{

public class CloudAuth
{
    private readonly Action<string>      setStatus;
    private readonly Func<string, Task>  onSignedInComplete;

    private bool     isSignedIn          = false;
    private string   playerID            = "---";
    private string   playerName          = "---";
    private string   authMethod          = "---";
    private TimeSpan _serverOffset       = TimeSpan.Zero;
    private string   serverOffsetDisplay = "---";

    public bool     IsSignedIn          => isSignedIn;
    public string   PlayerID            => playerID;
    public string   PlayerName          => playerName;
    public string   AuthMethod          => authMethod;
    public TimeSpan ServerOffset        => _serverOffset;
    public string   ServerOffsetDisplay => serverOffsetDisplay;

    public CloudAuth(Action<string> setStatus, Func<string, Task> onSignedInComplete)
    {
        this.setStatus          = setStatus;
        this.onSignedInComplete = onSignedInComplete;
    }

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
            setStatus("Signed out");
        };
        AuthenticationService.Instance.Expired += () =>
        {
            isSignedIn = false;
            authMethod = "---";
            setStatus("Session expired — sign in again");
            Debug.LogWarning("[CloudSync] Session expired.");
        };
    }

    private async void OnPlayerAccountSignedIn()
    {
        try
        {
            setStatus("Authenticating...");
            await AuthenticationService.Instance.SignInWithUnityAsync(
                PlayerAccountService.Instance.AccessToken);
            await OnSignedInComplete("Unity Account");
        }
        catch (AuthenticationException ex)
        {
            setStatus($"Auth error: {ex.Message}");
            Debug.LogException(ex);
        }
        catch (RequestFailedException ex)
        {
            setStatus($"Request failed: {ex.Message}");
            Debug.LogException(ex);
        }
    }

    private async Task OnSignedInComplete(string method)
    {
        playerID   = AuthenticationService.Instance.PlayerId;
        isSignedIn = true;
        authMethod = method;
        playerName = await SafeGetPlayerName();
        setStatus($"Signed in ({method})");

        await onSignedInComplete(method);
    }

    private async Task<string> SafeGetPlayerName()
    {
        try { return await AuthenticationService.Instance.GetPlayerNameAsync() ?? "---"; }
        catch { return "---"; }
    }

    // Calls the minimal get-server-time Cloud Code function and caches the offset.
    // Non-critical: if it fails the game falls back to local time gracefully.
    public async Task FetchServerTimeAsync()
    {
        try
        {
            var raw = await CloudCodeService.Instance.CallEndpointAsync<string>(
                "get-server-time", new System.Collections.Generic.Dictionary<string, object>());

            var resp = JsonConvert.DeserializeObject<System.Collections.Generic.Dictionary<string, string>>(raw);
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
            setStatus("Initializing...");

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
                setStatus("Resuming session...");
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                await OnSignedInComplete("Session resumed");
                return;
            }

            setStatus("Ready — press 'Sign In with Unity Account'");
        }
        catch (Exception e)
        {
            setStatus($"Init error: {e.Message}");
            Debug.LogError($"[CloudSync] Init failed: {e}");
        }
    }

    public async Task SignInAnonymouslyAsync()
    {
        try
        {
            setStatus("Signing in anonymously...");
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            await OnSignedInComplete("Anonymous");
        }
        catch (Exception e)
        {
            setStatus($"Anon sign-in error: {e.Message}");
            Debug.LogError($"[CloudSync] SignInAnonymously failed: {e}");
        }
    }

    public async Task StartUnitySignInAsync()
    {
        try
        {
            setStatus("Opening sign-in...");
            await PlayerAccountService.Instance.StartSignInAsync();
            // Flow continues in OnPlayerAccountSignedIn (event-driven by the browser callback)
        }
        catch (Exception e)
        {
            setStatus($"Sign-in error: {e.Message}");
            Debug.LogError($"[CloudSync] StartSignInAsync failed: {e}");
        }
    }

    public void SignOut()
    {
        AuthenticationService.Instance.SignOut();
        PlayerAccountService.Instance.SignOut();
        playerID   = "---";
        playerName = "---";
        // isSignedIn + status updated by the SignedOut event handler above
    }

    public async Task UpdatePlayerNameAsync(string newName)
    {
        try
        {
            await AuthenticationService.Instance.UpdatePlayerNameAsync(newName);
            playerName = await SafeGetPlayerName();
            setStatus($"Name updated: {playerName}");
            Debug.Log($"[CloudSync] Player name updated → {playerName}");
        }
        catch (Exception e)
        {
            setStatus($"Name update error: {e.Message}");
            Debug.LogError($"[CloudSync] UpdatePlayerName failed: {e}");
        }
    }

    public void Teardown()
    {
        PlayerAccountService.Instance.SignedIn -= OnPlayerAccountSignedIn;
    }
}
}
