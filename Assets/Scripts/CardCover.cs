using UnityEngine;

public class CardCover : MonoBehaviour
{
    public GameObject cardCover;
    
    // Changing this to a regular instance variable so each card can be independent!
    public bool isCoverActive = true; 

    // Cache reference to the CardDisplay on this card
    private CardDisplay cardDisplay;

    void Start()
    {
        cardDisplay = GetComponent<CardDisplay>();
        UpdateCoverState();
    }

    void Update()
    {
        if (cardDisplay != null)
        {
            isCoverActive = cardDisplay.cardCover;
        }

        if (cardCover != null)
        {
            cardCover.SetActive(isCoverActive);
        }
    }

    public void UpdateCoverState()
    {
        if (cardCover != null)
        {
            cardCover.SetActive(isCoverActive);
        }
    }
}