using Unity.Netcode;
using UnityEngine;

public class BulletController : NetworkBehaviour
{

    [SerializeField] private float speed;

    private int sourcePlayerId;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            GetComponent<Rigidbody>().velocity = transform.forward * speed;
        }
    }

    void OnCollisionEnter(Collision other)
    {
        if (IsServer)
        {
            if (other.gameObject.CompareTag("Player"))
            {
                Debug.Log("Hit player");
                var player = other.gameObject.GetComponent<PlayerController>();
                player.OnHitServer(5f, 0);
            }
            GetComponent<NetworkObject>().Despawn();
        }
    }
}
