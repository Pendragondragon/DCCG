using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;

public class Collection : MonoBehaviour
{
    public Transform contentParent; 
    public GameObject cardUIPrefab;
    public List<Card> allCards;

    void Start()
    {
        var sortedCards = allCards.OrderBy(c => c.name).ToList();

        foreach (Card card in sortedCards)
        {
            GameObject cardUI = Instantiate(cardUIPrefab, contentParent);
            
            cardUI.name = card.name;

            UpdateText(cardUI, "Name", card.name);
            UpdateText(cardUI, "Effect", card.effect);
            UpdateText(cardUI, "Essence/Text", card.essence.ToString());
            UpdateText(cardUI, "Attack/Text", card.attack.ToString());
            UpdateText(cardUI, "Vigor/Text", card.vigor.ToString());
            UpdateImage(cardUI, "Rarity", card.rarity);
            UpdateImage(cardUI, "CardArt", card.art);
            UpdateImage(cardUI, "CardBorder", card.border);
        }
    }

    void UpdateText(GameObject parent, string path, string content)
    {
        Transform child = parent.transform.Find(path);
        if (child != null) 
        {
            var tmp = child.GetComponent<TMP_Text>();
            if (tmp != null) tmp.text = content;
        }
    }

    void UpdateImage(GameObject parent, string name, Sprite sprite)
    {
        Transform child = parent.transform.Find(name);
        if (child != null) 
        {
            var img = child.GetComponent<Image>();
            if (img != null) 
            {
                img.sprite = sprite;
                img.color = Color.white;
                img.enabled = true;      
                
                // Debugging scale
                child.localScale = Vector3.one; 
                Debug.Log($"Successfully set {name} to {sprite.name}");
            }
        }
    }

    public void LoadMenuFromCollectionScene()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
