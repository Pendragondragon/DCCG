using UnityEngine;
using UnityEngine.UI;

public class RagnarkTrigger : MonoBehaviour
{
    public ScriptableObject blackDragonData;
    public GameObject cardPrefab;
    public GameObject linePrefab;

    private void OnDestroy()
    {
        // Safety net checking: ONLY fire if the app is actively playing (prevents editor leak bugs)
        if (!gameObject.scene.isLoaded) return;

        // Determine which side this card was actually on by looking at its parent
        bool isPlayerSide = true;
        if (transform.parent != null && (transform.parent.name.Contains("Opponent") || transform.parent.CompareTag("BattlefieldOpponent")))
        {
            isPlayerSide = false;
        }

        string targetLaneName = isPlayerSide ? "BattlefieldPlayer" : "BattlefieldOpponent";
        GameObject targetBattlefield = GameObject.Find(targetLaneName);

        if (targetBattlefield != null && blackDragonData != null && cardPrefab != null)
        {
            if (targetBattlefield.transform.childCount >= 7)
            {
                Debug.LogWarning($"[RAGNAROK DEATH] {targetLaneName} full! Black Dragon couldn't find space to land.");
                return;
            }

            GameObject dragonInstance = Instantiate(cardPrefab, targetBattlefield.transform);
            dragonInstance.name = "Black_Dragon_Token";

            Attack oldAttack = dragonInstance.GetComponent<Attack>();
            if (oldAttack != null) Destroy(oldAttack);

            CardDisplay display = dragonInstance.GetComponent<CardDisplay>();
            if (display != null)
            {
                display.displayCard = (Card)blackDragonData;
                
                // ✨ FIX: Look up exact Database ID index to prevent forced reversion to default Djinn prefab properties
                if (CardDatabase.cardList != null)
                {
                    for (int x = 0; x < CardDatabase.cardList.Count; x++)
                    {
                        if (CardDatabase.cardList[x] != null && CardDatabase.cardList[x].name == blackDragonData.name)
                        {
                            display.displayId = x;
                            break;
                        }
                    }
                }

                display.currentAttack = display.displayCard.attack;
                display.currentVigor = display.displayCard.vigor;
                display.cardCover = false;
                display.hasSlumber = true; 
                display.RefreshCardUI();
            }

            if (isPlayerSide)
            {
                CardInteraction interaction = dragonInstance.GetComponent<CardInteraction>();
                if (interaction == null) interaction = dragonInstance.AddComponent<CardInteraction>();
                
                if (interaction != null)
                {
                    interaction.linePrefab = linePrefab;
                    interaction.cardPrefab = cardPrefab;
                    interaction.ForceInit();
                    interaction.parentToReturnTo = targetBattlefield.transform;
                    interaction.hasAttackedThisTurn = false;

                    TurnSystem turnSystem = FindFirstObjectByType<TurnSystem>();
                    if (turnSystem != null)
                    {
                        interaction.SetSummonTurn(turnSystem.playerTurn);
                    }
                }
            }
            else
            {
                // Set up AI scripts for the opponent's dragon token
                CardInteraction playerInteraction = dragonInstance.GetComponent<CardInteraction>();
                if (playerInteraction != null) Destroy(playerInteraction);

                if (dragonInstance.GetComponent<CardInteractionOpponent>() == null)
                {
                    dragonInstance.AddComponent<CardInteractionOpponent>();
                }
            }

            RectTransform rect = dragonInstance.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.localScale = Vector3.one;
                rect.localPosition = Vector3.zero;
                rect.anchoredPosition = Vector2.zero;
                rect.localRotation = Quaternion.Euler(isPlayerSide ? 0 : 25, 0, 0);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(targetBattlefield.GetComponent<RectTransform>());
            Debug.Log($"[RAGNARK DEATH] Ragnarok has fallen! A fierce Black Dragon rises on the {targetLaneName}!");
        }
    }
}