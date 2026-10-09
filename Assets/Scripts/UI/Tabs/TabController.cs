using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class TabController : MonoBehaviour
{
    public GameObject[] tabs;
    public GameObject[] selectedIcons;

    public MenuPageController pageController;

    public int currentTab;

    public Key nextKey = Key.RightArrow;
    public Key previousKey = Key.LeftArrow;

    [Header("Navigation")]
    public bool wrapTabs = true;

    void Start()
    {
        ActivateTab(0);
    }

    void Update()
    {
        if (!GameInputManager.Instance.uiInputEnabled)
            return;

        if (Keyboard.current[nextKey].wasPressedThisFrame)
            ChangeTab(1);

        if (Keyboard.current[previousKey].wasPressedThisFrame)
            ChangeTab(-1);
    }

    void ChangeTab(int direction)
    {
        int newTab = currentTab + direction;

        if (wrapTabs)
        {
            if (newTab >= tabs.Length)
                newTab = 0;

            if (newTab < 0)
                newTab = tabs.Length - 1;
        }
        else
        {
            newTab = Mathf.Clamp(newTab, 0, tabs.Length - 1);
        }

        ActivateTab(newTab);
    }

    public void ActivateTab(int index)
    {
        currentTab = index;

        for (int i = 0; i < tabs.Length; i++)
        {
            selectedIcons[i].SetActive(i == index);

            Image[] images = tabs[i].GetComponentsInChildren<Image>();

            foreach (var img in images)
                img.color = (i == index) ? Color.white : Color.gray;
        }

        pageController.ActivatePage(index);
    }
}