using UnityEngine;

public class Projectile : MonoBehaviour
{
    Vector2 direction;
    float speed;
    float lifetime;
    int damage;
    private float alignmentTolerance;
    private bool useAlignment;    // NOVO: define se aplica alinhamento/ângulo

    Rigidbody2D rb;
    Collider2D projectileCollider;

    private GameObject owner;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        projectileCollider = GetComponent<Collider2D>();
    }

    public void SetOwner(GameObject newOwner)
    {
        owner = newOwner;

        if (owner == null) return;

        Collider2D[] ownerColliders = owner.GetComponentsInChildren<Collider2D>();
        foreach (Collider2D col in ownerColliders)
        {
            Physics2D.IgnoreCollision(projectileCollider, col);
        }
    }

    /// <summary>
    /// Inicializa o projétil.
    /// </summary>
    /// <param name="dir">Direção (normalizada).</param>
    /// <param name="spd">Velocidade.</param>
    /// <param name="life">Tempo de vida.</param>
    /// <param name="dmg">Dano base.</param>
    /// <param name="tolerance">Tolerância de alinhamento (para jogador).</param>
    /// <param name="align">Se true, aplica verificações de alinhamento e ângulo; false desativa.</param>
    public void Initialize(Vector2 dir, float spd, float life, int dmg, float tolerance, bool align = true)
    {
        direction = dir.normalized;
        speed = spd;
        lifetime = life;
        damage = dmg;
        alignmentTolerance = tolerance;
        useAlignment = align;

        if (rb != null)
        {
            rb.linearVelocity = direction * speed;
        }

        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Não acertar o dono
        if (owner != null && other.transform.root.gameObject == owner)
            return;

        // Tenta obter Hitbox (sistema de partes do corpo)
        if (other.TryGetComponent(out Hitbox hitbox))
        {
            // Ignora pés (opcional)
            if (hitbox.bodyPart == BodyPart.Feet)
                return;

            int finalDamage = damage;
            if (hitbox.bodyPart == BodyPart.Head)
                finalDamage *= 2;

            // Aplica verificações de alinhamento somente se useAlignment = true
            if (useAlignment)
            {
                if (!PlayerController.IsAxisAligned(
                       owner.transform,
                       other.transform.root,
                       direction,
                       alignmentTolerance))
                    return;

                Vector2 toTarget = (other.transform.position - owner.transform.position).normalized;
                if (Vector2.Dot(direction, toTarget) <= 0.5f)
                    return;
            }

            if (hitbox.damageable != null)
            {
                hitbox.damageable.TakeDamage(finalDamage);
                Destroy(gameObject);
                return;
            }
        }

        // Fallback: atinge objetos com IDamageable diretamente (ex.: jogador sem Hitbox, explosivos)
        IDamageable damageable = other.GetComponent<IDamageable>();
        if (damageable != null)
        {
            // Verificações de alinhamento também se aplicam aqui, se useAlignment = true
            if (useAlignment && owner != null)
            {
                if (!PlayerController.IsAxisAligned(
                       owner.transform,
                       other.transform,
                       direction,
                       alignmentTolerance))
                    return;

                Vector2 toTarget = (other.transform.position - owner.transform.position).normalized;
                if (Vector2.Dot(direction, toTarget) <= 0.5f)
                    return;
            }

            damageable.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        // Se não atingiu nada danificável, destrói assim mesmo (evita balas fantasmas)
        Destroy(gameObject);
    }
}