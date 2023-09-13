using System.Collections;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using static Blockchain;


public class CharacterEntry : MonoBehaviour, IEntry
{
    [SerializeField] private Image RaceIcon;
    [SerializeField] private TextMeshProUGUI Name;
    [SerializeField] private TextMeshProUGUI HealthString;
    [SerializeField] private Image HealthFillAmount;
    [SerializeField] private Image RarityIcon;
    [SerializeField] private Image ClassIcon;
    [SerializeField] private GameObject InParty;
    [SerializeField] private GameObject Active;
    [SerializeField] private Button Entry;
    [SerializeField] private Button DeleteEntry;

    public Chromia.Buffer HeroID { get; set; }
    public string EntryName{ get; set; }
    public long Price { get; set; }
    public Sprite HeroImage { get; set; }
    public bool IsSelect { get; set; }
    public bool IsAvailable { get; set; }
    public int Health { get; set; }
    public Blockchain.Hero Hero;
    private bool isParty;
    private bool isManagable;
    private Config config;
    public event Action<CharacterEntry> Selected;
    public event Action<CharacterEntry> Deleted;
    void Start()
    {
        Entry.onClick.AddListener(OnHeroSelected);
        DeleteEntry.onClick.AddListener(OnHeroDeleted);
    }

    private void OnHeroDeleted()
    {
        if(isManagable)
        {
            Deleted?.Invoke(this);
        }
    }

        private void OnHeroSelected()
    {
        if (IsAvailable == false || isParty)
            return;

        Active.SetActive(true);
        IsSelect = true;
        Selected?.Invoke(this);
    }

    public void Deselect()
    {
        IsSelect = false;
        Active.SetActive(false);
    }

    public void Initialize(Blockchain.Hero hero, Config configuration, bool managable, bool isPartyEntry, bool isAvailable, int price = default)
    {
        Hero = hero;
        Health = Hero.Health;
        isParty = isPartyEntry;
        Price = price;
        RaceIcon.sprite = configuration.RaceIcons.FirstOrDefault(x => x.species == hero.Species).icon;
        Name.text = hero.Name;
        EntryName = $"{hero.Rarity} {hero.Species} {hero.Class}";
        HeroID = hero.Id;
        config = configuration;
        RarityIcon.sprite = configuration.HeroRarityIcons.FirstOrDefault(x => x.rarity == hero.Rarity).heroRarity;
        ClassIcon.sprite = configuration.ClassIcons.FirstOrDefault(x => x.heroClass == hero.Class).icon;
        InParty.SetActive(!isAvailable);
        IsAvailable = isAvailable;
        isManagable = managable;
        DeleteEntry.gameObject.SetActive(managable);
        HeroImage = configuration.HeroImages.FirstOrDefault(x => x.rarity == hero.Rarity && x.species == hero.Species && x.classType == hero.Class).icon;
    }

    public void Destroy()
    {
        Destroy(gameObject);
    }

    private void Update()
    {
        var fullBaseHealth = config.BaseHealths.FirstOrDefault(x => x.classType == Hero.Class).health;
        var healthCoef = config.BaseHealthCoefs.FirstOrDefault(x => x.rarity == Hero.Rarity).coef;
        var fullRarityHealth = fullBaseHealth * healthCoef;
        HealthFillAmount.fillAmount = (float)Health / fullRarityHealth;
        HealthString.text = $"{Hero.Health}/{fullRarityHealth}";
    }

    public void Select(bool status) => Active.SetActive(true);
    public void Deselect(bool status) => Active.SetActive(false);

}
