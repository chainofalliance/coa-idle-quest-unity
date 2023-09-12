using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using Chromia;
using System.Linq;
using static Blockchain;
using System;
using static UnityEngine.EventSystems.EventTrigger;

public class ScreenShop : MonoBehaviour
{
    [SerializeField] private Image HeroesNavPanel, ConsumablesNavPanel, BackpacksNavPanel, HeroImage, ConsumableImage;
    [SerializeField] private GameObject HeroPanel, ConsumablesPanel, BackpacksPanel, Shard;
    [SerializeField] private Button Hero, Consumables, Backpacks, Buy, Back, Cheatshards;
    [SerializeField] private Sprite ActivePanel, PassivePanel;
    [SerializeField] private TextMeshProUGUI ShardsAmount;

    [SerializeField] private GameObject BackpackPrefab, BackpackRoot, HeroPrefab, HeroRoot, ConsumablePrefab, ConsumableRoot;
    [SerializeField] private TextMeshProUGUI Title, Price;
    [SerializeField] private Config config;


    private List<BackPackEntry> backpacks = new();
    private List<CharacterEntry> heroes = new();
    private List<ConsumableEntry> consumables = new();

    private long shardsAmount;

    async void Start()
    {
        Hero.onClick.AddListener(OnHeroPanelOpen);
        Cheatshards.onClick.AddListener(OnCheat);
        Consumables.onClick.AddListener(OnConsumablePanelOpen);
        Backpacks.onClick.AddListener(OnBackPacksPanelOpen);
        Buy.onClick.AddListener(OnBuyClicked);
        Back.onClick.AddListener(OnBackClicked);

        OnHeroPanelOpen();
        await RefreshShardsAmount();
    }

    private async void OnCheat()
    {
        await Blockchain.Instance.CheatShards(500);
        shardsAmount = await Blockchain.Instance.GetShards();
        ShardsAmount.text = shardsAmount.ToString();
    }

    public event Action ReturnBack;

    private async UniTask<List<BackpackShopEntry>> GetBackPackData()
    {
        var response = await Blockchain.Instance.GetBackpacksFromShop();

        foreach (var backpack in response)
        {
            var backPackEntry = Instantiate(BackpackPrefab, BackpackRoot.transform).GetComponent<BackPackEntry>();

            backPackEntry.Initialize(backpack.Backpack, backpack.Price);
            backPackEntry.Selected += OnBackPackSelected;
            backpacks.Add(backPackEntry);
        }

        return response;
    }

    private async UniTask<List<Blockchain.HeroShopEntry>> GetHeroData()
    {

        var response = await Blockchain.Instance.GetHeroSelectionFromShop();

        foreach (var hero in response)
        {
            var heroEntry = Instantiate(HeroPrefab, HeroRoot.transform).GetComponent<CharacterEntry>();

            heroEntry.Initialize(hero.Hero, config, false, false, true, hero.Price);
            heroEntry.Selected += OnHeroSelected;
            heroes.Add(heroEntry);
        }

        OnHeroSelected(heroes.First());

        return response;
    }

    private async UniTask<List<ConsumableShopEntry>> GetConsumableData()
    {

        var response = await Blockchain.Instance.GetConsumablesFromShop();

        foreach (var consumable in response)
        {
            var consumableEntry = Instantiate(ConsumablePrefab, ConsumableRoot.transform).GetComponent<ConsumableEntry>();

            consumableEntry.Initialize(consumable.Consumable, config, consumable.Price);
            consumableEntry.Selected += OnConsumableSelected;
            consumables.Add(consumableEntry);
        }

        return response;
    }

    void Update()
    {
        Shard.SetActive(Price.text != "");
        Buy.interactable = Price.text != "" && shardsAmount >= int.Parse(Price.text);
    }

    private void OnBackClicked()
    {
        ReturnBack?.Invoke();
    }

    private async void OnBuyClicked()
    {
        TransactionReceipt response = default;
        Title.text = "Connecting to blockchain. Please, wait";

        var backpack = backpacks.FirstOrDefault(x => x.IsSelect == true);

        if (backpack != null)
            response = await Blockchain.Instance.BuyBackpack(backpack.EntryName);

        var hero = heroes.FirstOrDefault(x => x.IsSelect == true);
        if (hero != null)
            response = await Blockchain.Instance.BuyHero(hero.HeroID, hero.EntryName);

        if (response.Status == TransactionReceipt.ResponseStatus.Confirmed)
        {
            ClearHeroes();
            await GetHeroData();
            await RefreshShardsAmount();
            Title.text = "Purchase successful!";
        }

        var cons = consumables.FirstOrDefault(x => x.IsSelect == true);
        if (cons != null)
            response = await Blockchain.Instance.BuyConsumable(cons.consumableType);

        if (response.Status == TransactionReceipt.ResponseStatus.Confirmed)
        {
            await RefreshShardsAmount();
            Title.text = "Purchase successful!";
        }

    }

    private void ClearHeroes()
    {
        for (int i = heroes.Count - 1; i >= 0; i--)
        {
            heroes[i].Destroy();
            heroes.RemoveAt(i);
        }
    }

    private void ClearBackPAcks()
    {
        for (int i = backpacks.Count - 1; i >= 0; i--)
        {
            backpacks[i].Destroy();
            backpacks.RemoveAt(i);
        }
    }

    private void ClearConsumables()
    {
        for (int i = consumables.Count - 1; i >= 0; i--)
        {
            consumables[i].Destroy();
            consumables.RemoveAt(i);
        }
    }

    private void ClearList(List<IEntry> entries)
    {
        for (int i = entries.Count - 1; i >= 0; i--)
        {
            entries[i].Destroy();
            entries.RemoveAt(i);
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
            Title.text = entry.EntryName;
            Price.text = entry.Price.ToString();
        }
    }

    private void OnHeroSelected(CharacterEntry hero)
    {
        HeroImage.sprite = hero.HeroImage;
        foreach (var heroentry in heroes)
        {
            if (heroentry != hero)
            {
                heroentry.Deselect();
                continue;
            }
            Title.text = hero.EntryName;
            Price.text = hero.Price.ToString();
        }
    }

    private void OnConsumableSelected(ConsumableEntry entry)
    {
        ConsumableImage.sprite = entry.Image;
        foreach (var consumable in consumables)
        {
            if (consumable != entry)
            {
                consumable.Deselect();
                continue;
            }

            Title.text = entry.EntryName.ToString();
            Price.text = entry.Price.ToString();
        }
    }


    private async void OnBackPacksPanelOpen()
    {
        HeroesNavPanel.sprite = PassivePanel;
        ConsumablesNavPanel.sprite = PassivePanel;
        BackpacksNavPanel.sprite = ActivePanel;

        HeroPanel.SetActive(false);
        ConsumablesPanel.SetActive(false);
        BackpacksPanel.SetActive(true);

        foreach (var hero in heroes)
            hero.Deselect();
        foreach (var cs in consumables)
            cs.Deselect();

        ClearBackPAcks();

        await GetBackPackData();
        await RefreshShardsAmount();
    }

    private async void OnHeroPanelOpen()
    {
        HeroesNavPanel.sprite = ActivePanel;
        ConsumablesNavPanel.sprite = PassivePanel;
        BackpacksNavPanel.sprite = PassivePanel;

        HeroPanel.SetActive(true);
        ConsumablesPanel.SetActive(false);
        BackpacksPanel.SetActive(false);

        //var result = await RefreshShardsAmount();
        //ShardsAmount.text = result.ToString();

        foreach (var bp in backpacks)
            bp.Deselect();
        foreach (var cs in consumables)
            cs.Deselect();

        ClearHeroes();

        await GetHeroData();
        await RefreshShardsAmount();
    }

    private async void OnConsumablePanelOpen()
    {
        HeroesNavPanel.sprite = PassivePanel;
        ConsumablesNavPanel.sprite = ActivePanel;
        BackpacksNavPanel.sprite = PassivePanel;

        HeroPanel.SetActive(false);
        ConsumablesPanel.SetActive(true);
        BackpacksPanel.SetActive(false);

        foreach (var bp in backpacks)
            bp.Deselect();
        foreach (var hero in heroes)
            hero.Deselect();

        ClearConsumables();

        await GetConsumableData();
        await RefreshShardsAmount();
    }

    private async UniTask<long> RefreshShardsAmount()
    {
        shardsAmount = await Blockchain.Instance.GetShards();
        ShardsAmount.text = shardsAmount.ToString();
        Title.text = "";
        Price.text = "";

        return shardsAmount;
    }
}
