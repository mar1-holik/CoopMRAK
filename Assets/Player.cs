using Fusion;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class Player : NetworkBehaviour
{
    [SerializeField] private Ball _prefabBall;
    [SerializeField] private Animator _animator;
    [SerializeField] private float shootDelay = 0.4f;
    [SerializeField] private float shootCooldown = 1.3f;
    [SerializeField] private float bulletSpeed = 10f;
    [SerializeField] private HealthUI healthUI;

    [SerializeField] private Image playerIcon;
    [SerializeField] private Sprite[] playerIcons;

    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _shootSound;

    [Networked] public int CurrentLives { get; private set; } = 3;
    [Networked] private TickTimer delay { get; set; }
    [Networked] private TickTimer respawnProtectionTimer { get; set; }

    private NetworkCharacterController _cc;
    private Vector3 _forward;
    public bool isRespawning = false;
    private bool isShooting = false;

    private void Awake()
    {
        _cc = GetComponent<NetworkCharacterController>();
        _forward = transform.forward;

        if (_audioSource == null)
        {
            _audioSource = GetComponent<AudioSource>();
        }

        if (healthUI == null)
        {
            Debug.LogError("HealthUI not assigned!");
        }

        if (_animator == null)
        {
            _animator = GetComponentInChildren<Animator>();
        }
    }

    public override void Spawned()
    {
        base.Spawned();

        if (HasStateAuthority)
        {
            CurrentLives = 3;
            healthUI?.InitializeHealth(CurrentLives);
        }

        if (Object.HasInputAuthority && playerIcons.Length > 0)
        {
            int iconIndex = Object.InputAuthority.PlayerId % playerIcons.Length;
            playerIcon.sprite = playerIcons[iconIndex];
        }
        else
        {
            playerIcon.enabled = false;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (_cc == null || !_cc.enabled) return;

        if (GetInput(out NetworkInputData data))
        {
            data.direction.Normalize();
            _cc.Move(5 * data.direction * Runner.DeltaTime);

            if (data.direction.sqrMagnitude > 0)
            {
                _forward = data.direction;
            }

            if (Object.HasInputAuthority && data.buttons.IsSet(NetworkInputData.MOUSEBUTTON0))
            {
                TryShoot();
            }

            RPC_SetAnimation("isRunning", data.direction.sqrMagnitude > 0);
        }
    }

    private void TryShoot()
    {
        if (delay.ExpiredOrNotRunning(Runner) && !isShooting)
        {
            isShooting = true;
            RPC_SetAnimation("isShooting", true);
            StartCoroutine(CompleteShooting());
        }
    }

    private IEnumerator CompleteShooting()
    {
        yield return new WaitForSeconds(shootDelay);

        if (_audioSource != null && _shootSound != null)
        {
            _audioSource.PlayOneShot(_shootSound);
        }

        if (Object.HasInputAuthority)
        {
            // Отправляем запрос хосту на спавн пули
            RPC_RequestSpawnBullet(_forward, transform.position);
        }

        RPC_SetAnimation("isShooting", false);
        isShooting = false;

        if (Object.HasStateAuthority)
        {
            delay = TickTimer.CreateFromSeconds(Runner, shootCooldown);
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestSpawnBullet(Vector3 direction, Vector3 spawnPosition)
    {
        if (delay.ExpiredOrNotRunning(Runner))
        {
            Runner.Spawn(
                _prefabBall,
                spawnPosition + direction,
                Quaternion.LookRotation(direction),
                Object.InputAuthority,
                (runner, o) => o.GetComponent<Ball>().SetSpeed(bulletSpeed)
            );
            delay = TickTimer.CreateFromSeconds(Runner, shootCooldown);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_SetAnimation(string parameter, bool state)
    {
        _animator?.SetBool(parameter, state);
    }

    public void HandleDeathZone()
    {
        if (Object == null || isRespawning) return;

        if (HasStateAuthority)
        {
            if (IsProtected()) return;

            if (CurrentLives > 0)
            {
                CurrentLives--;
                RPC_UpdateHealthUI(CurrentLives);
            }

            if (CurrentLives > 0)
            {
                StartCoroutine(RespawnCoroutine());
            }
            else
            {
                Runner.Despawn(Object);
                FindObjectOfType<WinManager>()?.CheckWinner();
            }
        }
    }

    public bool IsProtected() => !respawnProtectionTimer.ExpiredOrNotRunning(Runner);

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

    public void EnableControls()
    {
        if (_cc != null)
        {
            _cc.enabled = true;
        }
    }

    public void DisableControls()
    {
        if (_cc != null)
        {
            _cc.enabled = false;
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_UpdateHealthUI(int lives)
    {
        healthUI?.UpdateHealth(lives);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_Teleport(Vector3 position, Quaternion rotation)
    {
        _cc.Teleport(position, rotation);
    }
}
