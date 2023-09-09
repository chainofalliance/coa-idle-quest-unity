using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using static Blockchain;
using static UnityEditor.Progress;

public class CreatePartyScreen : MonoBehaviour
{
    [SerializeField] private Config Config;
    [SerializeField] private Button Entry, Shop;
    [SerializeField] private GameObject ScreenShop;
    [SerializeField] private GameObject PartyPrefab, PartyRoot, HeroPrefab, HeroRoot;

    // Start is called before the first frame update
    void Start()
    {
        Entry.onClick.AddListener(OnEntrySelected);
        Shop.onClick.AddListener(OnShopSelected);
        
    }

    public event Action<List<Blockchain.Hero>, List<ConsumableEntry>, Rarity> PartyUpdated;
    private List<Blockchain.Hero> heroes = new();

    private void OnEntrySelected()
    {
        List<Blockchain.Hero> party = new();
        List<ConsumableEntry> consumables = new();
        PartyUpdated?.Invoke(heroes, consumables, Rarity.Common);
    }

    private void OnShopSelected()
    {
        ScreenShop.SetActive(true);
        var shopScreen = ScreenShop.GetComponent<ScreenShop>();
        shopScreen.ReturnBack += OnReturnFromShop;
    }

    private async void OnReturnFromShop()
    {
        ScreenShop.SetActive(false);

        await GetHeroData();
    }

    private async UniTask<List<Blockchain.Hero>> GetHeroData()
    {

        var response = await Blockchain.Instance.GetHeroes();

        foreach (var hero in response)
        {
            var heroEntry = Instantiate(HeroPrefab, HeroRoot.transform).GetComponent<CharacterEntry>();

            heroEntry.Initialize(hero, Config);
            heroEntry.Selected += OnHeroSelected;
         
        }

        return response;
    }

    private async void OnHeroSelected(CharacterEntry hero)
    {
        var response = await Blockchain.Instance.GetHeroes();

        foreach (var heroResponse in response)
        {
            if(heroResponse.Id == hero.HeroID)
            {
                var heroEntry = Instantiate(HeroPrefab, PartyRoot.transform).GetComponent<CharacterEntry>();
                heroEntry.Initialize(heroResponse, Config);
                 heroes.Add(heroResponse);
            }
        }
    }



    // Update is called once per frame
    void Update()
    {
        
    }
}
