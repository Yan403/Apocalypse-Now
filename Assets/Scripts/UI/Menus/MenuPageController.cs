using UnityEngine;

public class MenuPageController : MonoBehaviour
{
    public GameObject[] pages;
    private PlayerPageController[] pageControllers;

    void Awake()
    {
        // Cache dos controllers para evitar GetComponent a cada troca
        pageControllers = new PlayerPageController[pages.Length];
        for (int i = 0; i < pages.Length; i++)
        {
            if (pages[i] != null)
                pageControllers[i] = pages[i].GetComponent<PlayerPageController>();
        }
    }

    public void ActivatePage(int index)
    {
        for (int i = 0; i < pages.Length; i++)
        {
            bool active = (i == index);
            pages[i].SetActive(active);

            // Notifica o controller da página (se existir)
            if (pageControllers[i] != null)
                pageControllers[i].SetActive(active);
        }

        if (UIStateManager.Instance != null)
        {
            UIStateManager.Instance.SetPage((UIStateManager.UIPage)index);
        }
    }
}


/*
using UnityEngine;

public class MenuPageController : MonoBehaviour
{
    public GameObject[] pages;

    public void ActivatePage(int index)
    {
        for (int i = 0; i < pages.Length; i++)
        {
            pages[i].SetActive(i == index);
        }

        if (UIStateManager.Instance != null)
        {
            UIStateManager.Instance.SetPage((UIStateManager.UIPage)index);
        }
    }
}
*/