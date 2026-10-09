using UnityEngine;
using UnityEngine.InputSystem;

public class WalkState : PlayerState
{
    public float speed = 5f;

    public override void Enter() 
    {
        player.anim.SetInteger("Posture", 0);
    } 

    public override void DoUpdate()
    {
        if (player.isArmed)
        {
            player.ChangeState(player.idleArmedState);
            return;
        }

        player.rb.linearVelocity = player.processedInput * speed;

        // Atualiza animações baseada no input filtrado [Conversa Anterior]
        if (player.processedInput != Vector2.zero)
        {
            player.anim.SetFloat("InputX", player.processedInput.x);
            player.anim.SetFloat("InputY", player.processedInput.y);
        }
        else
        {
            player.ChangeState(player.idleState);
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
