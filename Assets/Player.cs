using Fusion;
using UnityEngine;
using System.Collections;

public class Player : NetworkBehaviour
{
    [SerializeField] private Ball _prefabBall;

    [Networked] private TickTimer delay { get; set; }
    [Networked] public int CurrentLives { get; private set; } = 3; // Количество жизней
    [Networked] private TickTimer respawnProtectionTimer { get; set; } // Таймер защиты после возрождения

    private NetworkCharacterController _cc;
    private Vector3 _forward;

    private CharacterController _characterController;

    private void Awake()
    {
        _cc = GetComponent<NetworkCharacterController>();
        _forward = transform.forward;

        _characterController = GetComponent<CharacterController>();
    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData data))
        {
            // Движение игрока
            data.direction.Normalize();
            _cc.Move(5 * data.direction * Runner.DeltaTime);

            if (data.direction.sqrMagnitude > 0)
                _forward = data.direction;

            // Стрельба
            if (HasStateAuthority && delay.ExpiredOrNotRunning(Runner))
            {
                if (data.buttons.IsSet(NetworkInputData.MOUSEBUTTON0))
                {
                    delay = TickTimer.CreateFromSeconds(Runner, 0.5f);
                    Runner.Spawn(_prefabBall,
                    transform.position + _forward, Quaternion.LookRotation(_forward),
                    Object.InputAuthority, (runner, o) =>
                    {
                        // Инициализируем шар до его синхронизации
                        o.GetComponent<Ball>().Init();
                    });
                }
            }
        }
    }

    // Метод для обработки попадания в зону смерти
    public void HandleDeathZone()
    {
        if (HasStateAuthority)
        {
            if (CurrentLives > 0)
            {
                CurrentLives--;
                Debug.Log($"Игрок {Object.InputAuthority} потерял жизнь. Осталось жизней: {CurrentLives}");
                Respawn();
            }
            else
            {
                Debug.Log($"Игрок {Object.InputAuthority} потерял все жизни.");
                RemoveFromGame();
            }
        }
    }

    // Метод для возрождения игрока
    private void Respawn()
    {
        Debug.Log("Игрок возрождается на начальной позиции...");
        StartCoroutine(RespawnCoroutine());
    }

    private IEnumerator RespawnCoroutine()
    {
        // Disable movement temporarily if necessary (optional)
        _cc.enabled = false; // Only disable if needed, `_cc` handles its own logic

        // Set respawn position using Teleport
        Vector3 respawnPosition = new Vector3(0, 10, 0); // Example position
        Debug.Log($"Teleporting to respawn position: {respawnPosition}");
        _cc.Teleport(respawnPosition, Quaternion.identity);

        // Reset velocity
        _cc.Velocity = Vector3.zero;

        // Wait a frame to ensure the position is applied
        yield return null;

        // Enable movement again if necessary
        _cc.enabled = true;

        // Activate respawn protection
        respawnProtectionTimer = TickTimer.CreateFromSeconds(Runner, 1.0f);
        Debug.Log("Respawn protection activated");
    }



    // Удаление игрока из игры
    private void RemoveFromGame()
    {
        Debug.Log($"Игрок {Object.InputAuthority} удалён из игры.");
        Runner.Despawn(Object); // Удаляем сетевой объект игрока
    }

    // Проверка на защиту после возрождения
    public bool IsProtected()
    {
        return respawnProtectionTimer.ExpiredOrNotRunning(Runner) == false;
    }
}
