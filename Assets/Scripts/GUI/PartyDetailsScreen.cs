using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Networking.Types;
using UnityEngine.UI;
using static Blockchain;

public class PartyDetailsScreen : MonoBehaviour
{
    [SerializeField] private Button Back, Use, Return;
    [SerializeField] private TextMeshProUGUI ShardsAmount, ChallengeOverview, ChallengeCompleted;
    [SerializeField] private GameObject HeroPrefab, HeroRoot;

    private TimeSpan timeDifference;
    public Chromia.Buffer Id;
    public List<Blockchain.Hero> party;
    public List<Blockchain.Consumable> partyconsumables;
    public Blockchain.Rarity partyrarity;
    DateTime dateTime;
    public event Action ReturnBack;
    // Start is called before the first frame update
    void Start()
    {
        Back.onClick.AddListener(OnBackClicked);
        Use.onClick.AddListener(OnUseClicked);
        Use.onClick.AddListener(OnReturn);
    }

    private async void OnReturn()
    {
        var response = await Blockchain.Instance.RetreatFromExpedition(Id);
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

    public void InitializeExpedition(ExpeditionOverview exp)
    {
        Id = exp.Id;
        //ChallengeCompleted.text = $" A {exp.ActiveChallenge.Challenge.Difficulty} {exp.ActiveChallenge.Challenge.Type} challenge, happens in {exp.ActiveChallenge.Challenge.Terrain}, {exp.ActiveChallenge.Challenge.ClassAdvantage} has an advantage.";
        ChallengeCompleted.text = $" A {Blockchain.ChallengeDifficulty.Hard} {Blockchain.ChallengeType.Fight} challenge, happens in {Blockchain.Terrain.Savannah}";
        ChallengeOverview.text = $" A {exp.ActiveChallenge.Challenge} {exp.ActiveChallenge.Challenge.Type} challenge, happens in {exp.ActiveChallenge.Challenge.Terrain}, {exp.ActiveChallenge.Challenge.ClassAdvantage} has an advantage.";

         dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        dateTime = dateTime.AddSeconds(exp.ActiveChallenge.ArrivalAt).ToLocalTime();
        timeDifference = dateTime - DateTime.UtcNow;
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
        Debug.Log("Here");
        var shards = await RefreshShardsAmount();
        ShardsAmount.text = shards.ToString();

        var response = await Blockchain.Instance.GetConsumables();
        foreach (var backpack in response)
        {
            Debug.Log(backpack.Key.ToString() + " !! " + backpack.Value.ToString());
        }

        await GetBackPackData();
        var result = await Blockchain.Instance.GetShards();
        return result;
    }

    void Update()
    {
        timeDifference = dateTime - DateTime.UtcNow;
        if(timeDifference < TimeSpan.FromSeconds(5))
        {
        }
    }
}
