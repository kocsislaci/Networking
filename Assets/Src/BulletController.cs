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
            var ctr = other.gameObject.GetComponent<PlayerController>();
            var hs = ctr.GetHealthSystem();
            Debug.Log("Current hp of " + ctr.OwnerClientId + " " + hs.GetHealth());
            hs.Damage(20);
            Debug.Log("After hit of " + ctr.OwnerClientId + " " + hs.GetHealth());
        }
        GetComponent<NetworkObject>().Despawn();
    }
}
