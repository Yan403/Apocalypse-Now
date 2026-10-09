using UnityEngine;

public class SearchState : EnemyState
{
    public float searchRadius = 4f;
    public float searchTime = 20f;
    private float timer;
    private Vector2 wanderTarget;

    public override void Enter()
    {
        timer = searchTime;
        SetNewWanderPoint();
    }

    public override void DoUpdate()
    {
        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            GlobalAlertManager.UnregisterAlertedEnemy(enemy);   // 🆕
            enemy.ReturnToPatrol();   // volta a patrulhar
        }

        Vector2 rawDirection = (wanderTarget - (Vector2)enemy.transform.position).normalized;
        Vector2 cleanDirection = enemy.FilterFourWayDirection(rawDirection);
        enemy.rb.linearVelocity = cleanDirection * 2f; // Velocidade de busca

        if (Vector2.Distance(enemy.transform.position, wanderTarget) < 0.3f)
        {
            enemy.rb.linearVelocity = Vector2.zero;
            SetNewWanderPoint();
        }
    }

    void SetNewWanderPoint()
    {
        // Vaga apenas em um raio de busca ao redor da última posição vista [12]
        wanderTarget = enemy.lastKnownPlayerPos + (Random.insideUnitCircle * searchRadius);
    }

    public override void Exit() { }
}
