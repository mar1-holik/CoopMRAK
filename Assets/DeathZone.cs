using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class DeathZone : MonoBehaviour
{
    private const int MaxLives = 3; // Максимальное количество жизней

    private void OnTriggerEnter(Collider other)
    {
        // Проверяем, является ли объект игроком
        if (other.TryGetComponent(out Player player))
        {
            // Уменьшаем количество жизней игрока
            player.DecreaseLife();

            if (player.CurrentLives <= 0)
            {
                // Удаляем игрока из игры
                Debug.Log($"{player.name} навсегда устранён!");
                player.RemoveFromGame();
            }
            else
            {
                // Респавним игрока
                Debug.Log($"{player.name} умер. Оставшиеся жизни: {player.CurrentLives}");
                player.Respawn();
            }
        }
    }
}
