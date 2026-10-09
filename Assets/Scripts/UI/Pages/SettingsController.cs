using UnityEngine;
using UnityEngine.InputSystem;

public class SettingsController : MonoBehaviour
{
    public bool pageActive;

    void Update()
    {
        if (!pageActive)
            return;

        Debug.Log("Settings Page");
        /*if (Keyboard.current.dKey.wasPressedThisFrame)
        {
            Debug.Log("Player page input");
        }*/
    }
}