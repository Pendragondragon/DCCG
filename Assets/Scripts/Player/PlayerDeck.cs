using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDeck : MonoBehaviour
{
    // Assuming you have these or similar variables based on OpponentDeck structure
    public static int deckSize;
    public List<Card> deck = new List<Card>();
    public static List<Card> staticDeck = new List<Card>();

    [Header("Setup Configurations")]
    public GameObject CardToHand;
    public GameObject Hand;

    void Start()
    {
        deck.Clear();
        if (CardDatabase.cardList != null && CardDatabase.cardList.Count > 0)
        {
            foreach (Card dbCard in CardDatabase.cardList)
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
        // Keep your active graphic layout checking elements intact here if needed!
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

    // Cleaned up pipeline function: Called safely from TurnSystem!
    public void TriggerPlayerDraw(int amount)
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

        if (Hand == null) Hand = GameObject.Find("HandPlayer"); // Or whatever your hand canvas is named
        if (Hand == null) Hand = GameObject.Find("Hand");

        // Inside PlayerDeck.cs -> DrawSingleCardDirectly()
    if (Hand != null)
    {
        GameObject cardInstance = Instantiate(CardToHand, Hand.transform);
        cardInstance.transform.localScale = Vector3.one;
        cardInstance.transform.localRotation = Quaternion.identity;

        CardDisplay display = cardInstance.GetComponent<CardDisplay>();
        if (display != null)
        {
            display.displayCard = drawnCardData;
            display.RefreshCardUI(); 
        }

        CardCover cover = cardInstance.GetComponent<CardCover>();
        if (cover != null)
        {
            cover.isCoverActive = false; // Player sees their own cards!
            cover.UpdateCoverState();
        }
    }
    }
}