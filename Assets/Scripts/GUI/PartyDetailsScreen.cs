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
    [SerializeField] private TextMeshProUGUI OptionOne, OptionTwo;
    [SerializeField] private Button OptionOneButton, OptionTwoButton;
    [SerializeField] private GameObject HeroPrefab, HeroRoot;
    [SerializeField] private TextMeshProUGUI Timer;
    [SerializeField] private GameObject ConsumablesRoot, ConsumablesPrefab;

    private TimeSpan timeDifference;
    public Chromia.Buffer Id;
    public List<Blockchain.Hero> party;
    public List<CharacterEntry> heroes;
    public List<Consumable> partyconsumables;
    public Rarity partyrarity;

    DateTime dateTime;
    public event Action ReturnBack;
    private ExpeditionOverview Expedition;
    // Start is called before the first frame update
    void Start()
    {
        Back.onClick.AddListener(OnBackClicked);
        Use.onClick.AddListener(OnUseClicked);
        Use.onClick.AddListener(OnReturn);
        OptionOneButton.onClick.AddListener(OnFirstClicked);
        OptionTwoButton.onClick.AddListener(OnSecondClicked);
    }

    private async void OnReturn()
    {
        var response = await Blockchain.Instance.RetreatFromExpedition(Id);
    }


    public void InitializeParty(List<Blockchain.Hero> heroesParty,
                                List<ConsumableEntry> consumables,
                                Config configuration,
                                Rarity rarity)
    {
        party = heroesParty;
        partyconsumables.AddRange(consumables.Select(c => c.consumableType));
        partyrarity = rarity;

        ClearParty();

        for (int i = 0; i < heroesParty.Count; i++)
        {
            var heroEntry = Instantiate(HeroPrefab, HeroRoot.transform).GetComponent<CharacterEntry>();
            heroEntry.Initialize(heroesParty[i], configuration, false, true, true);
            heroes.Add(heroEntry);
        }
    }

    private void ClearParty()
    {
        for (int i = heroes.Count - 1; i >= 0; i--)
        {
            heroes[i].Destroy();
            heroes.RemoveAt(i);
        }
    }

    public void InitializeExpedition(ExpeditionOverview exp)
    {
        Expedition = exp;
        Id = exp.Id;

   
        ChallengeCompleted.text = $" A {Blockchain.ChallengeDifficulty.Hard} {Blockchain.ChallengeType.Fight} challenge, happens in {Blockchain.Terrain.Savannah}";

        OptionOne.text = exp.NextChallenges.First().Type.ToString();
        OptionTwo.text = exp.NextChallenges.ElementAt(1).Type.ToString();


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

    private async void OnFirstClicked()
    {
        await Blockchain.Instance.SelectExpeditionChallenge(Expedition.NextChallenges.FirstOrDefault().Id);
    }

    private async void OnSecondClicked()
    {
        await RefreshStuff();
    }

    private async UniTask<List<BackpackEntry>> GetBackPackData()
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

        var response = await Blockchain.Instance.GetConsumables();

        await GetBackPackData();
        var result = await Blockchain.Instance.GetShards();
        return result;
    }

    void Update()
    {
       timeDifference = dateTime - DateTime.UtcNow;

       Timer.text = $"{timeDifference.Minutes} minutes left";
    }
}
