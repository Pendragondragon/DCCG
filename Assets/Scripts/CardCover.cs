using UnityEngine;

public class CardCover : MonoBehaviour
{
    public GameObject cardCover;
    
    public bool isCoverActive = true; 

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
