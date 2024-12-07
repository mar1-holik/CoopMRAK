using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System;
using UnityEngine.SceneManagement;
using System.Linq;

public class BasicSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    private NetworkRunner _runner;
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private NetworkPrefabRef[] playerPrefabs; // Массив префабов игроков
    [SerializeField] private Transform[] spawnPoints; // Массив точек для спавна
    [SerializeField] private int maxPlayers = 4; // Максимальное количество игроков

    private Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();
    private Dictionary<PlayerRef, Transform> _playerSpawnPoints = new Dictionary<PlayerRef, Transform>();

    async void StartGame(GameMode mode)
    {
        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;

        var scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);
        await _runner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = "TestRoom",
            Scene = scene,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });
    }

    public void StartHost()
    {
        if (_runner == null)
        {
            StartGame(GameMode.Host);
            CloseMenu();
        }
    }

    public void StartClient()
    {
        if (_runner == null)
        {
            StartGame(GameMode.Client);
            CloseMenu();
        }
    }

    private void CloseMenu()
    {
        if (menuPanel != null)
        {
            menuPanel.SetActive(false);
        }
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer)
        {
            // Проверяем количество игроков в комнате
            if (runner.ActivePlayers.Count() >= maxPlayers)
            {
                Debug.LogWarning($"Player {player} tried to join, but the room is full!");
                runner.Disconnect(player); // Отклоняем подключение
                return;
            }

            // Выбор точки спавна
            int spawnIndex = _spawnedCharacters.Count % spawnPoints.Length;
            Transform spawnPoint = spawnPoints[spawnIndex];

            // Сохраняем точку спавна для игрока
            if (!_playerSpawnPoints.ContainsKey(player))
            {
                _playerSpawnPoints[player] = spawnPoint;
            }

            // Выбор префаба игрока
            int prefabIndex = _spawnedCharacters.Count % playerPrefabs.Length;
            NetworkPrefabRef selectedPrefab = playerPrefabs[prefabIndex];

            // Спавн объекта игрока
            NetworkObject networkPlayerObject = runner.Spawn(
                selectedPrefab,
                spawnPoint.position,
                spawnPoint.rotation,
                player
            );

            // Добавляем игрока в словарь
            _spawnedCharacters.Add(player, networkPlayerObject);

            Debug.Log($"Игрок {player.PlayerId} заспавнен. Точка спавна: {spawnPoint.position}");
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (_spawnedCharacters.TryGetValue(player, out NetworkObject networkObject))
        {
            runner.Despawn(networkObject);
            _spawnedCharacters.Remove(player);
        }

        // Удаляем точку спавна игрока
        if (_playerSpawnPoints.ContainsKey(player))
        {
            _playerSpawnPoints.Remove(player);
        }

        Debug.Log($"Игрок {player.PlayerId} покинул игру.");
    }

    public Transform GetSpawnPointForPlayer(PlayerRef player)
    {
        if (_playerSpawnPoints.TryGetValue(player, out Transform spawnPoint))
        {
            return spawnPoint;
        }
        return null;
    }

    private void FixedUpdate()
    {
        _mouseButton0 = _mouseButton0 || Input.GetMouseButton(0);
    }

    // Реализация ввода
    private bool _mouseButton0;

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var data = new NetworkInputData();

        if (Input.GetKey(KeyCode.W))
            data.direction += Vector3.forward;

        if (Input.GetKey(KeyCode.S))
            data.direction += Vector3.back;

        if (Input.GetKey(KeyCode.A))
            data.direction += Vector3.left;

        if (Input.GetKey(KeyCode.D))
            data.direction += Vector3.right;

        data.buttons.Set(NetworkInputData.MOUSEBUTTON0, _mouseButton0);
        _mouseButton0 = false;

        input.Set(data);
    }

    // Пустые реализации интерфейса
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
}
