using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class RelayManager
{
    public static async Task<string> CreateRelay(int numberOfPlayers = 3) // 4 players - 1 host
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(numberOfPlayers); // 4 players - 1 host

            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            RelayServerData relayServerData = new RelayServerData(allocation, "dtls");

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            return joinCode;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError(e);
            return null;
        }
    }

    public static async Task<bool> JoinRelay(string joinCode)
    {
        try
        {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            RelayServerData relayServerData = new RelayServerData(joinAllocation, "dtls");

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
            return true;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError(e);
            return false;
        }
    }

    public static void StartHost()
    {
        try
        {
            NetworkManager.Singleton.StartHost();
        }
        catch (System.Exception e)
        {
            Debug.LogError(e);
        }
    }

    public static void StartClient()
    {
        try
        {
            NetworkManager.Singleton.StartClient();
        }
        catch (System.Exception e)
        {
            Debug.LogError(e);
        }
    }

    public static void ShutDown()
    {
        try
        {
            NetworkManager.Singleton.Shutdown();
        }
        catch (System.Exception e)
        {
            Debug.LogError(e);
        }
    }
}
