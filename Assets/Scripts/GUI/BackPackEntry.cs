using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BackPackEntry : MonoBehaviour//, Buyable
{
    [SerializeField] private GameObject Active;
    [SerializeField] private Button Entry;
    [SerializeField] private TextMeshProUGUI Name;

    public string BackPackname { get; set; }
    public bool IsSelect { get; set; }
    public int BackPackprice { get; set; }

    public event Action<BackPackEntry> Selected;

    void Start()
    {
        Entry.onClick.AddListener(OnBackPackSelected);
    }

    public void Initialize(string name, int price)
    {
        Name.text = name;
        BackPackname = name;
        BackPackprice = price;
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnBackPackSelected()
    {
        Active.SetActive(true);
        IsSelect = true;
        Selected?.Invoke(this);
    }

    public void Deselect()
    {
        IsSelect = false;
        Active.SetActive(false);
    }


    public void Destroy()
    {
        Destroy(gameObject);
    }

}
