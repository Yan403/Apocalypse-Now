using UnityEngine;
using UnityEngine.InputSystem;

public class WalkArmedState : PlayerState
{
    public float speed = 3f;

    public override void Enter()
    {
        player.anim.SetInteger("Posture", 3);
    }

    public override void DoUpdate()
    {
        player.rb.linearVelocity = player.processedInput * speed;

        // Atualiza animações baseada no input filtrado [Conversa Anterior]
        if (player.processedInput != Vector2.zero)
        {
            player.anim.SetFloat("InputX", player.processedInput.x);
            player.anim.SetFloat("InputY", player.processedInput.y);
        }
        else
        {
            player.ChangeState(player.idleArmedState);
        }

        // LÓGICA CORRIGIDA: Uso do novo Input System [2]
        if (Keyboard.current.cKey.wasPressedThisFrame)
        {
            player.ChangeState(player.crouchState);
        }
    }
    public override void Exit()
    {
        player.rb.linearVelocity = Vector2.zero;
    }
}
