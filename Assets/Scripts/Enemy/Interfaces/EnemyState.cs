using UnityEngine;

public abstract class EnemyState : MonoBehaviour
{
    protected EnemyController enemy;
    public virtual void Setup(EnemyController controller) => enemy = controller;
    public abstract void Enter();
    public abstract void DoUpdate();
    public abstract void Exit();
}