using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class DeckPanelCard : MonoBehaviour
{
    public GameObject cardCover;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        cardCover.SetActive(true);
    }
}
