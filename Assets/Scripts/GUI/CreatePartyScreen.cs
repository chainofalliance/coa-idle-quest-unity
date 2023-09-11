using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using static Blockchain;

public class CreatePartyScreen : MonoBehaviour
{
    [SerializeField] private Config Config;
    [SerializeField] private Button Entry, Shop, Backpack, Back;
    [SerializeField] private GameObject ScreenShop,BackPackScreen;
    [SerializeField] private GameObject PartyPrefab, PartyRoot, HeroPrefab, HeroRoot;

    private List<PartyEntry> createdParties = new();
    private List<CharacterEntry> heroEntries = new();
    private List<CharacterEntry> heroesInParty = new();
    private List<Blockchain.Hero> heroes = new();
    private List<ConsumableEntry> consumables = new();

    async void Start()
    {
        Entry.onClick.AddListener(OnEntrySelected);
        Back.onClick.AddListener(OnBackSelected);
        Shop.onClick.AddListener(OnShopSelected);
        Backpack.onClick.AddListener(OnBackpackSelected);
    }

    public event Action<List<Blockchain.Hero>, List<ConsumableEntry>, Rarity> PartyUpdated;
    public event Action ReturnClicked;

    private void OnBackSelected()
    {
        ReturnClicked?.Invoke();
    }

    private void OnEntrySelected()
    {
        List<ConsumableEntry> consumables = new();
        PartyUpdated?.Invoke(heroes, consumables, Rarity.Common);
    }

    private void OnBackpackSelected()
    {
        BackPackScreen.SetActive(true);
        var backPackScreen = BackPackScreen.GetComponent<BackPackMenu>();
        backPackScreen.ReturnBack += OnReturnFromBackPack;
        backPackScreen.EquippedReturn += OnEquippedReturn;
    }

    public async void InjectDependency(List<PartyEntry> parties)
    {
        createdParties = parties;
        UnityEngine.Debug.Log("Are parties injected");

        for (int i = heroesInParty.Count - 1; i >= 0; i--)
        {
            heroesInParty[i].Destroy();
            heroesInParty.RemoveAt(i);
        }

        for (int i = heroEntries.Count - 1; i >= 0; i--)
        {
            heroEntries[i].Destroy();
            heroEntries.RemoveAt(i);
        }

        await GetHeroData();
    }

    private void OnShopSelected()
    {
        ScreenShop.SetActive(true);
        var shopScreen = ScreenShop.GetComponent<ScreenShop>();
        shopScreen.ReturnBack += OnReturnFromShop;
    }

    private async  void OnReturnFromShop()
    {
        ScreenShop.SetActive(false);
        await GetHeroData();
    }

    private void OnReturnFromBackPack()
    {
        BackPackScreen.SetActive(false);
    }

    private void OnEquippedReturn(List<ConsumableEntry> consumablesEquipped)
    {
        BackPackScreen.SetActive(false);
        consumables = consumablesEquipped;
    }

    private async UniTask<List<Blockchain.Hero>> GetHeroData()
    {
        for (int i = heroEntries.Count - 1; i >= 0; i--)
        {
            heroEntries[i].Destroy();
            heroEntries.RemoveAt(i);
        }

        var response = await Blockchain.Instance.GetHeroes();

        foreach (var hero in response)
        {
            bool isAvailable = true;
            var heroEntry = Instantiate(HeroPrefab, HeroRoot.transform).GetComponent<CharacterEntry>();
            isAvailable = !createdParties.Any(x => x.party.Any(y => y.Id == hero.Id));
            UnityEngine.Debug.Log("IsAvailable" + isAvailable);
            heroEntry.Initialize(hero, Config, false, false, isAvailable);
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
