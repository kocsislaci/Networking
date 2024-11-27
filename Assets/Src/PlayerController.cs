using System;
using CodeMonkey.HealthSystemCM;
using TMPro;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using UnityEngine;

public class PlayerController : NetworkBehaviour, IGetHealthSystem
{
    private NetworkObject networkObject;
    private MeshRenderer[] meshRenderers;
    [SerializeField] private GameObject bulletPrefab;

    [SerializeField] private float maxHealth = 100f;
    private bool bulletShot = false;
    private HealthSystem hs;

    readonly private NetworkVariable<float> health = new NetworkVariable<float>();

    private int deaths = 0;

    private void Awake()
    {
        hs = new HealthSystem(maxHealth);
        hs.OnHealthChanged += OnHealthChangeServer;
        health.OnValueChanged += OnHealthChangeClient;
    }

    private void OnHealthChangeClient(float previousValue, float newValue)
    {
        Debug.Log("Got new value for health");
        if (IsClient)
        {
            hs.SetHealth(newValue);
        }
    }

    private void OnHealthChangeServer(object sender, EventArgs e)
    {

        if (IsServer)
        {
            health.Value = hs.GetHealth();
        }
    }

    public override void OnNetworkSpawn()
    {
        GameObject spawnPoints = GameObject.FindWithTag("SpawnPoints");

        var connectedClients = NetworkManager.Singleton.ConnectedClients;

        int spawnIndex = connectedClients.Count % spawnPoints.transform.childCount;

        Debug.Log("wtf index: " + spawnIndex + " clients " + connectedClients.Count + " spawnpoints " + spawnPoints.transform.childCount);
        Transform spawnPoint = spawnPoints.transform.GetChild(spawnIndex);

        transform.position = spawnPoint.position;

        networkObject = GetComponent<NetworkObject>();
    }

    private void Start()
    {
        meshRenderers = GetComponentsInChildren<MeshRenderer>();
        Array.Resize(ref meshRenderers, meshRenderers.Length + 1);
        meshRenderers[meshRenderers.GetUpperBound(0)] = GetComponent<MeshRenderer>();

        FetchAndSetColor();
    }

    private void FetchAndSetColor()
    {
        var lobbyManager = FindAnyObjectByType<LobbyManager>();
        var playerId = AuthenticationService.Instance.PlayerId;

        var data = lobbyManager.GetPlayerData(playerId);

        var colorData = data["color"];

        var m = PlayerColor.FindColor(lobbyManager.defaultColors, colorData.Value);
        SetColor(m.material);
    }

    private void Update()
    {
        if (!IsOwner) return;

        Move();
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.Mouse0))
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

        Destroy(bullet, 5f);
    }

    public void OnHitServer(float damage, ulong sourcePlayer)
    {
        if (IsServer)
        {
            Debug.Log($"Damaged.");

            hs.Damage(damage);
            if (hs.IsDead())
            {
                Debug.Log($"Thats it, Im dead :( killed by #{sourcePlayer}. I have died #{deaths} times.");
                deaths++;
                hs.SetHealth(maxHealth);
            }
        }
    }

    public HealthSystem GetHealthSystem()
    {
        return hs;
    }

    void SetColor(Material m)
    {
        Debug.Log($"Set Player #{OwnerClientId} color to #{m.name}");
        foreach (var renderer in meshRenderers)
        {
            renderer.materials = new Material[] { m };
        }
    }

}
