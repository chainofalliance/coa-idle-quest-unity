using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class ScreenMainMenu : Menu
{
    [SerializeField] private Button startButton;

    private Dictionary<string, int> dictionary = new Dictionary<string, int>();
    // Start is called before the first frame update
    void Start()
    {
        startButton.onClick.AddListener(OnClickButton);
        //startButton.OnClickAsync += OnClickButton();
        //await GetData();

    }

    public async void OnClickButton()
    {
;
    }

   


    // Update is called once per frame
    void Update()
    {
 
    }
}
