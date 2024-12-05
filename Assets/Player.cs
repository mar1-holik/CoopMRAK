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

    private HealthUI healthUI; // Ссылка на HealthUI для отображения здоровья

    [Networked] private TickTimer delay { get; set; } // Таймер задержки между выстрелами
    [Networked] public int CurrentLives { get; private set; } = 3; // Количество жизней
    [Networked] private TickTimer respawnProtectionTimer { get; set; } // Таймер защиты после возрождения

    private NetworkCharacterController _cc; // Компонент управления персонажем
    private Vector3 _forward; // Текущее направление персонажа

    private void Awake()
    {
        _cc = GetComponent<NetworkCharacterController>();
        _forward = transform.forward;

        // Находим HealthUI на сцене
        healthUI = FindObjectOfType<HealthUI>();
        if (healthUI == null)
        {
            Debug.LogError("HealthUI не найден. Убедитесь, что объект с этим компонентом присутствует на сцене.");
        }

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
            data.direction.Normalize();

            _cc.Move(5 * data.direction * Runner.DeltaTime);

            if (data.direction.sqrMagnitude > 0)
            {
                _forward = data.direction;
            }

            if (data.buttons.IsSet(NetworkInputData.MOUSEBUTTON0) && delay.ExpiredOrNotRunning(Runner))
            {
                if (_animator != null)
                {
                    _animator.SetBool("isShooting", true);
                }

                delay = TickTimer.CreateFromSeconds(Runner, shootCooldown);

                StartCoroutine(SpawnBallWithDelay());
            }
            else
            {
                if (_animator != null && _animator.GetBool("isShooting"))
                {
                    _animator.SetBool("isShooting", false);
                }

                if (_animator != null)
                {
                    _animator.SetBool("isRunning", data.direction.sqrMagnitude > 0);
                }
            }
        }
    }

    private IEnumerator SpawnBallWithDelay()
    {
        yield return new WaitForSeconds(shootDelay);

        Runner.Spawn(_prefabBall,
            transform.position + _forward,
            Quaternion.LookRotation(_forward),
            Object.InputAuthority, (runner, o) =>
            {
                o.GetComponent<Ball>().SetSpeed(bulletSpeed);
            });

        if (_animator != null)
        {
            _animator.SetBool("isShooting", false);
        }
    }

    public void HandleDeathZone()
    {
        if (HasStateAuthority)
        {
            if (CurrentLives > 0)
            {
                CurrentLives--;
                Debug.Log($"Игрок {Object.InputAuthority} потерял жизнь. Осталось жизней: {CurrentLives}");

                // Если healthUI ещё не найден, ищем его
                if (healthUI == null)
                {
                    healthUI = FindObjectOfType<HealthUI>();
                }

                if (healthUI != null)
                {
                    healthUI.DecreaseHealth();
                }
                else
                {
                    Debug.LogError("HealthUI не найден. Убедитесь, что он присутствует на сцене.");
                }

                Respawn();
            }
            else
            {
                Debug.Log($"Игрок {Object.InputAuthority} потерял все жизни.");
                Runner.Despawn(Object);
            }
        }
    }

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

        BasicSpawner spawner = FindObjectOfType<BasicSpawner>();
        if (spawner != null)
        {
            Transform spawnPoint = spawner.GetSpawnPointForPlayer(Object.InputAuthority);

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
        }
        else
        {
            Debug.LogError("BasicSpawner не найден на сцене.");
        }

        _cc.Velocity = Vector3.zero;
        yield return null;

        _cc.enabled = true;

        respawnProtectionTimer = TickTimer.CreateFromSeconds(Runner, 1.0f);
        Debug.Log("Активирована защита после респавна");
    }
}
