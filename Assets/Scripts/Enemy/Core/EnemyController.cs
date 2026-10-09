using UnityEngine;
using System.Collections;
public class EnemyController : MonoBehaviour
{
    private Vector2 lastVisualDir; // Armazena a última direção válida para evitar trepidação
    public Vector3 EyePosition => transform.position + (Vector3)eyeOffset;

    public EnemyState currentState;
    [Header("Estados")]
    public PatrolState patrolState;
    public SearchState searchState;
    public AttackState attackState;

    [Header("Componentes")]
    public Rigidbody2D rb;
    public Animator anim;
    public Transform player;

    [Header("Configurações de Visão (FOV)")]
    public Vector2 eyeOffset = new Vector2(0, 0.5f); // Deslocamento para a altura dos olhos
    public float viewDistance = 5f;    // Alcance da visão
    [Range(0, 360)]
    public float viewAngle = 90f;     // Ângulo total do cone de visão
    public LayerMask obstructionMask; // Camada de paredes/obstáculos
    public LayerMask playerMask;      // Camada do jogador

    [Header("Memória de Alerta")]
    public Vector2 lastKnownPlayerPos;
    public Vector2 facingDirection = Vector2.down;

    [Header("Ponto de Disparo")]
    public Transform firePoint;              // Arraste o GameObject "FirePoint" (filho do inimigo)
    public Vector2 firePointUp = new Vector2(0f, 0.5f);
    public Vector2 firePointDown = new Vector2(0f, -0.5f);
    public Vector2 firePointLeft = new Vector2(-0.5f, 0f);
    public Vector2 firePointRight = new Vector2(0.5f, 0f);

    // 🆕 NOVO - sistema de stun
    [Header("Stun")]
    public float stunDuration = 0.6f;   // duração base do stun (pode ser sobrescrito pelo soco)
    private float stunTimer;
    private bool isStunned;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();

        GlobalAlertManager.RegisterEnemy(this);
        
        // Aquisição segura do Player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

        if (playerObj != null)
        {
            player = playerObj.transform;
        }

        // 🆕 NOVO - inscrever no evento de dano
        HealthSystem health = GetComponent<HealthSystem>();
        if (health != null)
        {
            health.OnDamaged += OnTakeDamage;
            health.OnDeath += OnEnemyDeath;   // 🆕
        }

        // Inicialização modular conforme as fontes [7]
        patrolState.Setup(this);
        searchState.Setup(this);
        attackState.Setup(this);

        ChangeState(patrolState);
    }

    void Update()
    {
        if (DynamicMusicManager.IsFrozen)
            return;

        if (player == null)
            return;

        if (GameManager.IsPaused)
            return;

        // 🆕 NOVO - processa stun
        if (isStunned)
        {
            stunTimer -= Time.deltaTime;
            rb.linearVelocity = Vector2.zero;  // imobiliza
            
            if (stunTimer <= 0f)
            {
                isStunned = false;
                // Quando o stun acaba e o inimigo está em modo de ataque, dispara o alerta global
                if (currentState == attackState && player != null)
                    GlobalAlertManager.TriggerDetection(player.position);
            }

            UpdateAnimationState();            // mantém idle forçado
            return;                            // não atualiza estado durante o stun
        }

        // Só atualiza a direção do sprite se NÃO estiver atirando
        if (currentState != attackState)
            UpdateFacingDirection();

        if (currentState != null) currentState.DoUpdate();

        // --- NOVA LÓGICA DE ENGAJAMENTO POR FOV ---
        if (IsPlayerInFOV())
        {
            // Alerta outros inimigos (sistema global mantido)
            GlobalAlertManager.TriggerDetection(player.position);   // <-- alterado

            // O inimigo que vê o player entra imediatamente em modo de ataque
            if (currentState != attackState)
            {
                ChangeState(attackState);
                GlobalAlertManager.RegisterAlertedEnemy(this);   // 🆕
            }
        }
        else
        {
            // Sai do modo de ataque quando perde a visão do player
            if (currentState == attackState)
                ChangeState(searchState);
        }
        // --------------------------------------------

        UpdateAnimationState();
    }

    // 🆕 NOVO - chamado quando sofre qualquer dano
    private void OnTakeDamage()
    {
        // Entra em ataque imediatamente
        if (currentState != attackState)
            ChangeState(attackState);

        // Aplica stun padrão (sobrescrito se o soco chamar ApplyStun com duração específica)
        stunTimer = stunDuration;
        isStunned = true;
        rb.linearVelocity = Vector2.zero;
    }

    // 🆕 NOVO - permite que o soco defina uma duração de stun personalizada
    public void ApplyStun(float duration)
    {
        if (duration > stunTimer)   // mantém o maior tempo de stun (evita sobrescrever um stun longo)
            stunTimer = duration;
        isStunned = true;
        rb.linearVelocity = Vector2.zero;

        // Feedback visual de impacto – só no soco
        StartCoroutine(ShakeEffect(0.2f, 0.05f));
    }

    public void ChangeState(EnemyState newState)
    {
        if (currentState != null) currentState.Exit();
        currentState = newState;
        currentState.Enter();
    }

    void UpdateFacingDirection()
    {
        // Se o inimigo estiver se movendo, usamos a velocidade para definir o olhar
        if (rb.linearVelocity.magnitude > 0.1f)
        {
            facingDirection = rb.linearVelocity.normalized;
        }
    }
    private void UpdateAnimationState()
    {
        // Verificamos se há intenção de movimento no Rigidbody [4]
        bool isMoving = rb.linearVelocity.sqrMagnitude > 0.01f;
        anim.SetBool("isWalking", isMoving);

        if (isMoving)
        {
            // IMPORTANTE: Filtramos a velocidade atual para saber para onde ele DEVERIA olhar
            Vector2 cleanDir = FilterFourWayDirection(rb.linearVelocity.normalized);

            if (cleanDir != Vector2.zero)
            {
                anim.SetFloat("X", cleanDir.x);
                anim.SetFloat("Y", cleanDir.y);
            }
        }
    }

    public bool IsPlayerInFOV()
    {
        if (player == null)
            return false;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        // 1. Checagem de distância básica [5, 6]
        if (distanceToPlayer <= viewDistance)
        {
            // 2. Checagem de ângulo
            Vector2 dirToPlayer = (player.position - transform.position).normalized;
            float angleBetween = Vector2.Angle(facingDirection, dirToPlayer);

            if (angleBetween <= viewAngle/2)
            {
                // 3. Checagem de obstrução (Raycast) [2]
                // Lança um raio para ver se há algo entre o inimigo e o jogador
                RaycastHit2D hit = Physics2D.Raycast(EyePosition, dirToPlayer, viewDistance, obstructionMask | playerMask);

                if (hit.collider != null)
                {
                    PlayerController playerComponent = hit.collider.GetComponentInParent<PlayerController>();

                    if (playerComponent != null)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }
    public void NotifyAlert(Vector2 playerLastPos)
    {
        lastKnownPlayerPos = playerLastPos;

        if (currentState != attackState)
        {
            ChangeState(searchState);
        }

        // 🆕 Registra que este inimigo está alertado
        GlobalAlertManager.RegisterAlertedEnemy(this);
    }
    public Vector2 FilterFourWayDirection(Vector2 rawDirection)
    {
        // Se o movimento for quase zero, para totalmente [3]
        if (rawDirection.sqrMagnitude < 0.01f) return Vector2.zero;

        float absX = Mathf.Abs(rawDirection.x);
        float absY = Mathf.Abs(rawDirection.y);

        // Adicionamos uma margem de erro (0.1f). 
        // O eixo novo só ganha se for significativamente maior que o outro.
        if (absX > absY + 0.1f)
        {
            lastVisualDir = new Vector2(Mathf.Sign(rawDirection.x), 0);
        }
        else if (absY > absX + 0.1f)
        {
            lastVisualDir = new Vector2(0, Mathf.Sign(rawDirection.y));
        }
        // Se forem muito parecidos, mantém a última direção (lastVisualDir) para evitar flickering

        return lastVisualDir;
    }

    void OnDrawGizmosSelected()
    {
        if (anim == null) return;

        // Desenha o alcance máximo da visão (Círculo branco)
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(EyePosition, viewDistance);

        // Calcula as bordas do cone de visão baseado na direção estável (lastVisualDir)
        Vector3 viewAngleA = DirFromAngle(-viewAngle / 2, false);
        Vector3 viewAngleB = DirFromAngle(viewAngle / 2, false);

        // Desenha as linhas amarelas que formam o cone
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(EyePosition, EyePosition + viewAngleA * viewDistance);
        Gizmos.DrawLine(EyePosition, EyePosition + viewAngleB * viewDistance);
    }

    private Vector3 DirFromAngle(float angleInDegrees, bool isGlobal)
    {
        if (!isGlobal)
        {
            // IMPORTANTE: Usamos lastVisualDir em vez de rb.linearVelocity.normalized
            // para que o cone de visão não trema (flicker) e fique sempre alinhado ao sprite [Conversa Anterior]
            angleInDegrees += Mathf.Atan2(facingDirection.y, facingDirection.x) * Mathf.Rad2Deg;
        }
        return new Vector3(Mathf.Cos(angleInDegrees * Mathf.Deg2Rad), Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0);
    }
    public void UpdateFirePoint()
    {
        if (firePoint == null) return;

        Vector2 dir = facingDirection;

        if (dir == Vector2.up)
            firePoint.localPosition = firePointUp;
        else if (dir == Vector2.down)
            firePoint.localPosition = firePointDown;
        else if (dir == Vector2.left)
            firePoint.localPosition = firePointLeft;
        else if (dir == Vector2.right)
            firePoint.localPosition = firePointRight;
    }

    /// <summary>
    /// Gatilho de toque: jogador encostou → entra em ataque imediatamente.
    /// </summary>
    /*private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Alerta todos os inimigos (opcional, mas mantém a consistência)
            GlobalAlertManager.TriggerAlert(other.transform.position);

            // Força a troca para o estado de ataque, não importa o estado atual
            if (currentState != attackState)
                ChangeState(attackState);
        }
    }*/

    /// <summary>
    /// Efeito visual de "chacoalho" (shake) por curta duração.
    /// </summary>
    /// <param name="duration">Duração do shake.</param>
    /// <param name="intensity">Intensidade do deslocamento.</param>
    private IEnumerator ShakeEffect(float duration, float intensity = 0.1f)
    {
        Vector3 originalPos = transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float offsetX = Random.Range(-intensity, intensity);
            float offsetY = Random.Range(-intensity, intensity);
            transform.position = originalPos + new Vector3(offsetX, offsetY, 0);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = originalPos;
    }
    private void OnEnemyDeath()
    {
        // Remove este inimigo do conjunto de alertados
        GlobalAlertManager.UnregisterAlertedEnemy(this);
    }
    public void ReturnToPatrol() => ChangeState(patrolState);
}