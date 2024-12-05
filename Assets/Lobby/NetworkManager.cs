using System.Threading.Tasks;
using UnityEngine;
using Fusion;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }
    private NetworkRunner networkRunner;

    [Header("Game Settings")]
    public string roomName = "DefaultRoom"; // Имя комнаты
    public GameMode gameMode = GameMode.Host; // Режим игры
    public int maxPlayers = 4; // Максимальное количество игроков

    private void Awake()
    {
        // Singleton Pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Убедитесь, что NetworkRunner создан
        if (networkRunner == null)
        {
            networkRunner = gameObject.AddComponent<NetworkRunner>();
        }
    }

    public async void StartHost()
    {
        if (networkRunner == null)
        {
            Debug.LogError("NetworkRunner is not initialized!");
            return;
        }

        // Очищаем старые NetworkRunner
        CleanUpNetworkRunners();

        Debug.Log("Creating room...");

        var result = await networkRunner.StartGame(new StartGameArgs
        {
            GameMode = GameMode.Host,
            SessionName = roomName,
            PlayerCount = maxPlayers
        });

        if (result.Ok)
        {
            Debug.Log($"Room '{roomName}' created successfully.");
        }
        else
        {
            Debug.LogError($"Failed to create room: {result.ShutdownReason}");
        }
    }

    public async void JoinGame()
    {
        if (networkRunner == null)
        {
            Debug.LogError("NetworkRunner is not initialized!");
            return;
        }

        Debug.Log("Joining room...");

        var result = await networkRunner.StartGame(new StartGameArgs
        {
            GameMode = GameMode.Client,
            SessionName = roomName
        });

        if (result.Ok)
        {
            Debug.Log($"Successfully joined room '{roomName}'.");
        }
        else if (result.ShutdownReason == ShutdownReason.GameNotFound)
        {
            Debug.LogError("Room not found.");
        }
        else
        {
            Debug.LogError($"Failed to join room: {result.ShutdownReason}");
        }
    }

    private void CleanUpNetworkRunners()
    {
        var runners = FindObjectsOfType<NetworkRunner>();
        foreach (var runner in runners)
        {
            if (runner != networkRunner)
            {
                Destroy(runner.gameObject);
            }
        }
    }
}
