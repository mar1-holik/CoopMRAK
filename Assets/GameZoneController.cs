using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class GameZoneController : MonoBehaviour
{
    [SerializeField] private Collider gameZone; // Коллайдер зоны

    private Dictionary<PlayerRef, NetworkObject> players = new Dictionary<PlayerRef, NetworkObject>();

    // Добавляем игрока в отслеживаемый список
    public void AddPlayer(PlayerRef player, NetworkObject playerObject)
    {
        if (!players.ContainsKey(player))
        {
            players.Add(player, playerObject);
        }
    }

    // Удаляем игрока из отслеживаемого списка (например, если он покинул игру)
    public void RemovePlayer(PlayerRef player)
    {
        if (players.ContainsKey(player))
        {
            players.Remove(player);
        }
    }

    private void Update()
    {
        CheckPlayersInZone();
    }

    private void CheckPlayersInZone()
    {
        foreach (var kvp in new Dictionary<PlayerRef, NetworkObject>(players))
        {
            PlayerRef player = kvp.Key;
            NetworkObject playerObject = kvp.Value;

            // Проверяем, находится ли игрок в зоне
            if (!gameZone.bounds.Contains(playerObject.transform.position))
            {
                Debug.Log($"Player {player.PlayerId} вышел за пределы зоны и проиграл!");
                HandlePlayerLoss(player);
            }
        }
    }

    private void HandlePlayerLoss(PlayerRef player)
    {
        if (players.TryGetValue(player, out NetworkObject playerObject))
        {
            // Получаем компонент NetworkRunner у объекта игрока
            NetworkRunner runner = playerObject.GetComponent<NetworkObject>().Runner;

            // Проверяем, является ли текущий экземпляр сервером
            if (runner != null && runner.IsServer)
            {
                // Обрабатываем проигрыш: удаляем игрока
                runner.Despawn(playerObject);
            }

            // Убираем игрока из списка
            players.Remove(player);
        }
    }
}
