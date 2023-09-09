using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using Chromia;
using System.Linq;
using static Blockchain;
using System;

public class ScreenShop : MonoBehaviour
{
    [SerializeField] private Image HeroesNavPanel, ConsumablesNavPanel, BackpacksNavPanel;
    [SerializeField] private GameObject HeroPanel, ConsumablesPanel, BackpacksPanel;
    [SerializeField] private Button Hero, Consumables, Backpacks, Buy, Back;
    [SerializeField] private Sprite ActivePanel, PassivePanel;
    [SerializeField] private TextMeshProUGUI ShardsAmount;

    [SerializeField] private GameObject BackpackPrefab, BackpackRoot, HeroPrefab, HeroRoot, ConsumablePrefab, ConsumableRoot;
    [SerializeField] private TextMeshProUGUI Title, Price;
    [SerializeField] private Config config;


    private List<BackPackEntry> backpacks = new();
    private List<CharacterEntry> heroes = new();
    private List<ConsumableEntry> consumables = new();

    async void Start()
    {
        Hero.onClick.AddListener(OnHeroPanelOpen);
        Consumables.onClick.AddListener(OnConsumablePanelOpen);
        Backpacks.onClick.AddListener(OnBackPacksPanelOpen);
        Buy.onClick.AddListener(OnBuyClicked);
        Back.onClick.AddListener(OnBackClicked);

        OnHeroPanelOpen();
  
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

            heroEntry.Initialize(hero.Hero, config, hero.Price);
            heroEntry.Selected += OnHeroSelected;
            heroes.Add(heroEntry);
        }

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
    }

    private void OnBackClicked()
    {
        UnityEngine.Debug.Log("OnBack");
        ReturnBack?.Invoke();
    }

    private async void OnBuyClicked()
    {
        var backpack = backpacks.FirstOrDefault(x => x.IsSelect == true);

        if (backpack != null)
        {
            var response = await Blockchain.Instance.BuyBackpack(backpack.BackPackname);

            if (response.Status == TransactionReceipt.ResponseStatus.Confirmed)
            {
                ClearBackPAcks();
                await GetBackPackData();
            }
        }

        var hero = heroes.FirstOrDefault(x => x.IsSelect == true);

        if (hero != null)
        {
            var response = await Blockchain.Instance.BuyHero(hero.HeroID, hero.HeroName);

            if (response.Status == TransactionReceipt.ResponseStatus.Confirmed)
            {
                ClearHeroes();
                await GetHeroData();
            }
        }

        var cons = consumables.FirstOrDefault(x => x.IsSelect == true);

        if (cons != null)
        {
            var response = await Blockchain.Instance.BuyConsumable(cons.consumableType);

            if (response.Status == TransactionReceipt.ResponseStatus.Confirmed)
            {
                ClearConsumables();
                await GetConsumableData();
            }
        }
        await Blockchain.Instance.RefreshShop();
        await RefreshShardsAmount();
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

    private void OnBackPackSelected(BackPackEntry entry)
    {
        foreach (var bp in backpacks)
        {
            if (bp != entry)
            {
                bp.Deselect();
                continue;
            }
            Title.text = entry.BackPackname;
            Price.text = entry.BackPackprice.ToString();
        }
    }

    private void OnHeroSelected(CharacterEntry hero)
    {
        foreach (var heroentry in heroes)
        {
            if (heroentry != hero)
            {
                heroentry.Deselect();
                continue;
            }
            Title.text = hero.HeroName;
            Price.text = hero.Price.ToString();
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

            Title.text = entry.consumableType.ToString();
            Price.text = entry.ConsumablePrice.ToString();
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
    }

    private async void OnHeroPanelOpen()
    {
        await RefreshShardsAmount();

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
    }

        private async UniTask<long> RefreshShardsAmount()
    {
        var result = await Blockchain.Instance.GetShards();
        return result;
    }
}
