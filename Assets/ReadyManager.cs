using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Fusion;

public class ReadyManager : MonoBehaviour
{
    [SerializeField] private GameObject readyCanvas; // ������ �� Canvas Ready
    [SerializeField] private TextMeshProUGUI statusText; // ����� ��� ����������� ������� �������
    [SerializeField] private GameObject readyButton; // ������ Ready

    private Dictionary<PlayerRef, bool> playerReadyStatus = new Dictionary<PlayerRef, bool>(); // ������ ���������� �������
    private NetworkRunner _runner;
    private bool isReady = false; // ��������� ������ ����������

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

        // ��������� ������ UI
        UpdateStatusText();

        // ���������, ������ �� ��� ������
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
        if (_runner == null || playerReadyStatus.Count < 2) return; // �� ��������� ����, ���� ������� ������ ����

        foreach (var ready in playerReadyStatus.Values)
        {
            if (!ready) return; // ���� ���-�� �� �����, �������
        }

        // ��� ������ ������
        StartGame();
    }

    private void StartGame()
    {
        readyCanvas.SetActive(false);
        Debug.Log("���� ��������!");
        // ����� ����� �������������� ���������� ��������
    }

    public void OnPlayerJoined(PlayerRef player)
    {
        if (!playerReadyStatus.ContainsKey(player))
        {
            playerReadyStatus[player] = false; // ����� ����� �� ��������� �� �����
        }

        UpdateStatusText();
    }

    public void OnPlayerLeft(PlayerRef player)
    {
        if (playerReadyStatus.ContainsKey(player))
        {
            playerReadyStatus.Remove(player); // ������� ������ �� ������
        }

        UpdateStatusText();
    }
}