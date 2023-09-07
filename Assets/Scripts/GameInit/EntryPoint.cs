using UnityEngine;

public class EntryPoint : MonoBehaviour
{

    [SerializeField] GameObject MainMenuPrefab;
    // Start is called before the first frame update
    void Start()
    {
        Instantiate(MainMenuPrefab);


    }

    // Update is called once per frame
    void Update()
    {
        
    }
}