using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BackPackEntry : MonoBehaviour, IEntry
{
    [SerializeField] private GameObject Active;
    [SerializeField] private Button Entry;
    [SerializeField] private TextMeshProUGUI Name;

    public string EntryName { get; set; }
    public bool IsSelect { get; set; }
    public long Price { get; set; }

    public event Action<BackPackEntry> Selected;

    void Start()
    {
        Entry.onClick.AddListener(OnBackPackSelected);
    }

    public void Initialize(string name, int price = default)
    {
        Name.text = name;
        EntryName = name;
        Price = price;
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
