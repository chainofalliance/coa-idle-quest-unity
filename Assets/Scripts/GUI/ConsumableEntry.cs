using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ConsumableEntry : MonoBehaviour, IEntry
{
    [SerializeField] private Image Icon;
    [SerializeField] private Button Entry;
    [SerializeField] private GameObject Active;

    public string EntryName { get; set; }
    public Sprite Image { get; set; }
    public bool IsSelect { get; set; }
    public long Price { get; set; }
    public Blockchain.Consumable consumableType { get; set; }
    public event Action<ConsumableEntry> Selected;

    public void Initialize(Blockchain.Consumable consumable, Config configuration, int price = default)
    {
        Active.SetActive(false);
        consumableType = consumable;
        EntryName = consumable.ToString();
        Price = price;
        Icon.sprite = configuration.ConsumableIcons.FirstOrDefault(x => x.consumableType == consumable).sprite;
        Image = Icon.sprite;
    }

    void Start()
    {
        Entry.onClick.AddListener(ConsumableSelected);
    }

    private void ConsumableSelected()
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

    // Update is called once per frame
    void Update()
    {

    }
}
