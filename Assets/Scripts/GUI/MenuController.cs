using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MenuController : MonoBehaviour
{
    [SerializeField] private List<Menu> Menus;

    private readonly Stack<Menu> backgroundMenus = new Stack<Menu>(4);
    private GameObject menuRoot;
    private Menu activeMenu;
    public bool HasActiveMenu => activeMenu != null;

    public bool Enabled
    {
        get => menuRoot.activeInHierarchy;

        set
        {
            menuRoot.SetActive(value);
        }
    }

    public Menu ActiveMenu
    {
        get
        {
            return activeMenu;
        }

        private set
        {
            if (activeMenu == value)
                return;

            if (activeMenu != null && backgroundMenus.Contains(activeMenu) == false)
            {
                activeMenu.Closed -= OnMenuClosed;
                activeMenu.Hide();
            }

            activeMenu = value;

            if (activeMenu != null)
            {
                activeMenu.Closed += OnMenuClosed;
            }
        }
    }

    public void Awake()
    {
    }

    private void OpenMenu(MenuParameters parameters)
    {

        backgroundMenus.Push(ActiveMenu);

        ActiveMenu = parameters.CreateMenu(menuRoot);
    }

    private void OnMenuClosed()
    {
        ActiveMenu.Show();

        ActiveMenu = backgroundMenus.Any() ? backgroundMenus.Pop() : null;
    }
}
