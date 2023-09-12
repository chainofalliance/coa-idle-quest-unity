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

    public bool DangerSet => Danger.text != "";
    public List<Blockchain.Hero> party;
    public List<Blockchain.Consumable> partyconsumables;
    public List<ConsumableEntry> consumables;
    public Blockchain.Rarity partyrarity;
    public Blockchain.DangerLevel danger;
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
        var result = await Blockchain.Instance.GetActiveExpeditions();
        var exp = result.FirstOrDefault();

       if(exp != null)
        {
            DetailsClicked?.Invoke();
        }
   
    }

    public void Deselect()
    {
        Active.SetActive(false);

    }

    private void OnPartySelected()
    {
        Active.SetActive(true);
        Selected?.Invoke(this);
    }


    public async void InitializeExpedition()
    {
        var result = await Blockchain.Instance.GetActiveExpeditions();
        var exp = result.FirstOrDefault();

        if (exp != null && exp.ActiveChallenge.ArrivalAt > 0)
        {

            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds(exp.ActiveChallenge.ArrivalAt).ToLocalTime();
            timeDifference = dateTime - DateTime.UtcNow;

            Notification.SetActive(true);
        }
    }

    public  void InitializeDanger()
    {
        Array values = Enum.GetValues(typeof(Blockchain.DangerLevel));
        System.Random random = new System.Random();
        var randomDanger = (Blockchain.DangerLevel)values.GetValue(random.Next(values.Length));
        danger = randomDanger;
        Danger.text = $"{danger} Expedition";
    }

    public void Initialize(List<Blockchain.Hero> heroesParty, List<ConsumableEntry> consumable, Config configuration, string backpackName, Blockchain.Rarity rarity)
    {
        backpackname = backpackName;
        party = heroesParty;
        consumables = consumable;
        partyconsumables.AddRange(consumables.Select(c => c.consumableType));
        partyrarity = rarity;
        danger = Blockchain.DangerLevel.Harmless;
 
        for(int i = 0; i < heroes.Count; i++)
        {
            heroes[i].Initialize(heroesParty[i], configuration, false, true, true);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (timeDifference.Milliseconds > 0)
        {
            Timer.text = timeDifference.ToString("H:mm:ss");
        }

    }
}
