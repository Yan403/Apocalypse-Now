using UnityEngine;
using System.Collections.Generic;

public class GlobalAlertManager : MonoBehaviour
{
    public static bool IsGlobalAlertActive { get; private set; }
    private static List<EnemyController> allEnemies = new List<EnemyController>();
    private static HashSet<EnemyController> alertedEnemies = new HashSet<EnemyController>();

    public static event System.Action OnAlertActivated;
    public static event System.Action OnAlertDeactivated;

    public static void RegisterEnemy(EnemyController enemy) => allEnemies.Add(enemy);

    /// <summary>
    /// Chamado quando o jogador é detectado (FOV ou toque). Dispara o alerta e os efeitos sonoros.
    /// </summary>
    public static void TriggerDetection(Vector2 playerLastPos)
    {
        if (!IsGlobalAlertActive)
        {
            IsGlobalAlertActive = true;
            OnAlertActivated?.Invoke();
        }

        foreach (var enemy in allEnemies)
            enemy.NotifyAlert(playerLastPos);
    }

    /// <summary>
    /// Chamado quando o jogador causa dano ao inimigo. Alerta os inimigos mas NÃO repete o som.
    /// </summary>
    public static void TriggerAlert(Vector2 playerLastPos)
    {
        if (!IsGlobalAlertActive)
        {
            IsGlobalAlertActive = true;
            // Não invoca OnAlertActivated
        }

        foreach (var enemy in allEnemies)
            enemy.NotifyAlert(playerLastPos);
    }

    /// <summary>
    /// Registra um inimigo que entrou em estado de alerta (ataque ou busca).
    /// </summary>
    public static void RegisterAlertedEnemy(EnemyController enemy)
    {
        alertedEnemies.Add(enemy);
    }

    /// <summary>
    /// Remove um inimigo do estado de alerta (morreu ou tempo de busca acabou).
    /// Se não houver mais inimigos alertados, reseta o alerta global.
    /// </summary>
    public static void UnregisterAlertedEnemy(EnemyController enemy)
    {
        alertedEnemies.Remove(enemy);
        if (alertedEnemies.Count == 0 && IsGlobalAlertActive)
        {
            ResetAlert();
        }
    }

    public static void ResetAlert()
    {
        if (IsGlobalAlertActive)
        {
            IsGlobalAlertActive = false;
            OnAlertDeactivated?.Invoke();
        }

        alertedEnemies.Clear();
        foreach (var enemy in allEnemies)
            enemy.ReturnToPatrol();
    }
}