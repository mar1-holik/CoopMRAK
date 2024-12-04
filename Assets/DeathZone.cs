using Fusion;
using UnityEngine;

public class DeathZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out Player player))
        {
            if (!player.IsProtected()) // Проверяем, есть ли защита
            {
                Debug.Log($"Игрок {player.name} попал в зону смерти.");
                player.HandleDeathZone(); // Уменьшаем жизни и респавним игрока
            }
            else
            {
                Debug.Log($"Игрок {player.name} защищён. Смерть не засчитана.");
            }
        }
    }

}
