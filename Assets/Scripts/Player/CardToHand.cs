using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class CardToHand : MonoBehaviour
{
    public GameObject Hand;
    public GameObject HandCard;

    void Start()
    {
        // 1. DYNAMIC TAG LOOKUP: Find your hand container zone
        if (Hand == null)
        {
            Hand = GameObject.FindWithTag("HandPlayer");
            if (Hand == null) Hand = GameObject.Find("HandPlayer");
            if (Hand == null) Hand = GameObject.Find("Hand"); 
        }

        // 2. AUTO-ASSIGN SELF
        if (HandCard == null)
        {
            HandCard = this.gameObject;
        }

        if (Hand != null && HandCard != null)
        {
            // Set the parent layout group context cleanly
            HandCard.transform.SetParent(Hand.transform);
            
            // Clean up UI scaling transform issues
            HandCard.transform.localScale = Vector3.one;
            HandCard.transform.transform.eulerAngles = new Vector3(25, 0, 0);
            
            // Position tracking placement
            HandCard.transform.position = new Vector3(transform.position.x, transform.position.y, -48);

            // 3. REFRESH ONLY: Let the card display update its visuals using the data PlayerDeck gave it
            CardDisplay display = HandCard.GetComponent<CardDisplay>();
            if (display != null)
            {
                // We removed the static deck index overwrite block from here completely!
                display.RefreshCardUI();
            }
        }
        else
        {
            Debug.LogError($"[CardToHand] Missing Hand or HandCard assignment on {gameObject.name}");
        }
    }
}