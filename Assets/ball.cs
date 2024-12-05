using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class Ball : NetworkBehaviour
{

    [Networked] private TickTimer life { get; set; }
    public override void FixedUpdateNetwork()
    {
        if (life.Expired(Runner))
        {
            Runner.Despawn(Object);
        }
        else
        {
            transform.position += 5 * transform.forward * Runner.DeltaTime;
        }
    }
    public void Init()
    {
        life = TickTimer.CreateFromSeconds(Runner, 1.7f);
    }

    private float _speed;

    public void SetSpeed(float speed)
    {
        _speed = speed;
    }

    private void Update()
    {
        // Перемещаем снаряд вперёд с заданной скоростью
        transform.position += transform.forward * _speed * Time.deltaTime;
    }


}
