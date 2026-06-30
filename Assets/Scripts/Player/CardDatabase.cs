using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardDatabase : MonoBehaviour
{
    public List<Card> deckSource;
    public static List<Card> cardList = new List<Card>();

    void Awake()
    {
        if (cardList == null) cardList = new List<Card>();
        cardList.Clear(); 
        
        if (deckSource != null)
        {
            cardList.AddRange(deckSource);
        }
    }
}