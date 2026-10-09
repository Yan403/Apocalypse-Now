using UnityEngine;
using UnityEngine.InputSystem;

public class CrawlState : PlayerState
{
    public float crawlSpeed = 2f;
    public override void Enter()
    {
        player.anim.SetInteger("Posture", 2);
    }
    public override void DoUpdate()
    {
        player.rb.linearVelocity = player.processedInput * crawlSpeed;

        // Atualiza animações baseada no input filtrado [Conversa Anterior]
        if (player.processedInput != Vector2.zero)
        {
            player.anim.SetFloat("InputX", player.processedInput.x);
            player.anim.SetFloat("InputY", player.processedInput.y);
        }

        else
        {
            player.ChangeState(player.idlecrawlState);
        }
    }
    public override void Exit()
    {
        player.rb.linearVelocity = Vector2.zero;
    }
}
