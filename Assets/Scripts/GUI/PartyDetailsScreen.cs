using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Blockchain;

public class PartyDetailsScreen : MonoBehaviour
{
    [SerializeField] private Button Back, Use;
    [SerializeField] private TextMeshProUGUI ShardsAmount;
    [SerializeField] private GameObject HeroPrefab, HeroRoot;


    public List<Blockchain.Hero> party;
    public List<Blockchain.Consumable> partyconsumables;
    public Blockchain.Rarity partyrarity;

    public event Action ReturnBack;
    // Start is called before the first frame update
    void Start()
    {
        Back.onClick.AddListener(OnBackClicked);
        Use.onClick.AddListener(OnUseClicked);
    }


    public void InitializeParty(List<Blockchain.Hero> heroesParty, List<ConsumableEntry> consumables, Config configuration, Blockchain.Rarity rarity)
    {
        party = heroesParty;
        partyconsumables.AddRange(consumables.Select(c => c.consumableType));
        partyrarity = rarity;

        for (int i = 0; i < heroesParty.Count; i++)
        {
            var heroEntry = Instantiate(HeroPrefab, HeroRoot.transform).GetComponent<CharacterEntry>();
            heroEntry.Initialize(heroesParty[i], configuration);
        }
    }


    private void OnBackClicked()
    {
        ReturnBack?.Invoke();
    }

    private async void OnUseClicked()
    {
        await RefreshStuff();
    }

    private async UniTask<Dictionary<string, int>> GetBackPackData()
    {
        var response = await Blockchain.Instance.GetBackpacks();

        //foreach (var backpack in response)
        //{
        //    Debug.Log(backpack.Key/* + " !! " + backpack.Value.ToString()*/);
        //}

        return response;
    }


    private async UniTask<long> RefreshShardsAmount()
    {
        var result = await Blockchain.Instance.GetShards();
        return result;
    }

    private async UniTask<long> RefreshStuff()
    {
        var shards = await RefreshShardsAmount();

        ShardsAmount.text = shards.ToString();

        await GetBackPackData();
        var result = await Blockchain.Instance.GetShards();
        return result;
    }

    void Update()
    {

    }
}
