using UnityEngine;
using UnityEngine.InputSystem;

public class TabKeyboardController : MonoBehaviour
{
    public TabController tabController;

    public int currentTab = 0;
    public int tabCount = 3;

    public Key button = Key.RightArrow;
    public Key previousButton = Key.LeftArrow;

    public bool wrapTabs = true;

    void Update()
    {
        if (GameManager.IsPaused == false)
            return;

        if (Keyboard.current[button].wasPressedThisFrame)
        {
            ChangeTab(1);
        }

        if (Keyboard.current[previousButton].wasPressedThisFrame)
        {
            ChangeTab(-1);
        }
    }

    void ChangeTab(int direction)
    {

        currentTab = tabController.currentTab; // sincroniza
        currentTab += direction;

        if (currentTab >= tabCount)
        {
            if (wrapTabs)
                currentTab = 0;
            else
                currentTab = tabCount - 1;
        }

        if (currentTab < 0)
        {
            if (wrapTabs)
                currentTab = tabCount - 1;
            else
                currentTab = 0;
        }

        tabController.ActivateTab(currentTab);
    }
}