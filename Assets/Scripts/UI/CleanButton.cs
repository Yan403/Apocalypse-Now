using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Botão que NÃO permite clique se o mouse sair e depois voltar enquanto pressionado.
/// O pressionamento só é válido se iniciado e finalizado dentro do botão.
/// </summary>
public class CleanButton : Button
{
    private bool isPointerInside = false;
    private bool isPointerDown = false;
    private bool hasExitedWhilePressed = false;   // 🆕 indica que o mouse saiu após pressionar

    public override void OnPointerEnter(PointerEventData eventData)
    {
        base.OnPointerEnter(eventData);
        isPointerInside = true;

        // Se o mouse voltou enquanto ainda pressionado, marca que houve saída
        if (isPointerDown)
            hasExitedWhilePressed = true;

        UpdateButtonState();
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        base.OnPointerExit(eventData);
        isPointerInside = false;

        // Se estava pressionado ao sair, invalida o pressionamento visual
        if (isPointerDown)
            hasExitedWhilePressed = true;

        UpdateButtonState();
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);
        isPointerDown = true;
        hasExitedWhilePressed = false;   // reset ao iniciar um novo pressionamento
        UpdateButtonState();
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        // Se o mouse saiu em algum momento, NÃO dispara o clique
        if (hasExitedWhilePressed)
        {
            // Cancela o pressionamento visual e lógico
            isPointerDown = false;
            hasExitedWhilePressed = false;
            UpdateButtonState();

            // Não chama base.OnPointerUp para evitar que o evento onClick seja acionado
            return;
        }

        // Caso contrário, comportamento normal (clique válido)
        base.OnPointerUp(eventData);
        isPointerDown = false;
        hasExitedWhilePressed = false;
        UpdateButtonState();
    }

    private void UpdateButtonState()
    {
        if (!IsInteractable())
        {
            DoStateTransition(SelectionState.Disabled, true);
            return;
        }

        if (isPointerInside && isPointerDown && !hasExitedWhilePressed)
            DoStateTransition(SelectionState.Pressed, true);
        else if (isPointerInside)
            DoStateTransition(SelectionState.Highlighted, true);
        else
            DoStateTransition(SelectionState.Normal, true);
    }
}