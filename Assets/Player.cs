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
    private BasicSpawner _spawner;

    private void Awake()
    {
        _cc = GetComponent<NetworkCharacterController>();
        _forward = transform.forward;

        // Ссылка на BasicSpawner
        _spawner = FindObjectOfType<BasicSpawner>();
    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData data))
        {
            // Движение игрока
            data.direction.Normalize();
            _cc.Move(5 * data.direction * Runner.DeltaTime);

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
                            o.GetComponent<Ball>().Init();
                        });
                }
            }
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
            Respawn();
        }
        else
        {
            Debug.Log($"Игрок {Object.InputAuthority} потерял все жизни.");
            Runner.Despawn(Object); // Удаляем игрока из игры
        }
    }
}


    private void Respawn()
    {
        StartCoroutine(RespawnCoroutine());
    }

    private IEnumerator RespawnCoroutine()
{
    _cc.enabled = false;

    // Получаем точку спавна из BasicSpawner
    Transform spawnPoint = _spawner.GetSpawnPointForPlayer(Object.InputAuthority);

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
        _cc.Teleport(new Vector3(0, 10, 0), Quaternion.identity); // Фолбек позиция
    }

    _cc.Velocity = Vector3.zero;

    // Ждём один кадр, чтобы телепортация сработала корректно
    yield return null;

    _cc.enabled = true;

    // Активируем временную защиту
    respawnProtectionTimer = TickTimer.CreateFromSeconds(Runner, 1.0f);
    Debug.Log("Активирована защита после респавна");
}


    public bool IsProtected()
    {
        return respawnProtectionTimer.ExpiredOrNotRunning(Runner) == false;
    }
}
