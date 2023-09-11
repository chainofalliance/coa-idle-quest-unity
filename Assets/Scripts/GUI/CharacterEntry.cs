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

    public Chromia.Buffer HeroID { get; set; }
    public string EntryName{ get; set; }
    public long Price { get; set; }
    public Sprite HeroImage { get; set; }
    public bool IsSelect { get; set; }
    public event Action<CharacterEntry> Selected;

    void Start()
    {
        Entry.onClick.AddListener(OnHeroSelected);
    }

    private void OnHeroSelected()
    {
        Active.SetActive(true);
        IsSelect = true;
        Selected?.Invoke(this);
    }

    public void Deselect()
    {
        IsSelect = false;
        Active.SetActive(false);
    }

    public void Initialize(Blockchain.Hero hero, Config configuration, int price = default)
    {
        Price = price;
        RaceIcon.sprite = configuration.RaceIcons.FirstOrDefault(x => x.species == hero.Species).icon;
        Name.text = hero.Name;
        EntryName = $"{hero.Rarity} {hero.Species} {hero.Class}";
        HeroID = hero.Id;
        var fullBaseHealth = configuration.BaseHealths.FirstOrDefault(x => x.classType == hero.Class).health;
        var healthCoef = configuration.BaseHealthCoefs.FirstOrDefault(x => x.rarity == hero.Rarity).coef;
        var fullRarityHealth = fullBaseHealth * healthCoef;
        HealthFillAmount.fillAmount = (float)hero.Health / fullRarityHealth;
        HealthString.text = $"{hero.Health}/{fullRarityHealth}";
        RarityIcon.sprite = configuration.HeroRarityIcons.FirstOrDefault(x => x.rarity == hero.Rarity).heroRarity;
        ClassIcon.sprite = configuration.ClassIcons.FirstOrDefault(x => x.heroClass == hero.Class).icon;

        HeroImage = configuration.HeroImages.FirstOrDefault(x => x.rarity == hero.Rarity && x.species == hero.Species && x.classType == hero.Class).icon;
    }

    public void Destroy()
    {
        Destroy(gameObject);
    }



    public void Select(bool status) => Active.SetActive(true);
    public void Deselect(bool status) => Active.SetActive(false);

}
