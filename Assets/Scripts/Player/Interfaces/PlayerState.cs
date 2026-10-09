using UnityEngine;

public abstract class PlayerState : MonoBehaviour
{
    protected PlayerController player;
    public bool isComplete { get; protected set; } // [10, 11]

    public virtual void Setup(PlayerController player)
    {
        this.player = player;
    }
    public abstract void Enter(); // Chamado ao entrar no estado [8]
    public abstract void DoUpdate(); // Chamado a cada frame [8]
    public abstract void Exit(); // Chamado ao sair do estado [8]
}
