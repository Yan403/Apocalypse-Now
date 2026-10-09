using UnityEngine;

[CreateAssetMenu(fileName = "New Item")]
public class ItemSO : ScriptableObject
{
    public string itemName;
    [TextArea] public string itemDescription;
    public Sprite icon;

    public bool isGun;

    [Header("Passive Effect")]
    [Tooltip("Efeito passivo que será ativado enquanto este item estiver selecionado.")]
    public PassiveEffect passiveEffect;

    [Header("Tipo de Uso")]
    [Tooltip("Se verdadeiro, o item NÃO pode ser usado como consumível, apenas aplica seu efeito passivo.")]
    public bool isPassiveOnly = false;   // NOVA FLAG
}
