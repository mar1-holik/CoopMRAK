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
    private Animator _animator; // Ссылка на аниматор

    private void Awake()
    {
        _cc = GetComponent<NetworkCharacterController>();
        _forward = transform.forward;
        _characterController = GetComponent<CharacterController>();

        // Получаем Animator с дочернего объекта
        _animator = GetComponentInChildren<Animator>();

        if (_animator == null)
        {
            Debug.LogError("Animator не найден на объекте или его дочернем объекте!");
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData data))
        {
            // Движение игрока
            data.direction.Normalize();
            _cc.Move(5 * data.direction * Runner.DeltaTime);

            // Проверка, если игрок двигается
            if (data.direction.sqrMagnitude > 0)
            {
                _forward = data.direction;

                // Переключаем анимацию в зависимости от движения
                if (_animator != null)
                {
                    _animator.SetBool("isRunning", true);
                }
            }
            else
            {
                // Переключаем на анимацию стояния
                if (_animator != null)
                {
                    _animator.SetBool("isRunning", false);                  
                }
            }

            // Стрельба
            if (HasStateAuthority && delay.ExpiredOrNotRunning(Runner))
            {
                if (data.buttons.IsSet(NetworkInputData.MOUSEBUTTON0))
                {
                    // Включаем анимацию выстрела
                    if (_animator != null)
                    {
                        _animator.SetBool("isShooting", true); // Включаем анимацию выстрела
                    }

                    delay = TickTimer.CreateFromSeconds(Runner, 0.5f); // Устанавливаем задержку между выстрелами

                    // Спавним шарик
                    Runner.Spawn(_prefabBall,
                    transform.position + _forward, Quaternion.LookRotation(_forward),
                    Object.InputAuthority, (runner, o) =>
                    {
                        o.GetComponent<Ball>().Init();
                    });
                }
                else
                {
                    // Если кнопка выстрела не нажата, сбрасываем анимацию выстрела
                    if (_animator != null && _animator.GetBool("isShooting"))
                    {
                        _animator.SetBool("isShooting", false); // Сбрасываем анимацию выстрела
                    }
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
