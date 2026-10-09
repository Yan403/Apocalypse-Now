using UnityEngine;
using UnityEngine.InputSystem;

public class IdleCrawlState : PlayerState
{
    public override void Enter()
    {
        player.anim.SetInteger("Posture", 2);
        player.rb.linearVelocity = Vector2.zero;

   
    }
    public override void DoUpdate()
    {
        if (player.processedInput != Vector2.zero)
        {
            player.ChangeState(player.crawlState);
        }

        // LÓGICA CORRIGIDA: Uso do novo Input System [2]
        if (Keyboard.current.cKey.wasPressedThisFrame)
        {
            player.ChangeState(player.crouchState);
            return;
        }
    }
    public override void Exit() { }
}
