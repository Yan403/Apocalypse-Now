using UnityEngine;

public class GameInputManager : MonoBehaviour
{
    public static GameInputManager Instance;

    public bool gameplayInputEnabled = true;
    public bool uiInputEnabled = false;

    void Awake()
    {
        Instance = this;
    }

    public void EnableGameplay()
    {
        gameplayInputEnabled = true;
        uiInputEnabled = false;
    }

    public void EnableUI()
    {
        gameplayInputEnabled = false;
        uiInputEnabled = true;
    }
}
