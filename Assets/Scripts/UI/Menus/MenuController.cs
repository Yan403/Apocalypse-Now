using UnityEngine;
using UnityEngine.InputSystem;

public class MenuController : MonoBehaviour
{
    public GameObject menuCanvas;
    public TabController tabController;

    private PlayerController playerController;
    void Start()
    {
        menuCanvas.SetActive(false);

        if (tabController != null)
            tabController.ActivateTab(0);

        // Cache do PlayerController para evitar Find a cada frame
        playerController = FindFirstObjectByType<PlayerController>();
    }

    void Update()
    {
        if (Keyboard.current.tabKey.wasPressedThisFrame)
        {
            ToggleMenu();
        }
    }

    void ToggleMenu()
    {
        // Impede abrir/fechar o menu se o jogador estiver executando uma ação
        if (playerController != null && playerController.IsPerformingAction)
            return;

        bool open = !menuCanvas.activeSelf;

        menuCanvas.SetActive(open);

        if (open)
        {
            GameManager.PauseGame();
            GameInputManager.Instance.EnableUI();
            tabController.ActivateTab(0);
        }
        else
        {
            GameManager.ResumeGame();
            GameInputManager.Instance.EnableGameplay();
        }
    }
}