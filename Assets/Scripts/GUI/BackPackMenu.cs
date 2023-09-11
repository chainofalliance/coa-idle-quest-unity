using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using static Blockchain;
using static UnityEngine.EventSystems.EventTrigger;

public class BackPackMenu : MonoBehaviour
{
    [SerializeField] private Button Back, Equip;
    [SerializeField] private GameObject BackPackRoot, BackpackPrefab, ConsumablesRoot, ConsumablesPrefab, InventoryRoot, InventoryPrefab;
    [SerializeField] private Config config;

    private List<ConsumableEntry> consumables = new();
    private List<BackPackEntry> backpacks = new();

    public event Action ReturnBack;

    async void Start()
    {
        Back.onClick.AddListener(OnBackClicked);
        Equip.onClick.AddListener(OnEquipClicked);
     
    }

    private async void OnEquipClicked()
    {
        await GetBackPackData();
        await GetConsumableData();
    }

    private void OnBackClicked()
    {
        ReturnBack?.Invoke();
    }

    private async UniTask<Dictionary<string,int>> GetBackPackData()
    {
        var response = await Blockchain.Instance.GetBackpacks();
        UnityEngine.Debug.Log(response.Count);
        //foreach (var backpack in response)
        //{
        //    var backPackEntry = Instantiate(BackpackPrefab, BackpackRoot.transform).GetComponent<BackPackEntry>();

        //    backPackEntry.Initialize(backpack.Backpack, backpack.Price);
        //    backPackEntry.Selected += OnBackPackSelected;
        //    backpacks.Add(backPackEntry);
        //}

        return response;
    }

    private async UniTask<Dictionary<Consumable, int>> GetConsumableData()
    {

        var response = await Blockchain.Instance.GetConsumables();

        foreach (var consumable in response)
        {
            var consumableEntry = Instantiate(ConsumablesPrefab, ConsumablesRoot.transform).GetComponent<ConsumableEntry>();

            consumableEntry.Initialize(consumable.Key, config);
            consumableEntry.Selected += OnConsumableSelected;
            consumables.Add(consumableEntry);
        }

        return response;
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
        }
    }

    private void OnBackPackSelected(BackPackEntry entry)
    {
        foreach (BackPackEntry bp in backpacks)
        {
            if (bp != entry)
            {
                bp.Deselect();
                continue;
            }

        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
