using UnityEngine;

public class UIStateManager : MonoBehaviour
{
    public static UIStateManager Instance;

    public enum UIPage
    {
        Player,
        Inventory,
        Settings,
        Save
    }

    public UIPage currentPage;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetPage(UIPage page)
    {
        currentPage = page;
    }

    public bool IsPage(UIPage page)
    {
        return currentPage == page;
    }
}
