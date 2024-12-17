using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class Ball : NetworkBehaviour
{
    [Networked] private TickTimer life { get; set; }
    private float _speed;
    [SerializeField] private float knockbackForce = 20f; // Сила отталкивания

    public override void Spawned()
    {
        // Устанавливаем таймер жизни пули
        if (Object.HasStateAuthority)
        {
            life = TickTimer.CreateFromSeconds(Runner, 2f); // Пуля исчезнет через 5 секунд
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (life.Expired(Runner))
        {
            Runner.Despawn(Object);
        }
        else
        {
            transform.position += transform.forward * _speed * Runner.DeltaTime;
        }
    }

    public void SetSpeed(float speed)
    {
        _speed = speed;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.TryGetComponent<Player>(out Player player))
        {
            // Применяем силу отталкивания к игроку
            Vector3 direction = collision.transform.position - transform.position;
            direction.Normalize();

            if (player.TryGetComponent<Rigidbody>(out Rigidbody playerRigidbody))
            {
                playerRigidbody.AddForce(direction * knockbackForce, ForceMode.Impulse);
            }
        }

        // Удаляем пулю после столкновения
        if (Object.HasStateAuthority)
        {
            Runner.Despawn(Object);
        }
    }
}