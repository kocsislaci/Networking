using UnityEngine;
using Unity.Netcode;

public class CameraController : MonoBehaviour
{
    NetworkObject player;

    private void Update()
    {
        if (!player)
        {
            player = NetworkManager.Singleton.LocalClient?.PlayerObject ?? null;
        }
        if (player)
        {
            transform.position = player.transform.position;
            transform.rotation = player.transform.rotation;
        }
    }
}
