using Fusion;
using UnityEngine;

public class DeathZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Проверяем, является ли объект игроком
        if (other.TryGetComponent(out Player player))
        {
            // Проверяем, что игрок не защищён
            if (!player.IsProtected())
            {
                Debug.Log($"Игрок {player.name} попал в зону смерти.");
                player.HandleDeathZone(); // Уменьшаем жизни и перемещаем игрока в начальную точку
            }
            else
            {
                Debug.Log($"Игрок {player.name} временно защищён. Смерть не засчитана.");
            }
        }
    }
}
