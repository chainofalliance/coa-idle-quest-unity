using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cysharp.Threading.Tasks;
using static Blockchain;

public class PartyEntry : MonoBehaviour
{
    [SerializeField] private List<CharacterEntry> heroes = new();
    [SerializeField] private Button Entry, Details;
    [SerializeField] private TextMeshProUGUI Danger, Timer;
    [SerializeField] private GameObject Notification, Active;
    [SerializeField] private GameObject PartyDetailsScreen;

    public bool DangerSet { get; set; }
    public List<Blockchain.Hero> party;
    public ExpeditionOverview Expedition = default;
    public List<Consumable> partyconsumables;
    public List<ConsumableEntry> consumables;
    public Rarity partyrarity;
    public DangerLevel danger;
    public string backpackname = "";
    private TimeSpan timeDifference;
    public event Action<PartyEntry> Selected;
    public event Action DetailsClicked;

    void Start()
    {
        Entry.onClick.AddListener(OnPartySelected);
        Details.onClick.AddListener(OnDetailsClicked);
    }

    private async void OnDetailsClicked()
    {
        var result = await Instance.GetActiveExpeditions();
        var exp = result.FirstOrDefault();

        if (exp != null)
        {
            DetailsClicked?.Invoke();
        }
    }

    public void Deselect()
    {
        Active.SetActive(false);
    }

    public void Destroy()
    {
        Destroy(gameObject);
    }

    private void OnPartySelected()
    {
        Active.SetActive(true);
        Selected?.Invoke(this);
    }


    public async void InitializeExpedition()
    {
        var result = await Instance.GetActiveExpeditions();
        Expedition = result.FirstOrDefault();

        if (Expedition != null && Expedition.ActiveChallenge.ArrivalAt > 0)
        {
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds(Expedition.ActiveChallenge.ArrivalAt).ToLocalTime();
            timeDifference = dateTime - DateTime.UtcNow;

            Notification.SetActive(true);
        }

        DangerSet = true;
        Danger.text = $"In {Expedition.DangerLevel} Expedition";
    }

    public void InitializeDanger()
    {
        Array values = Enum.GetValues(typeof(DangerLevel));
        System.Random random = new System.Random();
        var randomDanger = (DangerLevel)values.GetValue(random.Next(values.Length));
        danger = randomDanger;
        Danger.text = $"{danger} Expedition";
    }

    public void Initialize(List<Blockchain.Hero> heroesParty,
                           List<Consumable> consumable,
                           Config configuration,
                           string backpackName,
                           Rarity rarity,
                           ExpeditionOverview exp = default)
    {
        if (exp != default)
        {
            Notification.SetActive(true);
            Danger.text = $"In {exp.DangerLevel} Expedition";
        }
        else
        {
            InitializeDanger();
        }

        Expedition = exp;
        DangerSet = Expedition != default;
        backpackname = backpackName;
        party = heroesParty;
        partyconsumables = consumable;
        partyrarity = rarity;

        for (int i = 0; i < heroes.Count; i++)
        {
            heroes[i].Initialize(heroesParty[i], configuration, false, true, true);
        }
    }

  
    void Update()
    {
        Details.interactable = Active.activeSelf && Expedition != default;

        if (timeDifference.Milliseconds > 0)
        {
            Timer.text = timeDifference.ToString("H:mm:ss");
        }
    }
}