using UnityEngine;
using UnityEngine.InputSystem;

public class IdleState : PlayerState
{
    public override void Enter()
    {
        player.anim.SetInteger("Posture", 0);
        player.rb.linearVelocity = Vector2.zero;

        
    }
    public override void DoUpdate()
    {
        if (player.isArmed)
        {
            player.ChangeState(player.idleArmedState);
            return;
        }

        if (player.processedInput != Vector2.zero)
        {
            player.ChangeState(player.walkState);
        }

        // LÓGICA CORRIGIDA: Uso do novo Input System [2]
        if (!player.isTurning && Keyboard.current.cKey.wasPressedThisFrame)
        {
            player.ChangeState(player.crouchState);
            return;
        }
    }
    public override void Exit() { }
}