using UnityEngine;
using UnityEngine.InputSystem;

public class SaveController : MonoBehaviour
{
    public bool pageActive;

    void Update()
    {
        if (!pageActive)
            return;

        Debug.Log("Save Page");
        /*if (Keyboard.current.dKey.wasPressedThisFrame)
        {
            Debug.Log("Player page input");
        }*/
    }
}
