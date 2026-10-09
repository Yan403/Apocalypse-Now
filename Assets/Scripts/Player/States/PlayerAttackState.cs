using UnityEngine;
using System.Collections.Generic;

public class PlayerAttackState : PlayerState, ICombat
{
    [Header("Punch")]
    public float punchRange = 0.5f;
    public int punchDamage = 1;
    public LayerMask hitLayer;

    [Header("Soco - Áudio")]
    public AudioClip punchClip;            // som do soco
    public AudioSource punchAudioSource;   // AudioSource dedicado (opcional, pode ser do player)

    private float stateTimer;
    public override void Enter()
    {
        if (GameManager.IsPaused)
            return;

        stateTimer = 0.5f;

        player.isAttacking = true;

        if (player.isArmed)
        {
            player.anim.SetInteger("Posture", 3);
        }
        else
        {
            player.anim.SetInteger("Posture", 4);
        }

        // Garante um AudioSource para o soco (usa o do player se não tiver)
        if (punchAudioSource == null)
            punchAudioSource = player.GetComponent<AudioSource>();
        if (punchAudioSource == null && punchClip != null)
            punchAudioSource = player.gameObject.AddComponent<AudioSource>();

        player.anim.SetFloat("InputX", player.lastDirection.x);
        player.anim.SetFloat("InputY", player.lastDirection.y);
        player.anim.SetFloat("Speed", 0); // 🔥 ESSENCIAL

        ExecuteAttack();
    }

    public override void DoUpdate()
    {
        stateTimer -= Time.deltaTime;

        if (stateTimer <= 0)
        {
            DecideNextState();
        }
    }

    public void ExecuteAttack()
    {
        if (player.isArmed)
        {
            Shoot();
            player.nextAttackTime = Time.time + player.equippedWeapon.fireRate;
        }
        else
        {
            // Toca o som de soco
            if (punchClip != null && punchAudioSource != null)
                punchAudioSource.PlayOneShot(punchClip);

            Debug.Log("PUNCH");
            Punch();
            player.nextAttackTime = Time.time + player.punchCooldown;
        }
    }

    // =========================
    // 🔫 SHOOT
    // =========================
    void Shoot()
    {
        if (player.currentAmmo <= 0) return;

        GameObject bullet = Object.Instantiate(
            player.equippedWeapon.projectilePrefab,
            player.firePoint.position,
            Quaternion.identity
        );

        Projectile projectileScript = bullet.GetComponent<Projectile>();

        projectileScript.SetOwner(player.gameObject);

        if (projectileScript != null)
        {
            projectileScript.Initialize(
                player.lastDirection,
                player.equippedWeapon.projectileSpeed,
                player.equippedWeapon.projectileLifetime,
                player.equippedWeapon.damage,
                player.attackAlignmentTolerance
            );
        }

        player.UseAmmo(1);
    }

    // =========================
    // 👊 PUNCH
    // =========================
    void Punch()
    {
        Vector2 direction = player.lastDirection;

        var data = player.GetPunchData(direction);

        Vector2 origin = (Vector2)player.transform.position
                       + direction * data.distance
                       + data.offset;

        float angle = 0f;

        if (direction == Vector2.up) angle = 90f;
        else if (direction == Vector2.down) angle = -90f;
        else if (direction == Vector2.left) angle = 180f;

        Collider2D[] hits = Physics2D.OverlapBoxAll(
            origin,
            data.boxSize,
            angle,
            hitLayer
        );


        HashSet<IDamageable> alreadyHit = new HashSet<IDamageable>();

        foreach (Collider2D hit in hits)
        {
            Hitbox hitbox = hit.GetComponent<Hitbox>();

            if (hitbox == null)
                continue;

            // 🔴 IGNORA O PRÓPRIO PLAYER
            if (hit.transform.root == player.transform)
                continue;

            // 🚫 ignora feet
            if (hitbox.bodyPart == BodyPart.Feet)
                continue;

            // 🎯 alinhamento (mesma linha OU coluna)
            if (!PlayerController.IsAxisAligned(
                player.transform,
                hit.transform.root,
                player.lastDirection,
                player.attackAlignmentTolerance))
                continue;

            // 🎯 direção (não acerta atrás)
            Vector2 toTarget = (hit.transform.position - player.transform.position).normalized;
            float dot = Vector2.Dot(player.lastDirection, toTarget);

            if (dot <= 0.5f)
                continue;

            if (hitbox.damageable != null && !alreadyHit.Contains(hitbox.damageable))
            {
                alreadyHit.Add(hitbox.damageable);
                hitbox.damageable.TakeDamage(punchDamage);

                // 🆕 NOVO - aplica stun no inimigo acertado pelo soco
                EnemyController enemy = hitbox.GetComponentInParent<EnemyController>();
                if (enemy != null)
                {
                    enemy.ApplyStun(player.punchStunDuration);
                }

                Debug.Log("Punch acertou: " + hitbox.bodyPart);
            }
        }
    }

    // =========================
    // 🔄 TRANSIÇÃO
    // =========================
    void DecideNextState()
    {
        if (player.isArmed)
        {
            if (player.processedInput != Vector2.zero)
                player.ChangeState(player.walkArmedState);
            else
                player.ChangeState(player.idleArmedState);
        }
        else
        {
            if (player.processedInput != Vector2.zero)
                player.ChangeState(player.walkState);
            else
                player.ChangeState(player.idleState);
        }
    }

    public override void Exit()
    {
        player.isAttacking = false;

        // 🔴 volta pra postura normal
        if (player.isArmed)
            player.anim.SetInteger("Posture", 3);
        else
            player.anim.SetInteger("Posture", 0);

        isComplete = false;
    }
}