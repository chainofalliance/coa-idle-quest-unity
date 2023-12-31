using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class BackPackMenu : MonoBehaviour
{
    [SerializeField] private Button Back, Equip, Add;
    [SerializeField] private GameObject BackPackRoot, BackpackPrefab, ConsumablesRoot, ConsumablesPrefab, InventoryRoot, InventoryPrefab;
    [SerializeField] private Config config;
    [SerializeField] private GameObject SlotPrefab;

    private List<ConsumableEntry> consumables = new();
    private List<ConsumableEntry> backPackconsumables = new();
    private List<BackPackEntry> backpacks = new();
    private List<GameObject> freeinventorySlots = new();
    private List<GameObject> freeconsumeSlots = new();

    private ConsumableEntry selectedConsumable;
    private BackPackEntry selectedbackpack;
    private int consumableSlots;

    public event Action<List<Blockchain.Consumable>, string, Blockchain.Rarity> EquippedReturn;
    public event Action ReturnBack;

    async void Start()
    {
        Back.onClick.AddListener(OnBackClicked);
        Equip.onClick.AddListener(OnEquipClicked);
        Add.onClick.AddListener(OnAddClicked);

        await GetConsumableData();
    }

    private void OnEquipClicked()
    {
        EquippedReturn?.Invoke(backPackconsumables.Select(c => c.consumableType).ToList(), selectedbackpack.EntryName, ConvertStringToRarity(selectedbackpack.EntryName));
    }

    private void OnAddClicked()
    {
        if (freeconsumeSlots.Count <= 0 || selectedConsumable == null)
            return;

        for (int i = freeconsumeSlots.Count - 1; i >= 0; i--)
        {
            Destroy(freeconsumeSlots[i]);
        }

        var consumable = Instantiate(ConsumablesPrefab, ConsumablesRoot.transform).GetComponent<ConsumableEntry>();
        consumable.Initialize(selectedConsumable.consumableType, config);
        backPackconsumables.Add(consumable);

        var freeSlotsCount = consumableSlots - backPackconsumables.Count;

        for (var i = freeSlotsCount; i > 0; i--)
        {
            var slot = Instantiate(SlotPrefab, ConsumablesRoot.transform);
            freeconsumeSlots.Add(slot);
        }

        RemoveConsumableFromInventory(selectedConsumable);
        selectedConsumable = null;
    }
    private void CheckInventoryList(List<Blockchain.ConsumableEntry> list, int i, ConsumableEntry backPackEntry)
    {
        if (list[i].Consumable == backPackEntry.consumableType)
        {
           if( list[i].Amount > 1)
            {
                list[i].Amount--;
            }
           else
            {
                list.RemoveAt(i);
            }
            return;
        }
    }

    private async void RemoveConsumableFromInventory(ConsumableEntry entry)
    {
        var response = await Blockchain.Instance.GetConsumables();
        var slotsCount = 40;

        RefreshInventory();

        foreach (var consumable in backPackconsumables)
        {
            for (int i = response.Count - 1; i >= 0; i--)
            {
                CheckInventoryList(response, i, consumable);
            }
        }

        foreach (var consumable in response)
        {
            for (long i = consumable.Amount; i > 0; i--)
            {
                var consumableEntry = Instantiate(ConsumablesPrefab, InventoryRoot.transform).GetComponent<ConsumableEntry>();

                consumableEntry.Initialize(consumable.Consumable, config);
                consumableEntry.Selected += OnConsumableSelected;
                consumables.Add(consumableEntry);
            }
        }

        for (var i = slotsCount; i > 0; i--)
        {
            var slot = Instantiate(SlotPrefab, InventoryRoot.transform);
            freeinventorySlots.Add(slot);
        }
    }

    private void OnBackClicked()
    {
        ReturnBack?.Invoke();
    }

    private async UniTask<List<Blockchain.BackpackEntry>> GetBackPackData()
    {
        var response = await Blockchain.Instance.GetBackpacks();

        foreach (var backpack in response)
        {
            Debug.Log(backpack.Backpack + " " + backpack.Amount);
            for (long i = backpack.Amount ; i > 0; i--)
            {
                var backPackEntry = Instantiate(BackpackPrefab, BackPackRoot.transform).GetComponent<BackPackEntry>();
                backPackEntry.Initialize(backpack.Backpack);
                backPackEntry.Selected += OnBackPackSelected;
                backpacks.Add(backPackEntry);
            }
        }

        backpacks.First().OnBackPackSelected();

        return response;
    }

    private async UniTask<List<Blockchain.ConsumableEntry>> GetConsumableData()
    {
        var response = await Blockchain.Instance.GetConsumables();
        var slotsCount = 40;
        foreach (var consumable in response)
        {
            for (long i = consumable.Amount ; i > 0; i--)
            {
                slotsCount--;
                var consumableEntry = Instantiate(ConsumablesPrefab, InventoryRoot.transform).GetComponent<ConsumableEntry>();
                consumableEntry.Initialize(consumable.Consumable, config);
                consumableEntry.Selected += OnConsumableSelected;
                consumables.Add(consumableEntry);
            }
        }

        for (var i = slotsCount; i > 0; i--)
        {
            var slot = Instantiate(SlotPrefab, InventoryRoot.transform);
            freeinventorySlots.Add(slot);
        }

        return response;
    }

    private void RefreshInventory()
    {
        for (int i = consumables.Count - 1; i >= 0; i--)
        {
            consumables[i].Destroy();
            consumables.RemoveAt(i);
        }

        for (int i = freeinventorySlots.Count - 1; i >= 0; i--)
        {
            Destroy(freeinventorySlots[i]);
        }


    }

    private void OnConsumableSelected(ConsumableEntry entry)
    {
        foreach (var consumable in consumables)
        {
            if (consumable != entry)
            {
                consumable.Deselect();
                continue;
            }
            selectedConsumable = consumable;
        }
    }

    private void ClearBackpackSlots()
    {

        for (int i = freeconsumeSlots.Count - 1; i >= 0; i--)
        {
            Destroy(freeconsumeSlots[i]);
        }

        for (int i = backPackconsumables.Count - 1; i >= 0; i--)
        {
            backPackconsumables[i].Destroy();
            backPackconsumables.RemoveAt(i);
        }
    }

    private async void OnBackPackSelected(BackPackEntry entry)
    {
        RefreshInventory();

        await GetConsumableData();
        ClearBackpackSlots();

        foreach (BackPackEntry bp in backpacks)
        {
            if (bp != entry)
            {
                bp.Deselect();
                continue;
            }

            selectedbackpack = entry;
            var rarity = ConvertStringToRarity(bp.EntryName);
            consumableSlots = config.BackpackSlots.FirstOrDefault(x => x.rarity == rarity).consumableSlots;

            for (var i = consumableSlots; i > 0; i--)
            {
                var slot = Instantiate(SlotPrefab, ConsumablesRoot.transform);
                freeconsumeSlots.Add(slot);
            }
        }
    }

    private Blockchain.Rarity ConvertStringToRarity(string str)
    {
        StringComparison comp = StringComparison.InvariantCultureIgnoreCase;
        var buf = Blockchain.Rarity.Common;

        foreach (Blockchain.Rarity rarity in (Blockchain.Rarity[])Enum.GetValues(typeof(Blockchain.Rarity)))
        {
            if (str.Contains(rarity.ToString(), comp))
            {
                buf = rarity;
            }
        }

        return buf;
    }

    private async void OnEnable()
    {
        RefreshInventory();
        ClearBackpackSlots();

        foreach (var bp in backpacks)
            bp.Deselect();

        await GetBackPackData();
        await GetConsumableData();
    }

    void Update()
    {
        Add.interactable = selectedbackpack != null;
    }
}
