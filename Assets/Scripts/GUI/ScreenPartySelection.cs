using System;
using System.Collections;
using System.Collections.Generic;
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
            SelectedPartyEntry.partyrarity);
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
    }

    private void OnDetailsClicked()
    {
        PartyDetailsScreen.SetActive(true);
        List<ConsumableEntry> consumables = new();
        var partyDetailsScreen = PartyDetailsScreen.GetComponent<PartyDetailsScreen>();
        partyDetailsScreen.ReturnBack += OnReturnFromDetails;
        partyDetailsScreen.InitializeParty(SelectedPartyEntry.party, consumables, Config, Rarity.Common);
    }

    private void OnReturnFromDetails()
    {
        SelectedPartyEntry.Deselect();

    }

    private void OnPartyUpdated(List<Blockchain.Hero> heroes, List<ConsumableEntry> consumables, Rarity rarities)
    {
        CreatePartyScreen.SetActive(false);

        var partyEntry = Instantiate(PartyPrefab, PartyRoot.transform).GetComponent<PartyEntry>();

        partyEntry.Initialize(heroes, consumables, Config, Rarity.Common);
        partyEntry.Selected += OnSelected;
    }

    private void OnSelected(PartyEntry entry)
    {
        SelectedPartyEntry = entry;
        SelectedPartyEntry.DetailsClicked += OnDetailsClicked;

        // Update is called once per frame

    }

    void Update()
    {

    }
}
