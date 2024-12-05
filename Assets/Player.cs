using Fusion;
using UnityEngine;
using System.Collections;

public class Player : NetworkBehaviour
{
    [SerializeField] private Ball _prefabBall; // Префаб снаряда
    [SerializeField] private Animator _animator; // Аниматор для управления анимациями
    [SerializeField] private float shootDelay = 0.4f; // Задержка перед выстрелом в секундах
    [SerializeField] private float shootCooldown = 1.3f; // Минимальное время между выстрелами в секундах
    [SerializeField] private float bulletSpeed = 10f; // Скорость снаряда

    [Networked] private TickTimer delay { get; set; } // Таймер задержки между выстрелами
    [Networked] public int CurrentLives { get; private set; } = 3; // Количество жизней
    [Networked] private TickTimer respawnProtectionTimer { get; set; } // Таймер защиты после возрождения

    private NetworkCharacterController _cc; // Компонент управления персонажем
    private Vector3 _forward; // Текущее направление персонажа

    private void Awake()
    {
        _cc = GetComponent<NetworkCharacterController>();
        _forward = transform.forward;

        // Инициализируем аниматор
        if (_animator == null)
        {
            _animator = GetComponentInChildren<Animator>();
        }

        if (_animator == null)
        {
            Debug.LogError("Animator не найден! Убедитесь, что он установлен в инспекторе.");
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData data))
        {
            // Нормализуем направление для корректного передвижения
            data.direction.Normalize();

            // Перемещение игрока
            _cc.Move(5 * data.direction * Runner.DeltaTime);

            // Если игрок двигается, обновляем направление стрельбы
            if (data.direction.sqrMagnitude > 0)
            {
                _forward = data.direction; // Сохраняем текущее направление
            }

            // Если игрок стреляет, учитываем задержку между выстрелами
            if (data.buttons.IsSet(NetworkInputData.MOUSEBUTTON0) && delay.ExpiredOrNotRunning(Runner))
            {
                if (_animator != null)
                {
                    // Включаем анимацию стрельбы
                    _animator.SetBool("isShooting", true);
                }

                // Устанавливаем задержку между выстрелами
                delay = TickTimer.CreateFromSeconds(Runner, shootCooldown);

                // Запускаем Coroutine для спавна шара
                StartCoroutine(SpawnBallWithDelay());
            }
            else
            {
                // Останавливаем анимацию стрельбы, если кнопка не нажата
                if (_animator != null && _animator.GetBool("isShooting"))
                {
                    _animator.SetBool("isShooting", false);
                }

                // Если не стреляем, проверяем анимацию бега
                if (_animator != null)
                {
                    _animator.SetBool("isRunning", data.direction.sqrMagnitude > 0);
                }
            }
        }
    }

    private IEnumerator SpawnBallWithDelay()
    {
        // Ждём задержку перед выстрелом
        yield return new WaitForSeconds(shootDelay);

        // Спавн снаряда
        Runner.Spawn(_prefabBall,
            transform.position + _forward, // Начальная позиция снаряда
            Quaternion.LookRotation(_forward), // Направление стрельбы
            Object.InputAuthority, (runner, o) =>
            {
                // Устанавливаем скорость снаряда
                o.GetComponent<Ball>().SetSpeed(bulletSpeed);
            });

        // Завершаем анимацию стрельбы
        if (_animator != null)
        {
            _animator.SetBool("isShooting", false);
        }
    }

    // Метод для обработки попадания в зону смерти
    public void HandleDeathZone()
    {
        if (HasStateAuthority) // Проверяем, является ли этот объект владельцем состояния
        {
            if (CurrentLives > 0)
            {
                CurrentLives--; // Уменьшаем жизни
                Debug.Log($"Игрок {Object.InputAuthority} потерял жизнь. Осталось жизней: {CurrentLives}");
                Respawn(); // Вызываем респавн
            }
            else
            {
                Debug.Log($"Игрок {Object.InputAuthority} потерял все жизни.");
                Runner.Despawn(Object); // Удаляем объект игрока из игры
            }
        }
    }

    // Проверка, находится ли игрок под защитой после респавна
    public bool IsProtected()
    {
        return respawnProtectionTimer.ExpiredOrNotRunning(Runner) == false;
    }

    private void Respawn()
    {
        StartCoroutine(RespawnCoroutine());
    }

    private IEnumerator RespawnCoroutine()
    {
        _cc.enabled = false;

        // Получаем точку спавна
        Transform spawnPoint = FindObjectOfType<BasicSpawner>().GetSpawnPointForPlayer(Object.InputAuthority);

        if (spawnPoint != null)
        {
            Vector3 respawnPosition = spawnPoint.position;
            Quaternion respawnRotation = spawnPoint.rotation;

            Debug.Log($"Респавн игрока на позиции: {respawnPosition}");
            _cc.Teleport(respawnPosition, respawnRotation);
        }
        else
        {
            Debug.LogError("Точка спавна не найдена! Используется стандартная позиция.");
            _cc.Teleport(new Vector3(0, 10, 0), Quaternion.identity);
        }

        _cc.Velocity = Vector3.zero;
        yield return null;

        _cc.enabled = true;

        // Активируем временную защиту
        respawnProtectionTimer = TickTimer.CreateFromSeconds(Runner, 1.0f);
        Debug.Log("Активирована защита после респавна");
    }

}
