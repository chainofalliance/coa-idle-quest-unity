using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Chromia;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Networking.Types;
using UnityEngine.UI;
using static Blockchain;
using static UnityEngine.Networking.UnityWebRequest;

public class PartyDetailsScreen : MonoBehaviour
{
    [SerializeField] private Button Back, Use, Return, TimerReturn;
    [SerializeField] private TextMeshProUGUI ShardsAmount, ChallengeOverview, ChallengeCompleted;
    [SerializeField] private TextMeshProUGUI OptionOne, OptionTwo;
    [SerializeField] private Button OptionOneButton, OptionTwoButton, Consumables, Artifacts;
    [SerializeField] private GameObject HeroPrefab, HeroRoot, QuestResults, QuestTask;
    [SerializeField] private TextMeshProUGUI Timer;
    [SerializeField] private GameObject ConsumablesRoot, ConsumablesPrefab, SlotPrefab;
    [SerializeField] private Sprite ActivePanel, PassivePanel;
    [SerializeField] private Image ArtifactsNavPanel, ConsumablesNavPanel;


    public Chromia.Buffer Id;
    public List<Blockchain.Hero> party;
    public List<CharacterEntry> heroes;
    public List<Consumable> partyconsumables;
    public Rarity partyrarity;


    DateTime dateTime;
    public event Action ReturnBack;
    private ExpeditionOverview expeditionOverview;
    private Expedition expeditionDetails;
    private int consSlots;
    private int artifSlots;
    private List<GameObject> freeinventorySlots = new();
    private TimeSpan timeDifference;
    private Config config;
    private List<ConsumableEntry> consumables = new();
    private ConsumableEntry selectedConsumable;
    private Challenge challenge;
    private State state;
    private State prevState;

    private State State
    {
        get
        {
            return state;
        }
        set
        {
            if (value == state)
                return;

            state = value;
            if (state == State.Selectable && challenge != null)
            {
                if (prevState == State.Traveling)
                {
                    ChallengeOverview.text = "You've reached the challenge. What will you do?";
                    OptionOne.text = "Resolve";
                    OptionTwo.text = "Sneak";
                }
                else
                {
                    Debug.Log("Selectable state set");
                    SetNextChallengeOptions();
                }

            }
            if (state == State.Traveling)
            {
                ChallengeOverview.text += $"You've chosen {challenge.Difficulty} challenge of {challenge.Level} level in the {challenge.Terrain} where you face the {challenge.Type}." +
                    $" {challenge.ClassAdvantage} has an advantage.\n";
            }

            prevState = state;
        }
    }

    void Start()
    {
        Back.onClick.AddListener(OnBackClicked);
        Use.onClick.AddListener(OnUseClicked);
        TimerReturn.onClick.AddListener(OnReturn);
        Return.onClick.AddListener(OnReturn);
        Consumables.onClick.AddListener(OnConsumablesClicked);
        Artifacts.onClick.AddListener(OnArtifactsClicked);
        OptionOneButton.onClick.AddListener(OnFirstClicked);
        OptionTwoButton.onClick.AddListener(OnSecondClicked);
    }

    private void OnConsumablesClicked()
    {
        ArtifactsNavPanel.sprite = PassivePanel;
        ConsumablesNavPanel.sprite = ActivePanel;

        RefreshBackPack(consSlots);
    }

    private async void OnArtifactsClicked()
    {
        ArtifactsNavPanel.sprite = ActivePanel;
        ConsumablesNavPanel.sprite = PassivePanel;

        expeditionDetails = await Blockchain.Instance.GetExpeditionDetails(Id);
        RefreshBackPack(artifSlots);
    }

    private async void OnReturn()
    {
        await Blockchain.Instance.RetreatFromExpedition(Id);
        await Blockchain.Instance.FinishExpedition(Id);
        ReturnBack?.Invoke();
    }


    private async UniTask<TransactionReceipt> OnFinish()
    {
        var result = await Blockchain.Instance.FinishExpedition(Id);
        ReturnBack?.Invoke();
        return result;
    }

    public void InitializeParty(List<Blockchain.Hero> heroesParty,
                                List<ConsumableEntry> consumables,
                                Config configuration,
                                Rarity rarity)
    {
        config = configuration;
        party = heroesParty;
        partyconsumables.AddRange(consumables.Select(c => c.consumableType));
        partyrarity = rarity;
        consSlots = configuration.BackpackSlots.FirstOrDefault(x => x.rarity == rarity).consumableSlots;
        artifSlots = configuration.BackpackSlots.FirstOrDefault(x => x.rarity == rarity).lootSlots;
        ClearParty();

        for (int i = 0; i < heroesParty.Count; i++)
        {
            var heroEntry = Instantiate(HeroPrefab, HeroRoot.transform).GetComponent<CharacterEntry>();
            heroEntry.Initialize(heroesParty[i], configuration, false, true, true);
            heroes.Add(heroEntry);
        }
    }

    private void RefreshBackPack(int slots)
    {
        RefreshInventory();
        RefreshStuff();

        if (slots == artifSlots)
        {
            var list = expeditionDetails.Challenges.Select(x => x.Loot);
        }
        else
        {
            var list = expeditionDetails.Consumables;

            foreach (var consumable in list)
            {
                slots--;
                var consumableEntry = Instantiate(ConsumablesPrefab, ConsumablesRoot.transform).GetComponent<ConsumableEntry>();
                consumableEntry.Initialize(consumable, config);
                consumableEntry.Selected += OnConsumableSelected;

                consumables.Add(consumableEntry);
            }
        }

        for (var i = slots; i > 0; i--)
        {
            var slot = Instantiate(SlotPrefab, ConsumablesRoot.transform);
            freeinventorySlots.Add(slot);
        }
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

    private void ClearParty()
    {
        for (int i = heroes.Count - 1; i >= 0; i--)
        {
            heroes[i].Destroy();
            heroes.RemoveAt(i);
        }
    }

    public async void InitializeExpedition(ExpeditionOverview exp)
    {
        Id = exp.Id;
        var result = await RefreshExpedition();
        expeditionOverview = result.FirstOrDefault(x => x.Id == Id);
        expeditionDetails = await RefreshDetails();

        if (challenge == null)
        {
            SetNextChallengeOptions();
        }

        if (exp != default)
            OnConsumablesClicked();

    }

    private async UniTask<Expedition> RefreshDetails()
    {
        var activeChallenge = expeditionOverview.ActiveChallenge;

        var details = await Instance.GetExpeditionDetails(Id);
        challenge = details.Challenges.FirstOrDefault(x => x.Id == activeChallenge?.Challenge?.Id);

        if (challenge != null)
        {
            dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddMilliseconds(activeChallenge.ArrivalAt);
        }

        return details;
    }

    private void OnBackClicked()
    {
        ReturnBack?.Invoke();
    }

    private void SetNextChallengeOptions()
    {
        var ending = expeditionOverview.NextChallenges.Count > 1 ? "s" : "";
        ChallengeOverview.text = $"You have {expeditionOverview.NextChallenges.Count} way{ending}:\n ";

        foreach (var challenge in expeditionOverview.NextChallenges)
        {
            ChallengeOverview.text += $"{challenge.Difficulty} challenge of {challenge.Level} level in the {challenge.Terrain} where you face the {challenge.Type}." +
                $" {challenge.ClassAdvantage} has an advantage.\n";
        }

        OptionOne.text = "First";
        OptionTwo.text = expeditionOverview.NextChallenges.Count > 1 ? "Second" : "";
    }

    private async void OnUseClicked()
    {
        await Instance.UseConsumable(Id, selectedConsumable.consumableType);

        expeditionDetails = await RefreshDetails();
        foreach (var hero in expeditionDetails.Party)
        {
            foreach (var partyHero in heroes)
            {
                if (hero.Id == partyHero.HeroID)
                {
                    partyHero.Health = hero.Health;
                }
            }
        }

        RefreshStuff();
    }

    private async void OnFirstClicked()
    {
        if (OptionOne.text == "First")
        {
            await Instance.SelectExpeditionChallenge(expeditionOverview.NextChallenges.FirstOrDefault().Id);
            InitializeExpedition(expeditionOverview);
            QuestResults.SetActive(false);
        }
        else if (OptionOne.text == "Resolve")
        {
            Debug.Log("Resolve");
            SetChallengeResults(ChallengeAction.Resolve);
        }
    }

    private async void OnSecondClicked()
    {
        if (OptionTwo.text == "Second")
        {
            await Instance.SelectExpeditionChallenge(expeditionOverview.NextChallenges.ElementAt(1).Id);
            InitializeExpedition(expeditionOverview);
            QuestResults.SetActive(false);
        }
        else if (OptionTwo.text == "Sneak")
        {
            Debug.Log("Skip");
            SetChallengeResults(ChallengeAction.Skip);
        }
    }

    private async UniTask<List<ExpeditionOverview>> RefreshExpedition()
    {
        var result = await Instance.GetActiveExpeditions();
        return result;
    }

    private long RefreshShardsAmount()
    {
        long shards = 0;
        foreach (var r in expeditionDetails.Challenges)
        {
            var result = r.Loot;
            foreach (var loot in result)
            {
                if (loot.Type == Loot.Shards)
                {
                    shards += loot.Amount;
                }
            }
        }
        return shards;
    }

    private async void SetChallengeResults(ChallengeAction action)
    {
        var result = await Blockchain.Instance.GetActiveExpeditions();
        expeditionOverview = result.FirstOrDefault(x => x.Id == Id);

        var advanceResult = await Instance.AdvanceExpedition(expeditionOverview.ActiveChallenge.Challenge.Id, action);
        if (advanceResult.Status != TransactionReceipt.ResponseStatus.Confirmed)
            return;


        var challengeResult = await Instance.GetChallengeResult(expeditionOverview.ActiveChallenge.Challenge.Id);
        QuestResults.SetActive(true);
        ChallengeCompleted.text = "Results of the challenge: \n";

        foreach (var hero in challengeResult.Current.Outcome)
        {
            var res = hero.Success ? "successful" : "failed";
            ChallengeCompleted.text += $" Hero {hero.HeroId} took {hero.Damage}. A neded roll was {hero.NeededRoll}, actual roll was {hero.ActualRoll}, so challenge was {res} \n ";
        }

        expeditionDetails = await RefreshDetails();
        foreach (var hero in expeditionDetails.Party)
        {
            foreach (var partyHero in heroes)
            {
                if (hero.Id == partyHero.HeroID)
                {
                    partyHero.Health = hero.Health;
                }
            }
        }
        if (expeditionDetails.Party.Sum(x => x.Health) == 0)
            await OnFinish();

        foreach (var loot in challengeResult.Current.Loot)
        {
            //loot.State == 
        }

        InitializeExpedition(expeditionOverview);
    }

    private void RefreshStuff()
    {
        var shards = RefreshShardsAmount();
        ShardsAmount.text = shards.ToString();
    }

    void Update()
    {
        if (challenge == null)
            return;

        if (dateTime > DateTime.UtcNow)
        {
            timeDifference = dateTime - DateTime.UtcNow;
            Timer.text = timeDifference.ToString(@"hh\:mm\:ss");
        }
        else if (State == State.Traveling)
        {
            ChallengeOverview.text = "You've reached the challenge. What will you do?";
            OptionOne.text = "Resolve";
            OptionTwo.text = "Sneak";
        }

        State = challenge.States.FirstOrDefault(x => x.CreatedAt == challenge.States.Max(x => x.CreatedAt)).State;
        Debug.Log("State " + State);
    }
}
