using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum RarityType
{
    Basic,
    Common,
    Rare, 
    Epic,
    Legendary
}

[System.Serializable]
[CreateAssetMenu(fileName = "New Card", menuName = "Card")]
public class Card : ScriptableObject
{
    public int id;
    public new string name;
    public string effect;
    public string faction;
    public RarityType rarityType;
    public Sprite rarity;
    public int essence;
    public int attack;
    public int vigor;
    public Sprite art;
    public Sprite border;
    
    public Card() { }

    public Card (int Id, string Name, string Faction, 
        RarityType RarityType, Sprite Rarity, 
        int Essence, int Attack, int Vigor, 
        string Effect, Sprite Art, Sprite Border)
    {
        id = Id;
        name = Name;
        faction = Faction;
        rarityType = RarityType;
        rarity = Rarity;
        essence = Essence;
        attack = Attack;
        vigor = Vigor;
        effect = Effect;
        art = Art;
        border = Border;
    }
}