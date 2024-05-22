using Unity.Netcode;
using UnityEngine;

public class BulletController : NetworkBehaviour
{

    [SerializeField] private float speed;

    public override void OnNetworkSpawn() {
        if (IsServer) {
            GetComponent<Rigidbody>().velocity = transform.forward * speed;
        }
    }

    void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            Debug.Log("Hit player");
        }
        GetComponent<NetworkObject>().Despawn();
    }
}
