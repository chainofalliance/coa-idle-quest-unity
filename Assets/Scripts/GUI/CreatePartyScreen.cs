using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using static Blockchain;

public class CreatePartyScreen : MonoBehaviour
{
    [SerializeField] private Config Config;
    [SerializeField] private Button Entry, Shop, Backpack, Back;
    [SerializeField] private GameObject ScreenShop, BackPackScreen;
    [SerializeField] private GameObject PartyPrefab, PartyRoot, HeroPrefab, HeroRoot;

    private List<PartyEntry> createdParties = new();
    private List<CharacterEntry> heroEntries = new();
    private List<CharacterEntry> heroesInParty = new();
    private List<Blockchain.Hero> heroes = new();
    private List<Consumable> consumables = new();
    private Blockchain.Rarity backpackEquiped;
    private string backpackEquipedName;

    async void Start()
    {
        Entry.onClick.AddListener(OnEntrySelected);
        Back.onClick.AddListener(OnBackSelected);
        Shop.onClick.AddListener(OnShopSelected);
        Backpack.onClick.AddListener(OnBackpackSelected);
        var shopScreen = ScreenShop.GetComponent<ScreenShop>();
        shopScreen.ReturnBack += OnReturnFromShop;
        var response = await Blockchain.Instance.GetBackpacks();
        backpackEquipedName = response.FirstOrDefault().Backpack;

    }

    public event Action<List<Blockchain.Hero>, List<Consumable>, string, Blockchain.Rarity, ExpeditionOverview> PartyUpdated;
    public event Action ReturnClicked;

    private void OnBackSelected()
    {
        ReturnClicked?.Invoke();
        ResetEntries();
    }

    private async void OnEnable()
    {
        ResetEntries();

        await GetHeroData();
    }

    private void OnEntrySelected()
    {
        ExpeditionOverview exp = default;
        PartyUpdated?.Invoke(heroes, consumables, backpackEquipedName, backpackEquiped, exp);
    }

    private void OnBackpackSelected()
    {
        BackPackScreen.SetActive(true);
        var backPackScreen = BackPackScreen.GetComponent<BackPackMenu>();
        backPackScreen.ReturnBack += OnReturnFromBackPack;
        backPackScreen.EquippedReturn += OnEquippedReturn;
    }

    private void ResetEntries()
    {
        for (int i = heroEntries.Count - 1; i >= 0; i--)
        {
            heroEntries[i].Destroy();
            heroEntries.RemoveAt(i);
        }

        for (int i = heroesInParty.Count - 1; i >= 0; i--)
        {
            heroesInParty[i].Destroy();
            heroesInParty.RemoveAt(i);
        }

        heroes.Clear();
    }

    private void OnShopSelected()
    {
        ScreenShop.SetActive(true);
    }

    private async void OnReturnFromShop()
    {
        ScreenShop.SetActive(false);
        ResetEntries();
        await GetHeroData();
    }

    private void OnReturnFromBackPack()
    {
        BackPackScreen.SetActive(false);
    }

    private void OnEquippedReturn(List<Consumable> consumablesEquipped, string backpackName, Blockchain.Rarity backpack)
    {
        BackPackScreen.SetActive(false);
        consumables = consumablesEquipped;
        backpackEquiped = backpack;
        backpackEquipedName = backpackName;
    }

    private async UniTask<List<Blockchain.Hero>> GetHeroData()
    {
        var response = await Blockchain.Instance.GetHeroes();

        foreach (var hero in response)
        {
            var heroEntry = Instantiate(HeroPrefab, HeroRoot.transform).GetComponent<CharacterEntry>();
            heroEntry.Initialize(hero, Config, false, false, !hero.IsDeployed);
            heroEntry.Selected += OnHeroSelected;
            heroEntries.Add(heroEntry);
        }

        return response;
    }

    private void OnHeroDeleted(CharacterEntry hero)
    {
        for (int i = heroesInParty.Count - 1; i >= 0; i--)
        {
            if (hero.HeroID != heroesInParty[i].HeroID)
            {
                continue;
            }
            var h = heroEntries.FirstOrDefault(h => h.HeroID == hero.HeroID);
            h.Deselect();
            h.IsAvailable = true;
            heroes.Remove(hero.Hero);
            heroesInParty[i].Destroy();
            heroesInParty.RemoveAt(i);
        }
    }
    private async void OnHeroSelected(CharacterEntry hero)
    {
        if (heroesInParty.Count >= 3 || hero.IsAvailable == false || heroesInParty.Any(x => x.HeroID == hero.HeroID))
        {
            hero.Deselect();
            return;
        }

        var response = await Blockchain.Instance.GetHeroes();

        foreach (var heroResponse in response)
        {
            if (heroResponse.Id == hero.HeroID)
            {
                var heroEntry = Instantiate(HeroPrefab, PartyRoot.transform).GetComponent<CharacterEntry>();
                heroEntry.Initialize(heroResponse, Config, true, true, true);
                heroEntry.Deleted += OnHeroDeleted;
                heroes.Add(heroResponse);
                heroesInParty.Add(heroEntry);
            }
        }
    }



    // Update is called once per frame
    void Update()
    {
        Entry.interactable = heroesInParty.Count >= 3;
    }
}
