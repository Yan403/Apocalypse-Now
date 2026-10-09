using UnityEngine;

public class AttackState : EnemyState
{
    [Header("Tiro")]
    public GameObject bulletPrefab;
    public float fireRate = 1f;
    public float bulletSpeed = 5f;
    public float bulletLifetime = 3f;
    public int bulletDamage = 10;

    [Header("Ronda")]
    public float moveSpeed = 3f;
    public float minDistance = 3f;
    public float maxDistance = 5f;
    public float pointReachThreshold = 0.5f;
    public float newPointInterval = 2f;
    public float directionChangeCooldown = 0.2f; // tempo mínimo entre mudanças de direção

    private Vector2 targetPoint;
    private float pointTimer;
    private float nextFire;

    private Vector2 currentMoveDirection = Vector2.zero; // direção estável atual
    private Vector2 pendingDirection = Vector2.zero;     // direção candidata à mudança
    private float pendingDirectionTimer = 0f;            // há quanto tempo a candidata é consistente

    public override void Enter()
    {
        ChooseNewTargetPoint();
        pointTimer = 0f;
        currentMoveDirection = enemy.facingDirection; // começa com a direção atual
    }

    public override void DoUpdate()
    {
        pointTimer += Time.deltaTime;

        // ---- 1. Verifica necessidade de novo ponto alvo ----
        float distToTarget = Vector2.Distance(enemy.transform.position, targetPoint);
        float distToPlayer = Vector2.Distance(enemy.transform.position, enemy.player.position);

        bool needNewPoint = distToTarget < pointReachThreshold
                            || pointTimer >= newPointInterval
                            || distToPlayer < minDistance
                            || distToPlayer > maxDistance;

        if (needNewPoint)
            ChooseNewTargetPoint();

        // ---- 2. Calcula direção desejada (cardeal) para o ponto alvo ----
        Vector2 toTarget = targetPoint - (Vector2)enemy.transform.position;
        Vector2 desiredDirection = enemy.FilterFourWayDirection(toTarget.normalized);

        if (desiredDirection == Vector2.zero)
            desiredDirection = currentMoveDirection; // mantém a atual se falhar

        // ---- 3. Debounce de direção (evita flicker) ----
        if (desiredDirection != currentMoveDirection)
        {
            // Se é uma nova direção, inicializa ou verifica consistência
            if (desiredDirection != pendingDirection)
            {
                pendingDirection = desiredDirection;
                pendingDirectionTimer = 0f;
            }
            else
            {
                pendingDirectionTimer += Time.deltaTime;
                if (pendingDirectionTimer >= directionChangeCooldown)
                {
                    // Confirma a troca de direção
                    currentMoveDirection = pendingDirection;
                    pendingDirection = currentMoveDirection; // reseta
                }
            }
        }
        else
        {
            // Se voltou para a direção atual, cancela qualquer pendência
            if (pendingDirection != currentMoveDirection)
            {
                pendingDirection = currentMoveDirection;
                pendingDirectionTimer = 0f;
            }
        }

        // ---- 4. Movimento e animação ----
        enemy.rb.linearVelocity = currentMoveDirection * moveSpeed;

        enemy.facingDirection = currentMoveDirection;
        enemy.UpdateFirePoint();

        enemy.anim.SetBool("isWalking", true);
        enemy.anim.SetFloat("X", currentMoveDirection.x);
        enemy.anim.SetFloat("Y", currentMoveDirection.y);

        // ---- 5. Tiro contínuo ----
        if (Time.time > nextFire)
        {
            Shoot(currentMoveDirection);
            nextFire = Time.time + fireRate;
        }
    }

    private void ChooseNewTargetPoint()
    {
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float radius = Random.Range(minDistance, maxDistance);
        Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        targetPoint = (Vector2)enemy.player.position + offset;
        pointTimer = 0f;
    }

    void Shoot(Vector2 direction)
    {
        if (bulletPrefab == null) return;

        Vector3 spawnPos = enemy.firePoint != null ? enemy.firePoint.position : enemy.transform.position;
        GameObject bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);
        Projectile projectileScript = bullet.GetComponent<Projectile>();

        if (projectileScript != null)
        {
            projectileScript.SetOwner(enemy.gameObject);
            projectileScript.Initialize(
                direction,
                bulletSpeed,
                bulletLifetime,
                bulletDamage,
                0f,       // tolerância (ignorada porque align = false)
                false     // desativa verificações de alinhamento/ângulo
            );
        }
    }

    public override void Exit()
    {
        enemy.rb.linearVelocity = Vector2.zero;
    }
}