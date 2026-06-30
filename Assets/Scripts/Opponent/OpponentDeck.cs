using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpponentDeck : MonoBehaviour
{
    public static int deckSize;
    
    public List<Card> deck = new List<Card>();
    public static List<Card> staticDeck = new List<Card>();

    [Header("Deck Size Graphic Elements")]
    public GameObject cardInDeck1; public GameObject cardInDeck2; public GameObject cardInDeck3;
    public GameObject cardInDeck4; public GameObject cardInDeck5; public GameObject cardInDeck6;
    public GameObject cardInDeck7; public GameObject cardInDeck8; public GameObject cardInDeck9;
    public GameObject cardInDeck10; public GameObject cardInDeck11; public GameObject cardInDeck12;
    public GameObject cardInDeck13; public GameObject cardInDeck14; public GameObject cardInDeck15;
    public GameObject cardInDeck16; public GameObject cardInDeck17; public GameObject cardInDeck18;
    public GameObject cardInDeck19; public GameObject cardInDeck20; public GameObject cardInDeck21;
    public GameObject cardInDeck22; public GameObject cardInDeck23; public GameObject cardInDeck24;

    [Header("Setup Configurations")]
    public GameObject CardToHand;
    public GameObject Hand;

    void Start()
    {
        deck.Clear();

        if (CardDatabaseOpponent.cardList != null && CardDatabaseOpponent.cardList.Count > 0)
        {
            foreach (Card dbCard in CardDatabaseOpponent.cardList)
            {
                if (dbCard != null && !deck.Contains(dbCard))
                {
                    deck.Add(dbCard);
                }
            }
        }

        deckSize = deck.Count; 
        Shuffle();
        StartCoroutine(StartGame());
    }

    void Update()
    {
        staticDeck = deck;
        deckSize = deck.Count; 

        if(deckSize < 24) if (cardInDeck1 != null) cardInDeck1.SetActive(false);
        if(deckSize < 23) if (cardInDeck2 != null) cardInDeck2.SetActive(false);
        if(deckSize < 22) if (cardInDeck3 != null) cardInDeck3.SetActive(false);
        if(deckSize < 21) if (cardInDeck4 != null) cardInDeck4.SetActive(false);
        if(deckSize < 20) if (cardInDeck5 != null) cardInDeck5.SetActive(false);
        if(deckSize < 19) if (cardInDeck6 != null) cardInDeck6.SetActive(false);
        if(deckSize < 18) if (cardInDeck7 != null) cardInDeck7.SetActive(false);
        if(deckSize < 17) if (cardInDeck8 != null) cardInDeck8.SetActive(false);
        if(deckSize < 16) if (cardInDeck9 != null) cardInDeck9.SetActive(false);
        if(deckSize < 15) if (cardInDeck10 != null) cardInDeck10.SetActive(false);
        if(deckSize < 14) if (cardInDeck11 != null) cardInDeck11.SetActive(false);
        if(deckSize < 13) if (cardInDeck12 != null) cardInDeck12.SetActive(false);
        if(deckSize < 12) if (cardInDeck13 != null) cardInDeck13.SetActive(false);
        if(deckSize < 11) if (cardInDeck14 != null) cardInDeck14.SetActive(false);
        if(deckSize < 10) if (cardInDeck15 != null) cardInDeck15.SetActive(false);
        if(deckSize < 9)  if (cardInDeck16 != null) cardInDeck16.SetActive(false);
        if(deckSize < 8)  if (cardInDeck17 != null) cardInDeck17.SetActive(false);
        if(deckSize < 7)  if (cardInDeck18 != null) cardInDeck18.SetActive(false);
        if(deckSize < 6)  if (cardInDeck19 != null) cardInDeck19.SetActive(false);
        if(deckSize < 5)  if (cardInDeck20 != null) cardInDeck20.SetActive(false);
        if(deckSize < 4)  if (cardInDeck21 != null) cardInDeck21.SetActive(false);
        if(deckSize < 3)  if (cardInDeck22 != null) cardInDeck22.SetActive(false);
        if(deckSize < 2)  if (cardInDeck23 != null) cardInDeck23.SetActive(false);
        if(deckSize < 1)  if (cardInDeck24 != null) cardInDeck24.SetActive(false);
    }

    IEnumerator StartGame()
    {
        yield return new WaitForSeconds(0.3f); 
        for (int i = 0; i < 5; i++)
        {
            DrawSingleCardDirectly();
            yield return new WaitForSeconds(0.2f); 
        }
    }

    public void Shuffle()
    {
        for (int i = 0; i < deck.Count; i++)
        {
            Card temp = deck[i];
            int randomIndex = Random.Range(i, deck.Count);
            deck[i] = deck[randomIndex];
            deck[randomIndex] = temp;
        }
    }

    public void TriggerOpponentDraw(int amount)
    {
        StartCoroutine(Draw(amount));
    }

    IEnumerator Draw(int x)
    {
        for (int i = 0; i < x; i++)
        {
            DrawSingleCardDirectly();
            yield return new WaitForSeconds(0.2f);
        }
    }

    public void DrawSingleCardDirectly()
    {
        if (deck.Count <= 0) return;

        Card drawnCardData = deck[0];
        deck.RemoveAt(0);

        if (Hand == null) 
        {
            Hand = GameObject.FindWithTag("OpponentHand");
            if (Hand == null) Hand = GameObject.Find("OpponentHand");
        }

        // Inside OpponentDeck.cs -> DrawSingleCardDirectly()
        if (Hand != null)
        {
            GameObject cardInstance = Instantiate(CardToHand, Hand.transform);
            cardInstance.transform.localScale = Vector3.one;
            cardInstance.transform.localRotation = Quaternion.Euler(25, 0, 0);

            CardDisplay display = cardInstance.GetComponent<CardDisplay>();
            if (display != null)
            {
                display.displayCard = drawnCardData;
                display.RefreshCardUI(); 
            }

            CardCover cover = cardInstance.GetComponent<CardCover>();
            if (cover != null)
            {
                cover.isCoverActive = true; // Hidden from the player!
                cover.UpdateCoverState();
            }
        }
    }

}