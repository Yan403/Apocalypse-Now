using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.Rendering.DebugUI;

public class PlayerController : MonoBehaviour
{
    public PlayerState currentState;

    public IdleState idleState;
    public IdleCrawlState idlecrawlState;
    public IdleArmedState idleArmedState;
    public CrouchState crouchState;

    public WalkState walkState;
    public WalkArmedState walkArmedState;
    public CrawlState crawlState;

    public PlayerAttackState playerAttackState;

    public HealthSystem health;

    [Header("Componentes")]
    public Rigidbody2D rb; // [18]
    public Animator anim; // [19]
    public Vector2 moveInput;

    [Header("Combat")]
    public Transform firePoint;
    public Vector2 firePointUp = new Vector2(0f, 0.4f);
    public Vector2 firePointDown = new Vector2(0f, -0.4f);
    public Vector2 firePointLeft = new Vector2(-0.4f, 0f);
    public Vector2 firePointRight = new Vector2(0.4f, 0f);

    [System.Serializable]
    public class DirectionalPunchData
    {
        public Vector2 boxSize = new Vector2(0.6f, 0.4f);
        public Vector2 offset = new Vector2(0f, 0.5f);
        public float distance = 0.5f;
    }

    [Header("Punch Hitbox (Directional)")]
    public DirectionalPunchData punchUp;
    public DirectionalPunchData punchDown;
    public DirectionalPunchData punchLeft;
    public DirectionalPunchData punchRight;

    [Header("Alignment")]
    public float attackAlignmentTolerance = 0f;

    [Header("Attack")]
    public float punchCooldown = 0.6f;
    public float punchStunDuration = 0.6f; // 🆕 NOVO

    [HideInInspector]
    public float nextAttackTime = 0f;

    public GunSO equippedWeapon;
    public UsableItemSO equippedItem;
    public bool isArmed = false;
    public bool isAttacking;

    public Vector2 processedInput;
    public Vector2 lastDirection = Vector2.down;
    private float horizontalLastTime;
    private float verticalLastTime;
    public float turnDelay = 0.1f;
    
    public bool isTurning = false;
    public bool IsPerformingAction => isTurning || (currentState is PlayerAttackState);

    [Header("Efeitos Passivos")]
    public PassiveEffectManager passiveEffectManager;  // Arraste o componente na Unity
    
    private ItemSO currentPassiveItem; // armazena o item passivo ativo
    public ItemSO CurrentPassiveItem => currentPassiveItem; // expõe para a UI

    [Header("I-Frames")]
    public float invincibilityDuration = 1.5f;   // duração da invencibilidade
    private Coroutine invincibilityCoroutine;

    [Header("Munição")]
    public int currentAmmo;
    public int maxAmmo;   // será preenchido pela arma equipada
    private Dictionary<GunSO, int> weaponAmmo = new Dictionary<GunSO, int>();
    private GunSO lastEquippedWeapon;   // para guardar ao trocar de arma

    [Header("Munição - UI")]
    public System.Action<int> OnAmmoChanged;   // dispara sempre que a munição muda

    // Para o piscar
    private SpriteRenderer[] playerRenderers;    // todos os renderers do jogador (corpo, arma, etc.)
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        health = GetComponent<HealthSystem>();
        passiveEffectManager = GetComponent<PassiveEffectManager>();

        // Cache dos renderers para piscar
        playerRenderers = GetComponentsInChildren<SpriteRenderer>();

        // I‑frames: inscrever no evento de dano
        if (health != null)
            health.OnDamaged += OnPlayerDamaged;

        health.OnDeath += OnPlayerDeath;
       
        if (equippedWeapon != null)
            OnWeaponChanged(equippedWeapon);
        else
            lastEquippedWeapon = null;

        // Inicializa os estados [12, 20]
        idleState.Setup(this);
        idlecrawlState.Setup(this);
        idleArmedState.Setup(this);
        crouchState.Setup(this);

        walkState.Setup(this);
        walkArmedState.Setup(this);
        crawlState.Setup(this);

        playerAttackState.Setup(this);

        ChangeState(idleState);
    }

    void Update()
    {
        if (DynamicMusicManager.IsFrozen)
            return;

        if (GameInputManager.Instance != null &&
        !GameInputManager.Instance.gameplayInputEnabled)
        {
            return;
        }
        // Precisamos processar o input antes que o estado o utilize

        Armed();

        ProcessFourWayInput();

        UpdateFirePoint();

        if (!isTurning && !isAttacking && processedInput != Vector2.zero)
        {
            bool viradaHorizontal = (processedInput.x == -lastDirection.x && processedInput.x != 0);
            bool viradaVertical = (processedInput.y == -lastDirection.y && processedInput.y != 0);

            if (viradaHorizontal || viradaVertical)
            {
                // Inicia a animação intermediária e trava o movimento
                StartCoroutine(TurnRoutine(processedInput));
            }
            else
            {
                // Se for apenas uma virada de 90º ou movimento novo, atualiza a memória normalmente
                lastDirection = processedInput;
            }
        }

        // 2. Se estiver virando, forçamos o movimento a ser zero para o personagem não "escorregar"
        if (isTurning)
        {
            processedInput = Vector2.zero;
        }

        if(!isTurning && Keyboard.current.spaceKey.wasPressedThisFrame && Time.time >= nextAttackTime)
        {
            ChangeState(playerAttackState);
        }

        UpdateAnimator();

        if (currentState != null)
        {
            currentState.DoUpdate(); // [21]
        }
    }
    public void ChangeState(PlayerState newState)
    {
        if (currentState != null)
        {
            currentState.Exit(); // [22]
        }
        
        currentState = newState;
        currentState.Enter(); // [23]
    }
    private void ProcessFourWayInput()
    {
        // Se o jogador aperta duas direções, prioriza a última [Conversa Anterior]
        if (moveInput.x != 0 && moveInput.y != 0)
        {
            if (horizontalLastTime > verticalLastTime)
            {
                processedInput = new Vector2(moveInput.x, 0);
            }
            else
            {
                processedInput = new Vector2(0, moveInput.y);
            }
        }
        else
        {
            processedInput = moveInput;
        }
    }
    
    void UpdateAnimator()
    {
        if (isAttacking)
            return;

        Vector2 animDirection = processedInput;

        if (animDirection == Vector2.zero)
        {
            animDirection = lastDirection;
        }

        anim.SetFloat("InputX", animDirection.x);
        anim.SetFloat("InputY", animDirection.y);
        anim.SetFloat("Speed", processedInput.magnitude);
    }
    
    
    public void OnMove(InputAction.CallbackContext context)
    {
        Vector2 rawInput = context.ReadValue<Vector2>(); // [5]

        // Registra o tempo para decidir a prioridade da tecla
        if (rawInput.x != 0 && moveInput.x == 0)
        {
            horizontalLastTime = Time.time;
        }

        if (rawInput.y != 0 && moveInput.y == 0)
        {
            verticalLastTime = Time.time;
        }
        
        moveInput = rawInput;
    }
    private IEnumerator TurnRoutine(Vector2 targetDir)
    {
        isTurning = true;
        int currentPosture = anim.GetInteger("Posture"); // [1, 2]

        // CASO 1: RASTEJANDO (Posture 2) -> Fica de joelhos, vira e rasteja de novo
        if (currentPosture == 2)
        {
            // 1. Fica de joelhos primeiro (Posture 1 - Crouch)
            anim.SetInteger("Posture", 1);
            yield return new WaitForSeconds(turnDelay);

            // 2. Vira para a nova direção ainda de joelhos
            lastDirection = targetDir;
            yield return new WaitForSeconds(turnDelay);

            // 3. Volta para a postura de rastejo para continuar o movimento
            anim.SetInteger("Posture", 2);
        }
        // CASO 2: EM PÉ (Posture 0) -> Lógica Clássica MG2 (Frame intermediário)
        else
        {
            Vector2 intermediate = lastDirection;

            // Regra MG2: 180º Horizontal passa por BAIXO / 180º Vertical passa por DIREITA
            if (targetDir.x == -lastDirection.x && targetDir.x != 0) intermediate = Vector2.down;
            else if (targetDir.y == -lastDirection.y && targetDir.y != 0) intermediate = Vector2.right;

            lastDirection = intermediate;
            yield return new WaitForSeconds(turnDelay);

            lastDirection = targetDir;
        }

        isTurning = false;
    }
    void OnPlayerDeath()
    {
        Debug.Log("Player morreu");

        // Remove efeito passivo ao morrer
        SetPassiveItem(null);

        // aqui você pode:
        // trocar estado
        // tocar animação
        // reiniciar fase
    }

    public bool Armed()
    {
        if(equippedWeapon != null)
        {
            isArmed = true;
        }
        else
        {
            isArmed = false;
        }

        return isArmed;
    }
    public void UpdateFirePoint()
    {
        Vector2 dir = lastDirection;

        if (dir == Vector2.up)
            firePoint.localPosition = firePointUp;

        else if (dir == Vector2.down)
            firePoint.localPosition = firePointDown;

        else if (dir == Vector2.left)
            firePoint.localPosition = firePointLeft;

        else if (dir == Vector2.right)
            firePoint.localPosition = firePointRight;
    }
    public DirectionalPunchData GetPunchData(Vector2 direction)
    {
        if (direction == Vector2.up) return punchUp;
        if (direction == Vector2.down) return punchDown;
        if (direction == Vector2.left) return punchLeft;
        return punchRight;
    }
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        Vector2 direction = lastDirection == Vector2.zero ? Vector2.down : lastDirection;
        var data = GetPunchData(direction);

        Vector2 origin = (Vector2)transform.position
                       + direction * data.distance
                       + data.offset;

        float angle = 0f;

        if (direction == Vector2.up) angle = 90f;
        else if (direction == Vector2.down) angle = -90f;
        else if (direction == Vector2.left) angle = 180f;

        Matrix4x4 rotationMatrix = Matrix4x4.TRS(origin, Quaternion.Euler(0, 0, angle), Vector3.one);
        Gizmos.matrix = rotationMatrix;

        Gizmos.DrawWireCube(Vector3.zero, data.boxSize);
    }
    public static bool IsAxisAligned(Transform attacker, Transform target, Vector2 direction, float tolerance)
    {
        float dx = Mathf.Abs(attacker.position.x - target.position.x);
        float dy = Mathf.Abs(attacker.position.y - target.position.y);

        // ataque vertical → exige alinhamento em X
        if (direction == Vector2.up || direction == Vector2.down)
            return dx <= tolerance;

        // ataque horizontal → exige alinhamento em Y
        if (direction == Vector2.left || direction == Vector2.right)
            return dy <= tolerance;

        return false;
    }
    public void SetPassiveItem(ItemSO newItem)
    {
        Debug.Log($"[PlayerController] SetPassiveItem iniciado. Manager: {passiveEffectManager}");
        if (passiveEffectManager == null)
        {
            Debug.LogError("[PlayerController] passiveEffectManager é nulo!");
            return;
        }

        passiveEffectManager.ClearAllEffects();
        Debug.Log("[PlayerController] Efeitos antigos limpos.");

        currentPassiveItem = newItem; // 🔥 guarda o item atual (pode ser null)

        if (newItem != null && newItem.passiveEffect != null)
        {
            Debug.Log($"[PlayerController] Adicionando efeito do item '{newItem.itemName}'...");
            passiveEffectManager.AddEffect(newItem.passiveEffect);
        }
        else
        {
            Debug.Log("[PlayerController] Nenhum efeito para adicionar (item nulo ou passiveEffectData nulo).");
        }
    }
    private void OnPlayerDamaged()
    {
        // Se já houver i‑frames ativos, reinicie o cronômetro (evita acumular)
        if (invincibilityCoroutine != null)
            StopCoroutine(invincibilityCoroutine);

        invincibilityCoroutine = StartCoroutine(InvincibilityRoutine());
    }
    private IEnumerator InvincibilityRoutine()
    {
        // Ativa invencibilidade
        health.invincible = true;

        // Piscar enquanto durar a invencibilidade
        float elapsed = 0f;
        bool visible = true;
        while (elapsed < invincibilityDuration)
        {
            // Alterna visibilidade dos sprites
            visible = !visible;
            foreach (var sr in playerRenderers)
                sr.enabled = visible;

            yield return new WaitForSeconds(0.1f);   // intervalo de piscar
            elapsed += 0.1f;
        }

        // Garante que todos fiquem visíveis ao final
        foreach (var sr in playerRenderers)
            sr.enabled = true;

        // Desativa invencibilidade
        health.invincible = false;
        invincibilityCoroutine = null;
    }
    /// <summary>
    /// Adiciona munição à reserva da arma passada. Se for a arma equipada, atualiza currentAmmo e a UI.
    /// </summary>
    public bool AddAmmoForWeapon(GunSO weapon, int amount)
    {
        if (weapon == null) weapon = equippedWeapon;
        if (weapon == null) return false;

        // Se a arma ainda não tem reserva, inicializa com a munição atual (ou 0)
        if (!weaponAmmo.ContainsKey(weapon))
            weaponAmmo[weapon] = (equippedWeapon == weapon) ? currentAmmo : 0;

        int currentReserve = weaponAmmo[weapon];

        // Verifica se já atingiu o máximo
        if (currentReserve >= weapon.maxAmmo)
            return false;

        int newTotal = currentReserve + amount;
        // Limita ao máximo da arma
        if (newTotal > weapon.maxAmmo)
            newTotal = weapon.maxAmmo;

        weaponAmmo[weapon] = newTotal;

        // Se é a arma equipada, atualiza currentAmmo e UI
        if (equippedWeapon == weapon)
        {
            currentAmmo = Mathf.Clamp(newTotal, 0, maxAmmo);
            OnAmmoChanged?.Invoke(currentAmmo);
        }

        return true;
    }

    /// <summary>
    /// Chamado sempre que a arma equipada muda.
    /// </summary>
    public void OnWeaponChanged(GunSO newWeapon)
    {
        // Guarda a munição da arma anterior
        if (lastEquippedWeapon != null)
            weaponAmmo[lastEquippedWeapon] = currentAmmo;

        if (newWeapon != null)
        {
            maxAmmo = newWeapon.maxAmmo;
            // Recupera munição guardada ou usa a inicial
            if (weaponAmmo.TryGetValue(newWeapon, out int savedAmmo))
                currentAmmo = Mathf.Clamp(savedAmmo, 0, maxAmmo);
            else
                currentAmmo = Mathf.Clamp(newWeapon.startingAmmo, 0, maxAmmo);
        }
        else
        {
            maxAmmo = 0;
            currentAmmo = 0;
        }

        lastEquippedWeapon = newWeapon;
        OnAmmoChanged?.Invoke(currentAmmo);
    }

    /// <summary>
    /// Consome 1 unidade de munição da arma equipada. Deve ser chamado apenas quando currentAmmo > 0.
    /// </summary>
    public void UseAmmo(int amount = 1)
    {
        if (equippedWeapon == null) return;
        currentAmmo -= amount;
        weaponAmmo[equippedWeapon] = currentAmmo;
        OnAmmoChanged?.Invoke(currentAmmo);
    }
}
