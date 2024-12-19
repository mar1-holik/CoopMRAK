using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Fusion;

public class ReadyManager : MonoBehaviour
{
    [SerializeField] private GameObject readyCanvas; // Ссылка на Canvas Ready
    [SerializeField] private TextMeshProUGUI statusText; // Текст для отображения статуса игроков
    [SerializeField] private GameObject readyButton; // Кнопка Ready

    private Dictionary<PlayerRef, bool> playerReadyStatus = new Dictionary<PlayerRef, bool>(); // Статус готовности игроков
    private NetworkRunner _runner;
    private bool isReady = false; // Локальный статус готовности

    public void SetRunner(NetworkRunner runner)
    {
        _runner = runner;
    }

    public void OnReadyClicked()
    {
        isReady = !isReady;
        readyButton.GetComponentInChildren<TextMeshProUGUI>().text = isReady ? "Unready" : "Ready";
        RPC_SetPlayerReady(_runner.LocalPlayer, isReady);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SetPlayerReady(PlayerRef player, bool ready)
    {
        playerReadyStatus[player] = ready;

        // Обновляем статус UI
        UpdateStatusText();

        // Проверяем, готовы ли все игроки
        CheckAllPlayersReady();
    }

    public bool AreAllPlayersReady()
    {
        foreach (var status in playerReadyStatus.Values)
        {
            if (!status) return false;
        }
        return true;
    }

    private void UpdateStatusText()
    {
        string status = "";
        foreach (var player in playerReadyStatus)
        {
            status += $"Player {player.Key.PlayerId}: {(player.Value ? "Unready" : "Ready")}\n";
        }
        statusText.text = status;
    }

    private void CheckAllPlayersReady()
    {
        if (_runner == null || playerReadyStatus.Count < 2) return; // Не запускаем игру, если игроков меньше двух

        foreach (var ready in playerReadyStatus.Values)
        {
            if (!ready) return; // Если кто-то не готов, выходим
        }

        // Все игроки готовы
        StartGame();
    }

    private void StartGame()
    {
        readyCanvas.SetActive(false);
        Debug.Log("Игра началась!");
        // Здесь можно разблокировать управление игроками
    }

    public void OnPlayerJoined(PlayerRef player)
    {
        if (!playerReadyStatus.ContainsKey(player))
        {
            playerReadyStatus[player] = false; // Новый игрок по умолчанию не готов
        }

        UpdateStatusText();
    }

    public void OnPlayerLeft(PlayerRef player)
    {
        if (playerReadyStatus.ContainsKey(player))
        {
            playerReadyStatus.Remove(player); // Удаляем игрока из списка
        }

        UpdateStatusText();
    }
}