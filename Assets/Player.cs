using Fusion;
using UnityEngine;

public class Player : NetworkBehaviour
{
    [SerializeField] private Ball _prefabBall;

    [Networked] public int CurrentLives { get; private set; } = 3; // Количество жизней

    [Networked] private TickTimer delay { get; set; }

    private NetworkCharacterController _cc;
    private Vector3 _forward;

    private void Awake()
    {
        _cc = GetComponent<NetworkCharacterController>();
        _forward = transform.forward;
    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData data))
        {
            data.direction.Normalize();
            _cc.Move(5 * data.direction * Runner.DeltaTime);

            if (data.direction.sqrMagnitude > 0)
                _forward = data.direction;

            if (HasStateAuthority && delay.ExpiredOrNotRunning(Runner))
            {
                if (data.buttons.IsSet(NetworkInputData.MOUSEBUTTON0))
                {
                    delay = TickTimer.CreateFromSeconds(Runner, 1.5f);
                    Runner.Spawn(_prefabBall,
                    transform.position + _forward, Quaternion.LookRotation(_forward),
                    Object.InputAuthority, (runner, o) =>
                    {
                        // Initialize the Ball before synchronizing it
                        o.GetComponent<Ball>().Init();
                    });
                }
            }
        }
    }

    // Уменьшение количества жизней
    public void DecreaseLife()
    {
        if (HasStateAuthority)
        {
            CurrentLives--;
        }
    }

    // Респаун игрока
    public void Respawn()
    {
        if (HasStateAuthority)
        {
            transform.position = new Vector3(0, 3, 0); // Фиксированная точка респауна
        }
    }

    // Удаление игрока из игры
    public void RemoveFromGame()
    {
        if (HasStateAuthority)
        {
            Runner.Despawn(Object); // Удаляем игрока из игры
        }
    }
}
