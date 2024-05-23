using UnityEngine;
using Unity.Netcode;

public class CameraController : MonoBehaviour
{
    NetworkObject player;

    private void Update()
    {
        if (!player)
        {
            player = NetworkManager.Singleton.LocalClient.PlayerObject;
            transform.parent = player.transform;
            transform.localPosition = new Vector3(0, 3.0f, -7.0f);
        }
    }
}
