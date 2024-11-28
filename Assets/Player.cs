using Fusion;
using UnityEngine;

public class Player : NetworkBehaviour
{
    [Networked] public int CurrentLives { get; private set; } = 3; // Количество жизней
    [Networked] private TickTimer respawnProtectionTimer { get; set; } // Таймер защиты после перемещения

    private NetworkCharacterController _cc;

    private void Awake()
    {
        _cc = GetComponent<NetworkCharacterController>();
    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData data))
        {
            // Движение игрока
            data.direction.Normalize();
            _cc.Move(5 * data.direction * Runner.DeltaTime);
        }
    }

    // Уменьшение жизней и перемещение в начальную точку
    public void HandleDeathZone()
    {
        if (HasStateAuthority)
        {
            // Уменьшаем жизни
            CurrentLives--;
            Debug.Log($"Жизни игрока уменьшены до: {CurrentLives}");

            if (CurrentLives <= 0)
            {
                RemoveFromGame(); // Удаляем игрока из игры
            }
            else
            {
                MoveToStartPoint(); // Перемещаем игрока в начальную точку
            }
        }
    }

    // Перемещение в начальную точку
    private void MoveToStartPoint()
    {
        Debug.Log("Игрок перемещается в начальную точку...");

        // Если есть Rigidbody, корректно обрабатываем перемещение
        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true; // Отключаем физику временно
            rb.position = new Vector3(0, 3, 0); // Устанавливаем позицию
            rb.velocity = Vector3.zero; // Сбрасываем скорость
            rb.isKinematic = false; // Включаем физику обратно
        }
        else
        {
            transform.position = new Vector3(0, 3, 0); // Просто перемещаем объект
        }

        // Устанавливаем защиту на 1 секунду, чтобы избежать повторного попадания в зону смерти
        respawnProtectionTimer = TickTimer.CreateFromSeconds(Runner, 1.0f);
    }

    // Удаление игрока из игры
    private void RemoveFromGame()
    {
        Debug.Log("Игрок удалён из игры.");
        Runner.Despawn(Object); // Удаляем игрока из игры
    }

    // Проверка на защиту
    public bool IsProtected()
    {
        return respawnProtectionTimer.IsRunning;
    }
}
