using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class CardToOpponent : MonoBehaviour
{
    public GameObject OpponentHand;
    public GameObject HandCard;

    void Start()
    {
        // 1. AUTO-ASSIGN HANDCARD
        if (HandCard == null)
        {
            HandCard = this.gameObject;
        }

        // 2. AUTO-FIND HAND
        if (OpponentHand == null)
        {
            OpponentHand = GameObject.Find("OpponentHand"); 
        }

        // 3. EXECUTE LOGIC IF BOTH ARE FOUND
        if (OpponentHand != null && HandCard != null)
        {
            // Set the parent inside the opponent's UI layout
            HandCard.transform.SetParent(OpponentHand.transform);
            
            // Clean up scale and rotations for UI rendering
            HandCard.transform.localScale = Vector3.one;
            HandCard.transform.eulerAngles = new Vector3(25, 0, 0);
            
            // Offset position slightly on the Z-axis so it doesn't clip behind background textures
            HandCard.transform.position = new Vector3(transform.position.x, transform.position.y, -48);

            // Fetch the display script to load structural card graphics
            CardDisplay display = HandCard.GetComponent<CardDisplay>();
            if (display != null)
            {
                // ✨ FIX: Remove the manual index math overwrite! 
                // Let the CardDisplay keep the data assigned to it by OpponentDeck.cs
                HandCard.tag = "Untagged";
                display.RefreshCardUI();
            }
        }
        else
        {
            if (OpponentHand == null)
            {
                Debug.LogError($"[CardToOpponent] CRITICAL: Could not find 'OpponentHand' in the current scene Hierarchy!", gameObject);
            }
        }
    }
}