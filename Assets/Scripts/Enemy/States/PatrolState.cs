using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class PatrolState : EnemyState
{
    public PatrolPoint[] waypoints;
    public float speed = 2f;
    private int currentPoint = 0;

    private bool isWaiting = false;
    private bool isMovingForward = true;
    public override void Enter()
    {
        isWaiting = false;
    }

    public override void DoUpdate()
    {
        if (waypoints.Length == 0 || isWaiting) return;

        MoverParaPonto();

        // Verifica se chegou ao destino [2]
        if (Vector2.Distance(enemy.transform.position, waypoints[currentPoint].point.position) < 0.2f)
        {
            StartCoroutine(ObservarNoPonto());
        }
    }

    void MoverParaPonto()
    {
        if(waypoints.Length > 1)
        {
            Transform target = waypoints[currentPoint].point;
            float speed_point = waypoints[currentPoint].PointSpeed;
            Vector2 direction = (target.position - enemy.transform.position).normalized;

            // FILTRO: Garante apenas 4 direções
            Vector2 cleanDirection = enemy.FilterFourWayDirection(direction);

            // Aplica velocidade física para o Controller detectar e animar [4]
            enemy.rb.linearVelocity = cleanDirection * speed_point;
            // Durante o movimento, o Animator segue a velocidade física
            enemy.anim.SetBool("isWalking", true);

            // animação acompanha velocidade
            enemy.anim.speed = speed_point;
        }

        else
        {
            StartCoroutine(ObservarNoPonto());
        }
    }
    IEnumerator ObservarNoPonto()
    {
        isWaiting = true;
        enemy.rb.linearVelocity = Vector2.zero; // Para o movimento físico
        
        enemy.anim.speed = 1f;
        enemy.anim.SetBool("isWalking", false); // Ativa o Idle

        // --- LÓGICA DE DIREÇÃO MANUAL ---
        // Forçamos o Animator a olhar para a direção definida no ponto, 
        // ignorando para onde ele estava andando antes.
        Vector2 lookDir = waypoints[currentPoint].lookDirection;
        if (lookDir != Vector2.zero)
        {
            enemy.anim.SetFloat("X", lookDir.x);
            enemy.anim.SetFloat("Y", lookDir.y);

            enemy.facingDirection = lookDir;
        }

        yield return new WaitForSeconds(waypoints[currentPoint].waitTime);

        CalcularProximoPonto();
        isWaiting = false;
    }

    void CalcularProximoPonto()
    {
        // Lógica de Ida e Volta (Ping-Pong)
        if (isMovingForward)
        {
            if (currentPoint + 1 < waypoints.Length)
            {
                currentPoint++;
            }
            else
            {
                isMovingForward = false;
                currentPoint--; // Começa a voltar
            }
        }
        else
        {
            if (currentPoint - 1 >= 0)
            {
                currentPoint--;
            }
            else
            {
                isMovingForward = true;
                currentPoint++; // Começa a ir para frente novamente
            }
        }
    }
    public override void Exit()
    {
        StopAllCoroutines();
        enemy.rb.linearVelocity = Vector2.zero;
    }
}

[System.Serializable]
public class PatrolPoint
{
    public Transform point;       // Onde o inimigo deve ir
    public Vector2 lookDirection; // Para onde ele deve olhar ao chegar (ex: -1, 0 para esquerda)
    public float PointSpeed;
    public float waitTime; // tempo que o inimigo observa neste ponto
}