using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using System.Collections.Generic;
using System;
using System.Collections;
using UnityEngine.Events;
using System.Threading.Tasks;

public class LobbyManager : MonoBehaviour
{
    // state variables

    private LobbyState state = LobbyState.SignedOut;
    private LobbyState State
    {
        get => state;
        set
        {
            state = value;
            LobbyStateChanged.Invoke(state);
        }
    }
    private Dictionary<string, string> lobbies;
    private Dictionary<string, string> Lobbies
    {
        set
        {
            lobbies = value;
            LobbiesChanged.Invoke(lobbies);
        }
    }
    private Lobby connectedLobby;
    private Lobby ConnectedLobby
    {
        get => connectedLobby;
        set
        {
            connectedLobby = value;
            ConnectedLobbyChanged.Invoke(connectedLobby?.Id ?? "");
        }
    }
    private List<Player> players;
    private List<Player> Players
    {
        set
        {
            players = value;
            PlayersChanged.Invoke(players);
        }
    }

    // events on ui changes

    [HideInInspector] public UnityEvent<LobbyState> LobbyStateChanged = new();
    [HideInInspector] public UnityEvent<string> PlayerIdChanged = new();
    [HideInInspector] public UnityEvent<Dictionary<string, string>> LobbiesChanged = new();
    [HideInInspector] public UnityEvent<string> ConnectedLobbyChanged = new();
    [HideInInspector] public UnityEvent<List<Player>> PlayersChanged = new();

    // coroutines and pollings

    IEnumerator HeartbeatLobbyCoroutine(float waitTimeSeconds)
    {
        var delay = new WaitForSecondsRealtime(waitTimeSeconds);

        while (true)
        {
            try
            {
                if (ConnectedLobby == null)
                {
                    break;
                }
                LobbyService.Instance.SendHeartbeatPingAsync(ConnectedLobby.Id);
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError(e);
            }
            yield return delay;
        }
    }

    private float playerDataRefreshRate = 4f;
    private float playerDataTimer = 0f;
    private async void RefreshPlayerData()
    {
        playerDataTimer += Time.deltaTime;
        if (playerDataTimer >= playerDataRefreshRate)
        {
            try
            {
                playerDataTimer = 0f;
                Players = (await LobbyService.Instance.GetLobbyAsync(ConnectedLobby.Id)).Players;
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError(e);
            }
        }
    }
    private float lobbyDataRefreshRate = 4f;
    private float lobbyDataTimer = 0f;
    private async void RefreshLobbyData()
    {
        lobbyDataTimer += Time.deltaTime;
        if (lobbyDataTimer >= lobbyDataRefreshRate)
        {
            try
            {
                lobbyDataTimer = 0f;
                ConnectedLobby = await LobbyService.Instance.GetLobbyAsync(ConnectedLobby.Id);

                if (State == LobbyState.ClientInLobby)
                {
                    if (ConnectedLobby.Data["JOIN_CODE"].Value != "")
                    {
                        JoinGame();
                    }
                }
            }
            catch (LobbyServiceException e)
            {
                if (e.Reason == LobbyExceptionReason.LobbyNotFound)
                {
                    ConnectedLobby = null;
                    Players = new List<Player>();
                    State = LobbyState.SignedIn;
                    Debug.LogError(e);
                }
            }
        }
    }

    // lifecycle methods

    private async void Start()
    {
        await UnityServices.InitializeAsync();
        SetPlayerAuthenticationCallbacks();
    }

    private void Update()
    {
        if (State == LobbyState.HostInLobby || State == LobbyState.ClientInLobby)
        {
            RefreshPlayerData();
        }
        if (State == LobbyState.HostInLobby || State == LobbyState.ClientInLobby || State == LobbyState.ClientInGame)
        {
            RefreshLobbyData();
        }
    }

    private void OnDestroy()
    {
        if (ConnectedLobby != null)
        {
            if (State == LobbyState.HostInLobby)
            {
                CloseLobby();
            }
            else if (State == LobbyState.ClientInLobby)
            {
                LeaveLobby();
            }
        }
        if (AuthenticationService.Instance.IsSignedIn)
        {
            SignOut();
        }
    }

    // API methods

    public string GetPlayerName(string playerId)
    {
        players.ForEach(p => Debug.Log(p.Id));
        return "Unknown";
    }

    public async void SignIn()
    {
        try
        {
            if (AuthenticationService.Instance.SessionTokenExists)
            {
                AuthenticationService.Instance.ClearSessionToken();
            }
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            State = LobbyState.SignedIn;
            PlayerIdChanged.Invoke(AuthenticationService.Instance.PlayerId);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }

    public void SignOut()
    {
        AuthenticationService.Instance.SignOut();
        AuthenticationService.Instance.ClearSessionToken();
        State = LobbyState.SignedOut;
        PlayerIdChanged.Invoke("");
    }

    public async void RefreshLobbies()
    {
        try
        {
            QueryResponse queryResponse = await LobbyService.Instance.QueryLobbiesAsync(new QueryLobbiesOptions
            {
                Order = new List<QueryOrder> { new QueryOrder(false, QueryOrder.FieldOptions.Created) },
            });

            Dictionary<string, string> lobbies = new();
            queryResponse.Results.ForEach(lobby =>
            {
                lobbies.Add(lobby.Id, lobby.Name);
            });
            Lobbies = lobbies;
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }

    public async void HostLobby(string name)
    {
        try
        {
            Lobby createdLobby = await LobbyService.Instance.CreateLobbyAsync("neverMindChangedToLobbyCode", 10);
            ConnectedLobby = await LobbyService.Instance.UpdateLobbyAsync(createdLobby.Id, new UpdateLobbyOptions
            {
                Name = createdLobby.LobbyCode,
                Data = new Dictionary<string, DataObject> { { "JOIN_CODE", new DataObject(DataObject.VisibilityOptions.Member, "") } }
            });
            StartCoroutine(HeartbeatLobbyCoroutine(15));
            UpdatePlayerInitially(name);
            State = LobbyState.HostInLobby;
            ConnectedLobbyChanged.Invoke(ConnectedLobby.Name);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }

    public async void JoinLobby(string lobbyCode, string name)
    {
        try
        {
            ConnectedLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode);
            State = LobbyState.ClientInLobby;
            UpdatePlayerInitially(name);
            ConnectedLobbyChanged.Invoke(ConnectedLobby.Name);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }

    public async void CloseLobby()
    {
        try
        {
            StopCoroutine(HeartbeatLobbyCoroutine(15));
            await LobbyService.Instance.DeleteLobbyAsync(ConnectedLobby.Id);
            ConnectedLobby = null;
            State = LobbyState.SignedIn;
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }

    public async void LeaveLobby()
    {
        try
        {
            await LobbyService.Instance.RemovePlayerAsync(ConnectedLobby.Id, AuthenticationService.Instance.PlayerId);
            ConnectedLobby = null;
            State = LobbyState.SignedIn;
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }

    public async void StartGame()
    {
        try
        {
            string joinCode = await RelayManager.CreateRelay();

            ConnectedLobby = await UpdateJoinCodeInLobby(joinCode);

            RelayManager.StartHost();

            State = LobbyState.HostInGame;
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }

    public async void JoinGame()
    {
        try
        {
            await RelayManager.JoinRelay(ConnectedLobby.Data["JOIN_CODE"].Value);
            RelayManager.StartClient();
            State = LobbyState.ClientInGame;
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }

    public void CloseGame()
    {
        try
        {
            RelayManager.ShutDown();
            CloseLobby();
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }

    public void LeaveGame()
    {
        try
        {
            RelayManager.ShutDown();
            LeaveLobby();
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }

    // Update API methods
    public async void UpdatePlayerInitially(string name)
    {
        try {
            UpdatePlayerOptions options = new UpdatePlayerOptions();

            options.Data = new Dictionary<string, PlayerDataObject>()
            {
                {
                    "name", new PlayerDataObject(
                        visibility: PlayerDataObject.VisibilityOptions.Member,
                        value: name)
                },
                {
                    "color", new PlayerDataObject(visibility: PlayerDataObject.VisibilityOptions.Member, value: "")
                }
            };
            await LobbyService.Instance.UpdatePlayerAsync(connectedLobby.Id, AuthenticationService.Instance.PlayerId, options);
        } catch (LobbyServiceException e) {
            Debug.LogError(e);
        }
    }
    public async void UpdatePlayerColor(string color)
    {
        try
        {
            UpdatePlayerOptions options = new UpdatePlayerOptions();

            options.Data = new Dictionary<string, PlayerDataObject>()
            {
                {
                    "color", new PlayerDataObject(
                        visibility: PlayerDataObject.VisibilityOptions.Member,
                        value: color)
                },
            };
            await LobbyService.Instance.UpdatePlayerAsync(connectedLobby.Id, AuthenticationService.Instance.PlayerId, options);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }
    public async Task<Lobby> UpdateJoinCodeInLobby(string joinCode = "")
    {
        return await LobbyService.Instance.UpdateLobbyAsync(ConnectedLobby.Id, new UpdateLobbyOptions
        {
            Data = new Dictionary<string, DataObject> { { "JOIN_CODE", new DataObject(DataObject.VisibilityOptions.Member, joinCode) } }
        });
    }

    // helper methods

    private void SetPlayerAuthenticationCallbacks()
    {
        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Hello signed in " + AuthenticationService.Instance.PlayerId);
        };
        AuthenticationService.Instance.SignInFailed += (err) =>
        {
            Debug.LogError(err);
        };
        AuthenticationService.Instance.SignedOut += () =>
        {
            Debug.Log("Bye signed out " + AuthenticationService.Instance.PlayerId);
        };
        AuthenticationService.Instance.Expired += () =>
        {
            Debug.Log("Player session could not be refreshed and expired.");
        };
    }
}
