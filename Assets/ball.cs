using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class Ball : NetworkBehaviour
{
    [Networked] private TickTimer life { get; set; }
    private float _speed; // Add this line to define _speed

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
}
