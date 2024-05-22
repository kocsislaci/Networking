using Unity.Netcode;
using Unity.Networking.Transport.Error;
using UnityEngine;
using UnityEngine.UI;


enum UIState {
    Disconnected,
    Connected,
}

public class UIManager : MonoBehaviour
{
    [SerializeField] private Button hostButton;
    [SerializeField] private Button serverButton;
    [SerializeField] private Button clientButton;
    [SerializeField] private Button disconnectButton;

    private UIState uiState;
    internal UIState UIState
    {
        get => uiState;
        set
        {
            switch (value)
            {
                case UIState.Disconnected:
                    hostButton.gameObject.SetActive(true);
                    serverButton.gameObject.SetActive(true);
                    clientButton.gameObject.SetActive(true);
                    disconnectButton.gameObject.SetActive(false);
                    break;
                case UIState.Connected:
                    hostButton.gameObject.SetActive(false);
                    serverButton.gameObject.SetActive(false);
                    clientButton.gameObject.SetActive(false);
                    disconnectButton.gameObject.SetActive(true);
                    break;
                default:
                    break;
            }
            uiState = value;
        }
    }

    void Awake()
    {
        UIState = UIState.Disconnected;

        hostButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartHost();
            UIState = UIState.Connected;
        });
        serverButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartServer();
            UIState = UIState.Connected;
        });
        clientButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartClient();
            UIState = UIState.Connected;
        });
        disconnectButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.Shutdown();
            UIState = UIState.Disconnected;
        });
    }
}
