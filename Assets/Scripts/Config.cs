using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class Config : ScriptableObject
{
    [SerializeField]
    public List<RarityColor> RarityColors;
    [SerializeField]
    public List<HeroRarityIcon> HeroRarityIcons;
    [SerializeField]
    public List<RaceIcon> RaceIcons;
    [SerializeField]
    public List<ClassIcon> ClassIcons;
    [SerializeField]
    public List<BaseHealthCoef> BaseHealthCoefs;
    [SerializeField]
    public List<BaseHealth> BaseHealths;

    [SerializeField]
    public List<TerrainBackground> TerrainBackgrounds;

    [SerializeField]
    public List<HeroImage> HeroImages;

}

[System.Serializable]
public class HeroImage
{
    public Blockchain.Rarity rarity;
    public Blockchain.Species species;
    public Blockchain.Class heroClass;
    public Sprite image;
}

[System.Serializable]
public class TerrainBackground
{
    public Blockchain.Terrain terrain;
    public Sprite background;
}

[System.Serializable]
public class RarityColor
{
    public Blockchain.Rarity rarity;
    public Color color;
}

[System.Serializable]
public class HeroRarityIcon
{
    public Blockchain.Rarity rarity;
    public Sprite heroRarity;
}

[System.Serializable]
public class RaceIcon
{
    public Blockchain.Species species;
    public Sprite icon;
}

[System.Serializable]
public class ClassIcon
{
    public Blockchain.Class heroClass;
    public Sprite icon;
}


[System.Serializable]
public class BaseHealthCoef
{
    public Blockchain.Rarity rarity;
    public float coef;
}

[System.Serializable]
public class BaseHealth
{
    public Blockchain.Class classType;
    public int health;
}