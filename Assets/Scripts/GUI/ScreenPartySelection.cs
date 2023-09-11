using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Chromia;
using Cysharp.Threading.Tasks;
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

    private List<PartyEntry> CreatedParties = new List<PartyEntry>();
    private PartyEntry SelectedPartyEntry;
    // Start is called before the first frame update
    void Start()
    {
        StartExpedition.onClick.AddListener(OnStarted);
        Entry.onClick.AddListener(OnEntrySelected);
        Danger.onClick.AddListener(OnDangerSelected);
    }

    private async void OnStarted()
    {
        if (SelectedPartyEntry == null)
        {
            return;
        }

        var result = await Blockchain.Instance.StartExpedition(
            SelectedPartyEntry.party,
            SelectedPartyEntry.partyconsumables,
            SelectedPartyEntry.danger,
            "");
        Debug.Log(result.Status + "EXP");

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
        CreatePartyScreen.SetActive(true);
        var createPartyScreen = CreatePartyScreen.GetComponent<CreatePartyScreen>();
        createPartyScreen.PartyUpdated += OnPartyUpdated;
        createPartyScreen.ReturnClicked += OnReturn;
        createPartyScreen.InjectDependency(CreatedParties);
    }

    private async void OnDetailsClicked()
    {
        if(SelectedPartyEntry.DangerSet == false)
        {
            return;
        }

        var result = await Blockchain.Instance.GetActiveExpeditions();
        var exp = result.FirstOrDefault();
        if (SelectedPartyEntry == null || exp == null)
        {
            return;
        }

        PartyDetailsScreen.SetActive(true);
        List<ConsumableEntry> consumables = new();
        var partyDetailsScreen = PartyDetailsScreen.GetComponent<PartyDetailsScreen>();
        partyDetailsScreen.ReturnBack += OnReturnFromDetails;
        partyDetailsScreen.InitializeParty(SelectedPartyEntry.party, consumables, Config, Rarity.Common);
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
        createPartyScreen.InjectDependency(CreatedParties);
        CreatePartyScreen.SetActive(false);
    }

    private void OnPartyUpdated(List<Blockchain.Hero> heroes, List<ConsumableEntry> consumables, Rarity rarities)
    {
        CreatePartyScreen.SetActive(false);

        var partyEntry = Instantiate(PartyPrefab, PartyRoot.transform).GetComponent<PartyEntry>();

        partyEntry.Initialize(heroes, consumables, Config, Rarity.Common);
        partyEntry.Selected += OnSelected;

        if (CreatedParties.Contains(partyEntry) == false)
        {
            CreatedParties.Add(partyEntry);
            Debug.Log(CreatedParties);
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

        SelectedPartyEntry = entry;

        SelectedPartyEntry.DetailsClicked += OnDetailsClicked;
    }

    void Update()
    {
        StartExpedition.interactable = SelectedPartyEntry != null && SelectedPartyEntry.DangerSet;
    }
}
