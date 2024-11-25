using System.Collections.Generic;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;


public class LobbyUIController : MonoBehaviour
{
    // references
    [SerializeField] private LobbyManager lobbyManager;
    [SerializeField] private UIDocument lobbyListDocument;
    [SerializeField] private UIDocument lobbyDocument;
    [SerializeField] private VisualTreeAsset playerListElementTemplate;

    private bool lobbyListViewListenersSet = false;
    private bool lobbyViewListenersSet = false;

    // lobby list buttons
    private Button signInButton;
    private Button signOutButton;
    private Button refreshButton;
    private Button createLobbyButton;
    private Button joinLobbyButton;

    // lobby list fields
    private Label playerIdLabel;
    private TextField joinLobbyTextField;
    private string typedInLobbyCode = "";
    private TextField lobbyListTextField;

    // lobby view buttons
    private Button startButton;
    private Button readyButton;
    private Button closeButton;
    private Button leaveButton;

    // lobby view fields
    private Label lobbyIdLabel;
    private VisualElement playerList;
    private DropdownField colorPickerDropdown;

    // unity events
    private void Awake()
    {
        SetUpLobbyListViewReferences();
        SetupLobbyViewReferences();
        SetupDropdown();

        // listening to lobby manager events
        lobbyManager.LobbyStateChanged.AddListener(OnLobbyStateChanged);
        lobbyManager.PlayerIdChanged.AddListener(OnPlayerIdChanged);
        lobbyManager.LobbiesChanged.AddListener(OnLobbyListChanged);
        lobbyManager.ConnectedLobbyChanged.AddListener(OnLobbyIdChanged);
        lobbyManager.PlayersChanged.AddListener(OnPlayerListChanged);

        OnLobbyStateChanged(LobbyState.SignedOut);
    }
    private void OnDestroy()
    {
        lobbyManager.LobbyStateChanged.RemoveListener(OnLobbyStateChanged);
        lobbyManager.PlayerIdChanged.RemoveListener(OnPlayerIdChanged);
        lobbyManager.LobbiesChanged.RemoveListener(OnLobbyListChanged);
        lobbyManager.ConnectedLobbyChanged.RemoveListener(OnLobbyIdChanged);
        lobbyManager.PlayersChanged.RemoveListener(OnPlayerListChanged);
    }

    // setup references
    private void SetUpLobbyListViewReferences()
    {
        // list view buttons
        signInButton = lobbyListDocument.rootVisualElement.Q("sign-in") as Button;
        signOutButton = lobbyListDocument.rootVisualElement.Q("sign-out") as Button;
        refreshButton = lobbyListDocument.rootVisualElement.Q("refresh") as Button;
        createLobbyButton = lobbyListDocument.rootVisualElement.Q("create-lobby") as Button;
        joinLobbyButton = lobbyListDocument.rootVisualElement.Q("join-lobby") as Button;

        // list view fields
        playerIdLabel = lobbyListDocument.rootVisualElement.Q("player-id") as Label;
        joinLobbyTextField = lobbyListDocument.rootVisualElement.Q("lobby-id-text-field") as TextField;
        lobbyListTextField = lobbyListDocument.rootVisualElement.Q("lobby-list") as TextField;
    }
    private void SetupLobbyViewReferences()
    {
        // lobby view buttons
        startButton = lobbyDocument.rootVisualElement.Q("start") as Button;
        readyButton = lobbyDocument.rootVisualElement.Q("ready") as Button;
        closeButton = lobbyDocument.rootVisualElement.Q("close") as Button;
        leaveButton = lobbyDocument.rootVisualElement.Q("leave") as Button;

        // lobby view fields
        lobbyIdLabel = lobbyDocument.rootVisualElement.Q("lobby-id-label") as Label;
        playerList = lobbyDocument.rootVisualElement.Q("player-list");
        colorPickerDropdown = lobbyDocument.rootVisualElement.Q("color-picker") as DropdownField;
    }

    // subscribe and unsubscribe listeners
    public void SubscribeLobbyListViewElements()
    {
        // list view buttons
        signInButton.clicked += SignIn;
        signOutButton.clicked += SignOut;
        refreshButton.clicked += Refresh;
        createLobbyButton.clicked += CreateLobby;
        joinLobbyButton.clicked += JoinLobby;
        // list view fields
        joinLobbyTextField.RegisterCallback<ChangeEvent<string>>(UpdateTypedInLobbyId);
    }
    public void UnsubscribeLobbyListViewElements()
    {
        // list view buttons
        signInButton.clicked -= SignIn;
        signOutButton.clicked -= SignOut;
        refreshButton.clicked -= Refresh;
        createLobbyButton.clicked -= CreateLobby;
        joinLobbyButton.clicked -= JoinLobby;
        // list view fields
        joinLobbyTextField.UnregisterCallback<ChangeEvent<string>>(UpdateTypedInLobbyId);
    }
    public void SubscribeLobbyViewElements()
    {
        // lobby view buttons
        startButton.clicked += StartGame;
        readyButton.clicked += Ready;
        closeButton.clicked += Close;
        leaveButton.clicked += Leave;
    }
    public void UnsubscribeLobbyViewElements()
    {
        // lobby view buttons
        startButton.clicked -= StartGame;
        readyButton.clicked -= Ready;
        closeButton.clicked -= Close;
        leaveButton.clicked -= Leave;
    }

    private void SetupDropdown()
    {
        colorPickerDropdown.choices = new List<string> { "Red", "Green", "Blue", "Yellow" };
        colorPickerDropdown.RegisterValueChangedCallback(evt =>
        {
            lobbyManager.UpdatePlayerColor(evt.newValue);
        });
    }

    // listeners

    private void OnLobbyStateChanged(LobbyState state)
    {
        // set visibility of lobby list and lobby view
        switch (state)
        {
            case LobbyState.SignedOut:
            case LobbyState.SignedIn:
                lobbyListDocument.rootVisualElement.visible = true;
                lobbyDocument.rootVisualElement.visible = false;

                break;
            case LobbyState.Host:
            case LobbyState.Client:
                lobbyListDocument.rootVisualElement.visible = false;
                lobbyDocument.rootVisualElement.visible = true;

                break;
        }
        // set listeners for lobby list and lobby view
        if ((state == LobbyState.SignedOut || state == LobbyState.SignedIn) && !lobbyListViewListenersSet)
        {
            SubscribeLobbyListViewElements();
            lobbyListViewListenersSet = true;
            lobbyViewListenersSet = false;
        }
        if ((state == LobbyState.Host || state == LobbyState.Client) && !lobbyViewListenersSet)
        {
            SubscribeLobbyViewElements();
            lobbyViewListenersSet = true;
            lobbyListViewListenersSet = false;
        }
        // set button states
        switch (state)
        {
            case LobbyState.SignedOut:
                signInButton.SetEnabled(true);
                signOutButton.SetEnabled(false);
                refreshButton.SetEnabled(false);
                createLobbyButton.SetEnabled(false);
                joinLobbyButton.SetEnabled(false);
                break;
            case LobbyState.SignedIn:
                signInButton.SetEnabled(false);
                signOutButton.SetEnabled(true);
                refreshButton.SetEnabled(true);
                createLobbyButton.SetEnabled(true);
                joinLobbyButton.SetEnabled(true);
                break;
            case LobbyState.Host:
                startButton.SetEnabled(true);
                readyButton.SetEnabled(false);
                closeButton.SetEnabled(true);
                leaveButton.SetEnabled(false);
                break;
            case LobbyState.Client:
                startButton.SetEnabled(false);
                readyButton.SetEnabled(true);
                closeButton.SetEnabled(false);
                leaveButton.SetEnabled(true);
                break;
        }
    }
    private void OnPlayerIdChanged(string playerId)
    {
        playerIdLabel.text = playerId;
    }
    private void OnLobbyListChanged(Dictionary<string, string> lobbies)
    {
        lobbyListTextField.value = "";
        foreach (var lobby in lobbies)
        {
            lobbyListTextField.value += $"{lobby.Key}: {lobby.Value}\n";
        }
    }
    private void OnLobbyIdChanged(string connectedLobbyId)
    {
        lobbyIdLabel.text = connectedLobbyId;
    }
    private void OnPlayerListChanged(List<Player> players)
    {
        Debug.Log("Updating player list");
        playerList.Clear();
        foreach (var player in players)
        {
            var playerElement = playerListElementTemplate.CloneTree();
            playerElement.Q<Label>("id").text = player.Id;
            playerElement.Q<Label>("color").text = player.Data.ContainsKey("color") ? player.Data["color"].Value : "Not set";
            playerList.Add(playerElement);
        }
    }
    private void UpdateTypedInLobbyId(ChangeEvent<string> evt)
    {
        typedInLobbyCode = evt.newValue;
    }

    // List view functions

    private void SignIn()
    {
        lobbyManager.SignIn();
    }
    private void SignOut()
    {
        lobbyManager.SignOut();
    }
    private void Refresh()
    {
        lobbyManager.RefreshLobbies();
    }
    private void CreateLobby()
    {
        lobbyManager.HostLobby();
    }
    private void JoinLobby()
    {
        if (typedInLobbyCode != "")
        {
            lobbyManager.JoinLobby(typedInLobbyCode);
        }
        else
        {
            Debug.LogWarning("No lobby code typed in");
        }
    }

    // Lobby view functions

    private void StartGame()
    {
    }
    private void Ready()
    {
    }
    private void Close()
    {
        lobbyManager.CloseLobby();
    }
    private void Leave()
    {
        lobbyManager.LeaveLobby();
    }
}
