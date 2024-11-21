using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class Player : NetworkBehaviour

{

    private NetworkCharacterController _cc;
    [SerializeField] private ball _prefabBall;
    private Vector3 _forward = Vector3.forward;
    [Networked] private TickTimer delay { get; set; }

    private void Awake()
    {
        _cc = GetComponent<NetworkCharacterController>();
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

            if (HasInputAuthority && delay.ExpiredOrNotRunning(Runner))
            {


                if (data.buttons.IsSet(NetworkInputData.MOUSEBUTTON0))
                {
                    delay = TickTimer.CreateFromSeconds(Runner, 0.5f);
                    Runner.Spawn(
                        _prefabBall, transform.position + _forward,
                        Quaternion.LookRotation(_forward),
                        Object.InputAuthority, (runner, o) =>
                        {
                            o.GetComponent<ball>().Init();
                        });
                }
            }
        }
    }
}