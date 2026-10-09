using UnityEngine;
using UnityEngine.InputSystem;

public class IdleArmedState : PlayerState
{
    public override void Enter()
    {
        player.anim.SetInteger("Posture", 3);
        player.rb.linearVelocity = Vector2.zero;
    }
    public override void DoUpdate()
    {
        if (player.processedInput != Vector2.zero)
        {
            player.ChangeState(player.walkArmedState);
        }

        // LÓGICA CORRIGIDA: Uso do novo Input System [2]
        if (Keyboard.current.cKey.wasPressedThisFrame)
        {
            player.ChangeState(player.crouchState);
            return;
        }

        if(player.isArmed == false)
        {
            player.ChangeState(player.idleState);
        }
    }
    public override void Exit() { }
}
