using UnityEngine;

public class EnemyTouchSensor : MonoBehaviour
{
    [Header("Toque Horizontal")]
    public bool checkSameHeight = true;
    public float heightTolerance = 0.5f;

    [Header("Toque Vertical")]
    public bool checkVerticalAlignment = true;
    public float horizontalTolerance = 0.5f;
    public float verticalTouchRadius = 1.5f;

    [Header("Fallback por Distância (contra pontos cegos)")]
    [Tooltip("Se ativo, ignora os triggers e detecta o jogador por distância pura.")]
    public bool useDistanceFallback = true;
    [Tooltip("Raio máximo para a detecção por distância.")]
    public float fallbackRadius = 1.5f;
    [Tooltip("Intervalo mínimo entre verificações para performance.")]
    public float fallbackCheckInterval = 0.2f;

    private EnemyController enemy;
    private Transform player;
    private float lastFallbackTime;

    void Awake()
    {
        enemy = GetComponentInParent<EnemyController>();
        if (enemy == null)
            Debug.LogError("EnemyTouchSensor não encontrou EnemyController no pai!", this);

        // Cache do jogador (via EnemyController já tem referência, mas podemos pegar direto)
        if (enemy != null && enemy.player != null)
            player = enemy.player;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || enemy == null) return;
        AttemptAlert(other.transform);
    }

    private void Update()
    {
        if (DynamicMusicManager.IsFrozen)
            return;

        if (!useDistanceFallback || enemy == null || player == null) return;
        if (Time.time - lastFallbackTime < fallbackCheckInterval) return;
        lastFallbackTime = Time.time;

        float distance = Vector2.Distance(transform.position, player.position);
        if (distance <= fallbackRadius)
        {
            // Verifica se realmente não foi detectado recentemente? Pode chamar direto.
            AttemptAlert(player);
        }
    }

    private void AttemptAlert(Transform target)
    {
        if (enemy == null) return;

        // Reavalia as condições de altura / alinhamento
        bool triggered = false;

        if (checkSameHeight)
        {
            float deltaY = Mathf.Abs(transform.position.y - target.position.y);
            if (deltaY <= heightTolerance)
                triggered = true;
        }

        if (!triggered && checkVerticalAlignment)
        {
            float deltaX = Mathf.Abs(transform.position.x - target.position.x);
            if (deltaX <= horizontalTolerance)
            {
                float dist = Vector2.Distance(transform.position, target.position);
                if (dist <= verticalTouchRadius)
                    triggered = true;
            }
        }

        // Se o fallback por distância estiver ativo, qualquer distância já dispara (sem condições extras)
        if (useDistanceFallback && !triggered)
        {
            triggered = true; // confia na distância
        }

        if (triggered)
        {
            GlobalAlertManager.TriggerDetection(target.position);
            if (enemy.currentState != enemy.attackState)
                enemy.ChangeState(enemy.attackState);
        }
    }

    // Gizmo para visualizar o fallback
    private void OnDrawGizmosSelected()
    {
        if (useDistanceFallback)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, fallbackRadius);
        }
        if (checkVerticalAlignment)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, verticalTouchRadius);
        }
    }
}