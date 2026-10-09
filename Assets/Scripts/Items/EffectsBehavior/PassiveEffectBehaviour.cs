/// <summary>
/// Classe base para lógica de efeitos passivos. Não é um MonoBehaviour – as instâncias são gerenciadas pelo PassiveEffectManager.
/// </summary>
public abstract class PassiveEffectBehaviour
{
    /// <summary> Referência ao PlayerController (definida no OnEquip). </summary>
    protected PlayerController player;

    /// <summary> Chamado quando o efeito é ativado. </summary>
    public virtual void OnEquip(PlayerController owner)
    {
        player = owner;
    }

    /// <summary> Chamado a cada frame pelo manager (apenas se o efeito estiver ativo). </summary>
    public virtual void OnUpdate() { }

    /// <summary> Chamado quando o efeito é removido. Use para limpeza. </summary>
    public virtual void OnUnequip() { }
}