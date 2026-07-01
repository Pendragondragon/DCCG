using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AvatarPower : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    // Variables Neded
    [Header("Power Settings")]
    public string powerName = "Avatar Power";
    public int essenceCost = 2;
    public int damageAmount = 2;

    [HideInInspector] public bool hasBeenUsedThisTurn = false; 

    [Header("Line Settings")]
    public GameObject linePrefab; 
    private GameObject activeLine;
    private RectTransform lineRect;

    private TurnSystem turnSystem;
    private bool isPowerAllowed = false;

    void Start()
    {
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
        isPowerAllowed = false;
        FindTurnSystem();

        // Turn Rule: Only Playable on Player's turn
        if (turnSystem != null && !turnSystem.isPlayerTurn) return;

        // Game Rule: Can Only be used once per turn
        if (hasBeenUsedThisTurn)
        {
            Debug.LogWarning($"{powerName}: Already used this turn!");
            return;
        }

        // Game Rule: It needs enough essence points
        if (turnSystem != null && turnSystem.playerEssenceCurrent < essenceCost)
        {
            Debug.LogWarning($"{powerName}: Not enough Essence! Costs {essenceCost}.");
            return;
        }

        // Making sure it doesnt use opponent's ability
        Transform currentParent = transform;
        bool belongsToOpponent = false;

        while (currentParent != null)
        {
            string parentNameLower = currentParent.name.ToLower();
            
            // Check its related to opponent
            if (parentNameLower == "opponent" || parentNameLower.Contains("opponent"))
            {
                belongsToOpponent = true;
                break;
            }
            // Check its related to player
            if (parentNameLower == "player" || parentNameLower.Contains("player"))
            {
                belongsToOpponent = false;
                break;
            }

            currentParent = currentParent.parent;
        }

        // Reject event if it belongs to the opponent the ability
        if (belongsToOpponent)
        {
            Debug.LogWarning($"Avatar Pwer Validation: Action Blocked! {powerName} belongs to the opponent. You cannot trigger it on your turn!");
            return;
        }

        isPowerAllowed = true;

        //Draws the targetting line
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindFirstObjectByType<Canvas>();

        if (linePrefab != null && canvas != null)
        {
            activeLine = Instantiate(linePrefab, canvas.transform);
            lineRect = activeLine.GetComponent<RectTransform>();
            activeLine.transform.SetAsLastSibling(); 
        }
    }

    public void OnDrag(PointerEventData eventData)
    {   
        //Cancels power if the power wasn't verified or if the line failed 
        if (!isPowerAllowed || activeLine == null || lineRect == null) return;

        Vector2 startPos = eventData.pressPosition; 
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
        if (!isPowerAllowed) return;

        isPowerAllowed = false;

        GameObject hitObject = eventData.pointerCurrentRaycast.gameObject;
        
        // Fallback
        if (hitObject == null)
        {
            var pointerData = new PointerEventData(EventSystem.current) { position = eventData.position };
            var results = new System.Collections.Generic.List<RaycastResult>();
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

        // Target matches opponents avatar 
        if (hitOpponentFace)
        {
            ExecutePowerDamage();
            OpponentHP opponent = FindFirstObjectByType<OpponentHP>();
            if (opponent != null) opponent.TakeDamage(damageAmount);
            Debug.Log($"{powerName}: Hit face for {damageAmount} damage!");
            return;
        }

        // Target matches opponents monster
        CardDisplay targetCard = hitObject.GetComponentInParent<CardDisplay>();
        if (targetCard != null && targetCard.transform.parent != null)
        {
            if (targetCard.transform.parent.name == "BattlefieldOpponent" || targetCard.transform.parent.CompareTag("BattlefieldOpponent"))
            {
                // Shadowveil Check
                if (targetCard.hasShadowveil)
                {
                    Debug.LogWarning($"Avatar Power Cancelled: Cannot target {targetCard.displayCard.name} because it is shrouded in Shadowveil!");
                    return; 
                }

                //Deduct resource cost and mark used flags
                ExecutePowerDamage();
                // process health reductions
                targetCard.TakeDamage(damageAmount);
                Debug.Log($"[{powerName}] Dealt {damageAmount} damage to {targetCard.gameObject.name}!");
            }
        }
    }

    // Proccesses essence consumption
    private void ExecutePowerDamage()
    {
        if (turnSystem != null)
        {
            turnSystem.SpendEssence(essenceCost);
            hasBeenUsedThisTurn = true; 
        }
    }
    
    // Same logic for opponent
    public void ExecutePowerOpponent(GameObject targetObject)
    {
        FindTurnSystem();
        if (turnSystem == null || turnSystem.isPlayerTurn || hasBeenUsedThisTurn) return;
        if (turnSystem.opponentEssenceCurrent < essenceCost) return;

        // Verify target conditions (Shadowveil safety check)
        CardDisplay targetCard = targetObject.GetComponentInParent<CardDisplay>();
        if (targetCard != null)
        {
            if (targetCard.hasShadowveil)
            {
                Debug.LogWarning($"Opponent Avatar Power Cancelled: Cannot target {targetCard.displayCard.name} due to Shadowveil.");
                return;
            }
            
            // Deduct cost and mark as used
            turnSystem.SpendEssence(essenceCost);
            hasBeenUsedThisTurn = true;
            
            targetCard.TakeDamage(damageAmount);
            Debug.Log($"Opponent Avatar Power: Hit creature {targetCard.gameObject.name} for {damageAmount} damage!");
        }
        else if (targetObject.GetComponent<PlayerHP>() != null)
        {
            turnSystem.SpendEssence(essenceCost);
            hasBeenUsedThisTurn = true;

            PlayerHP playerFace = FindFirstObjectByType<PlayerHP>();
            if (playerFace != null) playerFace.TakeDamage(damageAmount);
            Debug.Log($"Opponent Avatar Power: Hit player face directly for {damageAmount} damage!");
        }
    }
}
