using Fusion;
using UnityEngine;
using System.Collections;

public class Player : NetworkBehaviour
{
    [SerializeField] private Ball _prefabBall; // Префаб снаряда
    [SerializeField] private Animator _animator; // Аниматор
    [SerializeField] private float shootDelay = 0.4f; // Задержка перед выстрелом
    [SerializeField] private float shootCooldown = 1.3f; // Время между выстрелами
    [SerializeField] private float bulletSpeed = 10f; // Скорость снаряда
    [SerializeField] private HealthUI healthUI; // Ссылка на HealthUI

    [Networked] public int CurrentLives { get; private set; } = 3; // Синхронизируемое количество жизней
    [Networked] private TickTimer delay { get; set; } // Таймер между выстрелами
    [Networked] private TickTimer respawnProtectionTimer { get; set; } // Таймер защиты после респавна

    private NetworkCharacterController _cc; // Компонент для движения
    private Vector3 _forward; // Направление движения
    public bool isRespawning = false; // Флаг респавна
    private bool isShooting = false; // Флаг выстрела
    private NetworkCharacterController _controller;

    private void Awake()
    {
        _cc = GetComponent<NetworkCharacterController>();
        _forward = transform.forward;

        if (healthUI == null)
        {
            Debug.LogError("HealthUI не привязан! Проверьте настройки в инспекторе.");
        }

        if (_animator == null)
        {
            _animator = GetComponentInChildren<Animator>();
        }

        if (_animator == null)
        {
            Debug.LogWarning("Animator не найден! Убедитесь, что он установлен в инспекторе.");
        }

        _controller = GetComponent<NetworkCharacterController>();
        if (_controller == null)
        {
            Debug.LogError($"{name}: NetworkCharacterController не найден!");
        }
    }

    public override void Spawned()
    {
        base.Spawned();

        if (HasStateAuthority)
        {
            CurrentLives = 3; // Устанавливаем начальное количество жизней

            if (healthUI != null)
            {
                healthUI.InitializeHealth(CurrentLives); // Инициализация UI здоровья
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (_controller == null || !_controller.enabled) return; // Не выполняем логику движения, если управление отключено

        if (GetInput(out NetworkInputData data))
        {
            data.direction.Normalize();
            _cc.Move(5 * data.direction * Runner.DeltaTime);

            if (data.direction.sqrMagnitude > 0)
            {
                _forward = data.direction;
            }

            if (Object.HasInputAuthority) // Проверяем права перед отправкой RPC
            {
                if (data.buttons.IsSet(NetworkInputData.MOUSEBUTTON0) && delay.ExpiredOrNotRunning(Runner))
                {
                    if (!isShooting)
                    {
                        RPC_SetAnimation("isShooting", true);
                        isShooting = true;
                    }

                    delay = TickTimer.CreateFromSeconds(Runner, shootCooldown);
                    StartCoroutine(SpawnBallWithDelay());
                }
                else if (isShooting && delay.ExpiredOrNotRunning(Runner))
                {
                    RPC_SetAnimation("isShooting", false);
                    isShooting = false;
                }

                RPC_SetAnimation("isRunning", data.direction.sqrMagnitude > 0);
            }
        }
    }

    private IEnumerator SpawnBallWithDelay()
    {
        yield return new WaitForSeconds(shootDelay);

        if (HasStateAuthority)
        {
            Runner.Spawn(
                _prefabBall,
                transform.position + _forward,
                Quaternion.LookRotation(_forward),
                Object.InputAuthority,
                (runner, o) => { o.GetComponent<Ball>().SetSpeed(bulletSpeed); }
            );
        }
    }

    public void HandleDeathZone()
    {
        if (Object == null) // Проверяем, был ли объект заспавнен
        {
            Debug.LogError("Игрок ещё не заспавнен. Невозможно обработать зону смерти.");
            return;
        }

        if (HasStateAuthority && !isRespawning)
        {
            if (IsProtected())
            {
                Debug.Log("Игрок находится под защитой. Смерть не засчитана.");
                return;
            }

            if (CurrentLives > 0)
            {
                CurrentLives--;
                RPC_UpdateHealthUI(CurrentLives);
            }

            if (CurrentLives > 0)
            {
                Respawn();
            }
            else
            {
                Runner.Despawn(Object);
                var winManager = FindObjectOfType<WinManager>();
                if (winManager != null)
                {
                    winManager.CheckWinner();
                }
                else
                {
                    Debug.LogError("WinManager не найден!");
                }
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
        isRespawning = true;

        _cc.enabled = false;
        Transform spawnPoint = FindObjectOfType<BasicSpawner>().GetSpawnPointForPlayer(Object.InputAuthority);
        RPC_Teleport(spawnPoint.position, spawnPoint.rotation);
        yield return null;

        _cc.enabled = true;
        respawnProtectionTimer = TickTimer.CreateFromSeconds(Runner, 1.0f);

        isRespawning = false;
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_UpdateHealthUI(int lives)
    {
        if (healthUI != null)
        {
            healthUI.UpdateHealth(lives); // Обновляем UI на всех клиентах
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_SetAnimation(string parameter, bool state)
    {
        if (_animator != null)
        {
            _animator.SetBool(parameter, state);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_Teleport(Vector3 position, Quaternion rotation)
    {
        _cc.Teleport(position, rotation);
    }

    public void EnableControls()
    {
        if (_controller != null)
        {
            _controller.enabled = true;
            Debug.Log($"{name}: управление включено.");
        }
        else
        {
            Debug.LogError($"{name}: Не удалось включить управление, так как _controller не инициализирован.");
        }
    }

    public void DisableControls()
    {
        if (_controller != null)
        {
            _controller.enabled = false; // Отключаем движение
            Debug.Log($"{name}: управление отключено.");
        }
        else
        {
            Debug.LogError($"{name}: Не удалось отключить управление, так как _controller не инициализирован.");
        }
    }
}