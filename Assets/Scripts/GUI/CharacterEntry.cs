using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;

public class CharacterEntry : MonoBehaviour
{
    [SerializeField] private Sprite RaceIcon;
    [SerializeField] private TextMeshProUGUI Name;
    [SerializeField] private TextMeshProUGUI HealthString;
    [SerializeField] private float HealthFillAmount;
    [SerializeField] private Sprite RarityIcon;
    [SerializeField] private Sprite ClassIcon;
    [SerializeField] private GameObject InParty;
    [SerializeField] private GameObject Selected;

    public Chromia.Buffer Id { get; set; }

    private void Start()
    {
        
    }

    public void Initialize(Blockchain.Hero hero, Config configuration)
    {
        RaceIcon = configuration.RaceIcons.FirstOrDefault(x => x.species == hero.Species).icon;
        Name.text = hero.Name;
        Id = hero.Id;

        var fullBaseHealth = configuration.BaseHealths.FirstOrDefault(x => x.classType == hero.Class).health;
        var healthCoef = configuration.BaseHealthCoefs.FirstOrDefault(x => x.rarity == hero.Rarity).coef;
        var fullRarityHealth = fullBaseHealth * healthCoef;
        HealthFillAmount = (float)hero.Health / fullRarityHealth;
        HealthString.text = $"{hero.Health}/{fullRarityHealth}";
        RarityIcon = configuration.HeroRarityIcons.FirstOrDefault(x => x.rarity == hero.Rarity).heroRarity;
        ClassIcon = configuration.ClassIcons.FirstOrDefault(x => x.heroClass == hero.Class).icon;
    }

    public void Select(bool status) => Selected.SetActive(true);
    public void Deselect(bool status) => Selected.SetActive(false);

}
