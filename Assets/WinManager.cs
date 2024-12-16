using UnityEngine;
using TMPro;
using Fusion;

public class WinManager : NetworkBehaviour
{
    [SerializeField] private GameObject winMenuCanvas; // Canvas с фоном и текстом
    [SerializeField] private TMP_Text winMessage; // Текстовое сообщение о победе

    private bool gameEnded = false;

    [Networked] private int WinnerId { get; set; } = -1;
    private int previousWinnerId = -1;

    private NetworkRunner _networkRunner;

    public void SetRunner(NetworkRunner runner)
    {
        if (runner == null)
        {
            Debug.LogError("Переданный NetworkRunner равен null!");
            return;
        }

        _networkRunner = runner;
        Debug.Log("NetworkRunner успешно установлен в WinManager.");
    }

    public void CheckWinner()
    {
        if (gameEnded || _networkRunner == null) // Здесь исправлено
        {
            Debug.LogError("Игра завершена или Runner не инициализирован.");
            return;
        }

        int alivePlayers = 0;
        int winningPlayerId = -1;

        foreach (var player in FindObjectsOfType<Player>())
        {
            if (player.Object == null)
            {
                Debug.LogWarning($"Игрок {player} ещё не заспавнен. Пропускаем.");
                continue;
            }

            if (player.CurrentLives > 0 && !player.isRespawning && !player.IsProtected())
            {
                alivePlayers++;
                winningPlayerId = player.Object.InputAuthority.PlayerId;
            }
        }

        Debug.Log($"Живых игроков: {alivePlayers}");

        if (alivePlayers == 1)
        {
            WinnerId = winningPlayerId;

            // Вызываем RPC для отображения победителя на всех клиентах
            RPC_DisplayWinner(winningPlayerId);
        }
    }

    private void Update()
    {
        if (_networkRunner == null || gameEnded || Object == null) return; // Здесь исправлено

        if (WinnerId != previousWinnerId)
        {
            previousWinnerId = WinnerId;
            DisplayWinner(WinnerId);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_DisplayWinner(int playerId)
    {
        DisplayWinner(playerId);
    }

    private void DisplayWinner(int playerId)
    {
        if (gameEnded) return;

        gameEnded = true;

        // Отключаем управление у всех игроков
        DisableAllPlayerControls();

        if (winMenuCanvas != null)
        {
            winMenuCanvas.SetActive(true); // Активируем Canvas
        }

        if (winMessage != null)
        {
            winMessage.text = playerId >= 0 ? $"Игрок №{playerId} выиграл!" : "Никто не победил!";
            winMessage.gameObject.SetActive(true);
        }

        Debug.Log($"Игрок №{playerId} выиграл!");
    }

    private void DisableAllPlayerControls()
    {
        foreach (var player in FindObjectsOfType<Player>())
        {
            if (player != null)
            {
                Debug.Log($"Отключаем управление у игрока: {player.name}");
                player.DisableControls();
            }
        }
    }
}
