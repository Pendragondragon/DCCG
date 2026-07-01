using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Attack : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    //Variables Needed
    [Header("Line Settings")]
    public GameObject linePrefab;
    private GameObject activeLine;
    private RectTransform lineRect;
    private CardDisplay cardDisplay;
    private TurnSystem turnSystem;
    private bool isAttackingAllowed = false;

    void Start()
    {
        cardDisplay = GetComponent<CardDisplay>();
        FindTurnSystem();
    }

    //Searching system to get te TurnSystem manager in the hierarchy 
    private void FindTurnSystem()
    {
        if (turnSystem == null)
        {
            GameObject turnSystemGO = GameObject.Find("TurnSystem");
            if (turnSystemGO != null) turnSystem = turnSystemGO.GetComponent<TurnSystem>();
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        FindTurnSystem();
        if (turnSystem == null) return;

        // Calculate current turn context based on whose turn is active
        int currentGlobalTurn = turnSystem.isPlayerTurn ? turnSystem.playerTurn : turnSystem.opponentTurn;
        
        // Cache the card's immediate UI parent container for validation checks
        Transform currentParent = this.transform.parent;
        if (currentParent == null) return;

        // Duplicate turn count calculation
        int currentTurnCount = turnSystem.isPlayerTurn ? turnSystem.playerTurn : turnSystem.opponentTurn;
        
        // Rule Check: Summoning Sickness/Slumber (meaning can't attack the turn it entered)
        //If the turn counter when the card enters matches the current turn, the attack doesn't happen.
        if (cardDisplay != null && cardDisplay.turnSpawned == currentTurnCount)
        {
            Debug.LogWarning($"Combat: {gameObject.name} has summoning sickness and cannot attack!");
            return; 
        }

        // Checks if it's player turn, player can only attack on its turn
        if (!turnSystem.isPlayerTurn) return;
        if (currentParent.name == "Hand" || currentParent.CompareTag("HandPlayer")) return;
        
        // Make sure that the monster is in the player's battlefield
        if (!IsValidBattlefieldParent(currentParent))
        {
            Debug.LogWarning($"[Attack Blocked: Drag rejected for '{gameObject.name}'");
            return;
        }

        // Allow attack
        isAttackingAllowed = true;

        // Draw target line to attack
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
        
        if (linePrefab != null && canvas != null)
        {
            //Spawns the target arrow overlayed in the scene
            activeLine = Instantiate(linePrefab, canvas.transform);
            lineRect = activeLine.GetComponent<RectTransform>();
            
            Transform backgroundTransform = canvas.transform.Find("Background");
            if (backgroundTransform != null)
                activeLine.transform.SetSiblingIndex(backgroundTransform.GetSiblingIndex() + 1);
            else
                activeLine.transform.SetAsLastSibling();
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        //Cancels attack if the attack wasn't verified or if the line failed 
        if (!isAttackingAllowed || activeLine == null || lineRect == null) return;
        
        Vector2 startPos = transform.position;
        Vector2 endPos = eventData.position;
        lineRect.position = startPos;
        
        //Calculations for direction and angle
        Vector2 direction = endPos - startPos;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        lineRect.rotation = Quaternion.Euler(0, 0, angle - 90);
        
        //Calculations for distance
        float distance = Vector2.Distance(startPos, endPos);
        lineRect.sizeDelta = new Vector2(lineRect.sizeDelta.x, distance);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (activeLine != null) Destroy(activeLine);
        if (!isAttackingAllowed) return;

        isAttackingAllowed = false;

        GameObject hitObject = eventData.pointerCurrentRaycast.gameObject;
        
        // Fallback de Raycast
        if (hitObject == null)
        {
            var pointerData = new PointerEventData(EventSystem.current) { position = eventData.position };
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);
            foreach (var result in results)
            {
                if (result.gameObject != this.gameObject && result.gameObject.name != "TargetingLine")
                {
                    hitObject = result.gameObject;
                    break;
                }
            }
        }

        if (hitObject == null) return;

        // Target verification
        Transform currentCheck = hitObject.transform;
        bool hitOpponentFace = false;

        while (currentCheck != null)
        {
            if (currentCheck.name == "OpponentHP" || currentCheck.CompareTag("Opponent"))
            {
                hitOpponentFace = true;
                break;
            }
            currentCheck = currentCheck.parent;
        }

        // reading that it targets opponents avatar
        if (hitOpponentFace)
        {
            AttackOpponentFace();
            return;
        }

        // reading that it targets opponents monster
        CardDisplay targetCard = hitObject.GetComponentInParent<CardDisplay>();
        if (targetCard != null && targetCard.transform.parent != null)
        {
            if (targetCard.transform.parent.name == "BattlefieldOpponent" || targetCard.transform.parent.CompareTag("BattlefieldOpponent"))
            {
                AttackEnemyMonster(targetCard);
            }
        }
    }

    // Attack directly opponents avatar
    private void AttackOpponentFace()
    {
        OpponentHP opponent = FindFirstObjectByType<OpponentHP>();
        if (opponent != null && cardDisplay != null)
        {
            Debug.Log($"[Combat:] Atack directly {cardDisplay.currentAttack} points of damage!");
            opponent.TakeDamage(cardDisplay.currentAttack);
        }
    }

    // Attack opponents monster 
    private void AttackEnemyMonster(CardDisplay enemy)
    {
        if (cardDisplay == null || enemy == null) return;
        Debug.Log($"[Combat:] Combat between monsters!");
        
        // apply damage taken on creature
        int pDmg = cardDisplay.currentAttack;
        int eDmg = enemy.currentAttack;

        enemy.TakeDamage(pDmg);
        cardDisplay.TakeDamage(eDmg);
    }

    // Checks thatthe card is in player's side of the board in the hierarchy
    private bool IsValidBattlefieldParent(Transform node)
    {
        while (node != null)
        {
            if (node.name == "BattlefieldPlayer" || node.CompareTag("BattlefieldPlayer"))
                return true;
            node = node.parent;
        }
        return false;
    }
}
