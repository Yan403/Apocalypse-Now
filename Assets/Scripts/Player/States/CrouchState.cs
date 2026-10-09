using UnityEngine;
using UnityEngine.InputSystem;

public class CrouchState : PlayerState
{
    [Header("Crouch Settings")]
    public float crouchLockTime = 0.15f; // tempo mínimo que o jogador fica agachado antes de poder rastejar
    private float lockTimer;
    public override void Enter()
    {
        player.anim.SetInteger("Posture", 1);
        player.rb.linearVelocity = Vector2.zero;
        lockTimer = crouchLockTime;

    }
    public override void DoUpdate()
    {
        // Reduz o temporizador de trava
        if (lockTimer > 0f)
        {
            lockTimer -= Time.deltaTime;
        }

        // Só permite rastejar se o tempo de trava já passou E há input de movimento
        if (!player.isTurning && lockTimer <= 0f && player.processedInput != Vector2.zero)
        {
            player.ChangeState(player.crawlState);
            return;
        }

        // LÓGICA CORRIGIDA: Uso do novo Input System [2]
        if (!player.isTurning && Keyboard.current.cKey.wasPressedThisFrame)
        {
            player.ChangeState(player.idleState);
            return;
        }
    }
    public override void Exit() { }
}
