using UnityEngine;
using UnityEngine.InputSystem;

public class UIInputRouter : MonoBehaviour
{
    public static UIInputRouter Instance;

    public System.Action OnRight;
    public System.Action OnLeft;
    public System.Action OnUp;
    public System.Action OnDown;

    public System.Action OnToggleMenu;

    public System.Action OnTabLeft;   // Q
    public System.Action OnTabRight;  // E

    private void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if (GameInputManager.Instance == null || !GameInputManager.Instance.uiInputEnabled)
            return;

        if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
            OnRight?.Invoke();

        if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
            OnLeft?.Invoke();

        if (Keyboard.current.upArrowKey.wasPressedThisFrame)
            OnUp?.Invoke();

        if (Keyboard.current.downArrowKey.wasPressedThisFrame)
            OnDown?.Invoke();

        if (Keyboard.current.tabKey.wasPressedThisFrame)
        {
            Debug.Log("TAB detectado");
            OnToggleMenu?.Invoke();
        }

        // 🔥 NOVO
        if (Keyboard.current.qKey.wasPressedThisFrame)
            OnTabLeft?.Invoke();

        if (Keyboard.current.eKey.wasPressedThisFrame)
            OnTabRight?.Invoke();
    }
}