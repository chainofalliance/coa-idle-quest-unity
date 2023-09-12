using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Blockchain;

public class ScreenPartySelection : MonoBehaviour
{
    [SerializeField] private GameObject CreatePartyScreen;
    [SerializeField] private GameObject PartyDetailsScreen;
    [SerializeField] private GameObject PartyDetails;
    [SerializeField] private Button Entry, Danger, StartExpedition;
    [SerializeField] private GameObject PartyRoot, PartyPrefab;
    [SerializeField] private Config Config;
    [SerializeField] private GameObject Loading;

    private List<PartyEntry> CreatedParties = new List<PartyEntry>();
    private PartyEntry SelectedPartyEntry;

    async void Start()
    {
        StartExpedition.onClick.AddListener(OnStarted);
        Entry.onClick.AddListener(OnEntrySelected);
        Danger.onClick.AddListener(OnDangerSelected);

        await UniTask.Delay(5000);
        await LoadParties();

        Loading.SetActive(false);
    }


    private async UniTask<List<ExpeditionOverview>> LoadParties()
    {
        var response = await Instance.GetActiveExpeditions();

        foreach (var exp in response)
        {
            var details = await Instance.GetExpeditionDetails(exp.Id);
            var backpack = details.Backpack;

            var cons = new List<Consumable>();
            foreach (var c in details.Consumables)
            {
                for (long i = c.Item2; i > 0; i--)
                {
                    cons.Add(c.Item1);
                }
            }
            var heroes = new List<Blockchain.Hero>();
            foreach (var pm in exp.Party)
                heroes.Add(pm);

            OnPartyUpdated(heroes, cons, backpack, Rarity.Common, exp);
        }

        return response;
    }

    private async void OnStarted()
    {
        if (SelectedPartyEntry == null)
        {
            return;
        }

        var result = await Instance.StartExpedition(
            SelectedPartyEntry.party,
            SelectedPartyEntry.partyconsumables,
            SelectedPartyEntry.danger,
            SelectedPartyEntry.backpackname);

        if (result.Status == Chromia.TransactionReceipt.ResponseStatus.Confirmed)
            SelectedPartyEntry.InitializeExpedition();
    }

    private void OnDangerSelected()
    {
        if (SelectedPartyEntry == null)
        {
            return;
        }

        SelectedPartyEntry.InitializeDanger();
    }

    private void OnEntrySelected()
    {
        if (Loading.activeSelf)
            return;

        CreatePartyScreen.SetActive(true);
        var createPartyScreen = CreatePartyScreen.GetComponent<CreatePartyScreen>();

        createPartyScreen.PartyUpdated += OnPartyUpdated;
        createPartyScreen.ReturnClicked += OnReturn;
    }

    private async void OnDetailsClicked()
    {
        var result = await Blockchain.Instance.GetActiveExpeditions();
        var exp = result.FirstOrDefault();

        if (SelectedPartyEntry == null || exp == null)
        {
            return;
        }

        PartyDetailsScreen.SetActive(true);

        var partyDetailsScreen = PartyDetailsScreen.GetComponent<PartyDetailsScreen>();
        partyDetailsScreen.ReturnBack += OnReturnFromDetails;
        partyDetailsScreen.InitializeParty(SelectedPartyEntry.party,
                                           SelectedPartyEntry.consumables,
                                           Config,
                                           Rarity.Common);
        partyDetailsScreen.InitializeExpedition(exp);

    }

    private void OnReturnFromDetails()
    {
        SelectedPartyEntry.Deselect();
        PartyDetailsScreen.SetActive(false);
    }
    private void OnReturn()
    {
        var createPartyScreen = CreatePartyScreen.GetComponent<CreatePartyScreen>();
        CreatePartyScreen.SetActive(false);
    }

    private void OnPartyUpdated(List<Blockchain.Hero> heroes,
                                List<Consumable> consumables,
                                string backpackName,
                                Rarity backpack,
                                ExpeditionOverview exp = default)
    {
        CreatePartyScreen.SetActive(false);

        var partyEntry = Instantiate(PartyPrefab, PartyRoot.transform).GetComponent<PartyEntry>();

        partyEntry.Initialize(heroes, consumables, Config, backpackName, backpack, exp);
        partyEntry.Selected += OnSelected;

        if (CreatedParties.Contains(partyEntry) == false)
        {
            CreatedParties.Add(partyEntry);
        }
    }

    private void OnSelected(PartyEntry entry)
    {
        if (entry == SelectedPartyEntry)
        {
            SelectedPartyEntry = null;
            entry.Deselect();
            return;
        }

        foreach (var party in CreatedParties)
        {
            if (party != entry)
                party.Deselect();
        }

        SelectedPartyEntry = entry;

        SelectedPartyEntry.DetailsClicked += OnDetailsClicked;
    }

    void Update()
    {
        StartExpedition.interactable = SelectedPartyEntry != null
            && SelectedPartyEntry.Expedition == default;

        Danger.interactable = SelectedPartyEntry != null
            && SelectedPartyEntry.Expedition == default
            && SelectedPartyEntry.DangerSet == false;

    }
}
