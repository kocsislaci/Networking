using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using System.Collections.Generic;
using System;
using System.Collections;
using UnityEngine.Events;

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

    // coroutines

    IEnumerator HeartbeatLobbyCoroutine(float waitTimeSeconds)
    {
        var delay = new WaitForSecondsRealtime(waitTimeSeconds);

        while (true)
        {
            LobbyService.Instance.SendHeartbeatPingAsync(ConnectedLobby.Id);
            yield return delay;
        }
    }

    private async void RefreshPlayerData()
    {
        timer += Time.deltaTime;
        if ((State == LobbyState.Host || State == LobbyState.Client) && timer >= refreshRate)
        {
            try
            {
                timer = 0f;
                Players = (await LobbyService.Instance.GetLobbyAsync(ConnectedLobby.Id)).Players;
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError(e);
            }
        }
    }
    private float refreshRate = 10f;
    private float timer = 0f;

    // lifecycle methods

    private async void Start()
    {
        await UnityServices.InitializeAsync();
        SetPlayerAuthenticationCallbacks();
    }

    private void Update()
    {
        RefreshPlayerData();
    }

    private void OnDestroy()
    {
        if (ConnectedLobby != null)
        {
            if (State == LobbyState.Host)
            {
                CloseLobby();
            }
            else if (State == LobbyState.Client)
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

    public async void HostLobby()
    {
        try
        {
            Lobby createdLobby = await LobbyService.Instance.CreateLobbyAsync("neverMindChangedToLobbyCode", 10);
            ConnectedLobby = await LobbyService.Instance.UpdateLobbyAsync(createdLobby.Id, new UpdateLobbyOptions { Name = createdLobby.LobbyCode });
            StartCoroutine(HeartbeatLobbyCoroutine(15));
            UpdatePlayerColor("");
            State = LobbyState.Host;
            ConnectedLobbyChanged.Invoke(ConnectedLobby.Name);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }

    public async void JoinLobby(string lobbyCode)
    {
        try
        {
            ConnectedLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode);
            State = LobbyState.Client;
            UpdatePlayerColor("");
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
