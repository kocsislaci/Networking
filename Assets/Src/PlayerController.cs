using Unity.Netcode;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

public class PlayerController : NetworkBehaviour
{
    private NetworkObject networkObject;
    private MeshRenderer meshRenderer;
    [SerializeField] private GameObject bulletPrefab;
    private bool bulletShot = false;

    [SerializeField] private Material[] playerSkins;

    public override void OnNetworkSpawn()
    {
        networkObject = GetComponent<NetworkObject>();
    }

    private void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        SetColor((uint)OwnerClientId);
    }

    private void Update()
    {
        if (!IsOwner) return;

        Move();
        if (Input.GetKey(KeyCode.Space))
        {
            if (!bulletShot)
            {
                AttackServerRpc();
                bulletShot = true;
            }
        }
        else
        {
            bulletShot = false;
        }
    }

    private void Move()
    {
        float movement = 0f;
        float movementSpeed = 5f;
        float rotation = 0f;
        float rotationSpeed = 80f;

        if (Input.GetKey(KeyCode.W)) movement = +movementSpeed;
        if (Input.GetKey(KeyCode.S)) movement = -movementSpeed;
        if (Input.GetKey(KeyCode.A)) rotation = -rotationSpeed;
        if (Input.GetKey(KeyCode.D)) rotation = +rotationSpeed;
        MoveServerRpc(movement, rotation);
    }

    [ServerRpc]
    private void MoveServerRpc(float movement, float rotation)
    {
        transform.Translate(Vector3.forward * movement * Time.deltaTime);
        transform.RotateAround(transform.position, Vector3.up, rotation * Time.deltaTime);
    }

    [ServerRpc]
    private void AttackServerRpc()
    {
        Debug.Log("Who attacks: " + OwnerClientId);
        GameObject bullet = Instantiate(bulletPrefab, transform.position + transform.forward * 1.3f + new Vector3(0, .5f, 0), transform.rotation);
        bullet.GetComponent<NetworkObject>().Spawn(true);

    }

    void SetColor(long skinId)
    {
        Debug.Log($"Set Player #{OwnerClientId} color to #{skinId}");
        meshRenderer.materials = new Material[] { playerSkins[skinId] };
    }

}
