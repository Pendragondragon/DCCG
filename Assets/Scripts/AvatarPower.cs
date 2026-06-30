using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AvatarPower : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
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

        // 1. Enforce turn rules
        if (turnSystem != null && !turnSystem.isPlayerTurn) return;

        // 2. Enforce ONCE PER TURN restriction
        if (hasBeenUsedThisTurn)
        {
            Debug.LogWarning($"[{powerName}] Already used this turn!");
            return;
        }

        // 3. Enforce resource availability
        if (turnSystem != null && turnSystem.playerEssenceCurrent < essenceCost)
        {
            Debug.LogWarning($"[{powerName}] Not enough Essence! Costs {essenceCost}.");
            return;
        }

        // 🛠️ HIERARCHY SECURITY GUARD
        Transform currentParent = transform;
        bool belongsToOpponent = false;

        while (currentParent != null)
        {
            string parentNameLower = currentParent.name.ToLower();
            
            // Check if this component sits inside any layout branch labeled for the Opponent
            if (parentNameLower == "opponent" || parentNameLower.Contains("opponent"))
            {
                belongsToOpponent = true;
                break;
            }
            // Stop parsing early if we explicitly hit the Player's tree branch root instead
            if (parentNameLower == "player" || parentNameLower.Contains("player"))
            {
                belongsToOpponent = false;
                break;
            }

            currentParent = currentParent.parent;
        }

        // 🌟 ANTI-CHEAT BLOCK ENFORCED: Disallow interactions if it belongs to the opponent
        if (belongsToOpponent)
        {
            Debug.LogWarning($"[AVATAR POWER VIOLATION] Action Blocked! {powerName} belongs to the opponent. You cannot trigger it on your turn!");
            return;
        }

        isPowerAllowed = true;

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
        if (!isPowerAllowed || activeLine == null || lineRect == null) return;

        Vector2 startPos = eventData.pressPosition; 
        Vector2 endPos = eventData.position;

        lineRect.position = startPos;

        Vector2 direction = endPos - startPos;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        lineRect.rotation = Quaternion.Euler(0, 0, angle - 90);

        float distance = Vector2.Distance(startPos, endPos);
        lineRect.sizeDelta = new Vector2(lineRect.sizeDelta.x, distance);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (activeLine != null) Destroy(activeLine);
        if (!isPowerAllowed) return;

        isPowerAllowed = false;

        GameObject hitObject = eventData.pointerCurrentRaycast.gameObject;
        
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

        if (hitOpponentFace)
        {
            ExecutePowerDamage();
            OpponentHP opponent = FindFirstObjectByType<OpponentHP>();
            if (opponent != null) opponent.TakeDamage(damageAmount);
            Debug.Log($"[{powerName}] Blasted face for {damageAmount} damage!");
            return;
        }

        CardDisplay targetCard = hitObject.GetComponentInParent<CardDisplay>();
        if (targetCard != null && targetCard.transform.parent != null)
        {
            if (targetCard.transform.parent.name == "BattlefieldOpponent" || targetCard.transform.parent.CompareTag("BattlefieldOpponent"))
            {
                // 👥 SHADOWVEIL SECURITY CHECK
                if (targetCard.hasShadowveil)
                {
                    Debug.LogWarning($"[HEROIC POWER CANCELLED] Cannot target {targetCard.displayCard.name} because it is shrouded in Shadowveil!");
                    return; 
                }

                ExecutePowerDamage();
                targetCard.TakeDamage(damageAmount);
                Debug.Log($"[{powerName}] Dealt {damageAmount} damage to {targetCard.gameObject.name}!");
            }
        }
    }

    private void ExecutePowerDamage()
    {
        if (turnSystem != null)
        {
            turnSystem.SpendEssence(essenceCost);
            hasBeenUsedThisTurn = true; 
        }
    }

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
                Debug.LogWarning($"[AI POWER CANCELLED] Cannot target {targetCard.displayCard.name} due to Shadowveil.");
                return;
            }
            
            // Deduct cost and mark as used
            turnSystem.SpendEssence(essenceCost);
            hasBeenUsedThisTurn = true;
            
            targetCard.TakeDamage(damageAmount);
            Debug.Log($"[AI AVATAR POWER] Blasted creature {targetCard.gameObject.name} for {damageAmount} damage!");
        }
        else if (targetObject.GetComponent<PlayerHP>() != null)
        {
            turnSystem.SpendEssence(essenceCost);
            hasBeenUsedThisTurn = true;

            PlayerHP playerFace = FindFirstObjectByType<PlayerHP>();
            if (playerFace != null) playerFace.TakeDamage(damageAmount);
            Debug.Log($"[AI AVATAR POWER] Blasted player face directly for {damageAmount} damage!");
        }
    }
}