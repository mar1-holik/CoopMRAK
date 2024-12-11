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
    private bool isRespawning = false; // Флаг респавна

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
        if (HasStateAuthority && !isRespawning)
        {
            if (IsProtected())
            {
                Debug.Log("Игрок находится под защитой. Смерть не засчитана.");
                return;
            }

            if (CurrentLives > 0)
            {
                CurrentLives--; // Уменьшаем жизни
                RPC_UpdateHealthUI(CurrentLives); // Обновляем UI для всех
            }

            if (CurrentLives > 0)
            {
                Respawn();
            }
            else
            {
                Runner.Despawn(Object); // Удаляем объект игрока, если жизни закончились
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
        _cc.Teleport(spawnPoint.position, spawnPoint.rotation);
        yield return null;

        _cc.enabled = true;
        respawnProtectionTimer = TickTimer.CreateFromSeconds(Runner, 1.0f);

        isRespawning = false;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_UpdateHealthUI(int lives)
    {
        if (healthUI != null)
        {
            healthUI.UpdateHealth(lives); // Обновляем UI на всех клиентах
        }
    }
    public void EnableControls()
    {
        // Здесь включаем управление
        Debug.Log($"{name} получил управление");
        // Например, включаем передвижение через NetworkCharacterController
        GetComponent<NetworkCharacterController>().enabled = true;


    }
}
