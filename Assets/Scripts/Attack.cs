using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Attack : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
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

        // 1. Summoning Sickness Check
        // Get the current turn from your TurnSystem (ensure your TurnSystem has a public variable or property for this)
        int currentGlobalTurn = turnSystem.isPlayerTurn ? turnSystem.playerTurn : turnSystem.opponentTurn;
        
        // Existing validation logic
        Transform currentParent = this.transform.parent;
        if (currentParent == null) return;

        // 2. Summoning Sickness Check
        // Get the current global turn count
        int currentTurnCount = turnSystem.isPlayerTurn ? turnSystem.playerTurn : turnSystem.opponentTurn;
        
        // Check if the card was spawned on this current turn
        if (cardDisplay != null && cardDisplay.turnSpawned == currentTurnCount)
        {
            Debug.LogWarning($"[COMBAT] {gameObject.name} has summoning sickness and cannot attack!");
            return; 
        }

        // 3. Validation Logic
        if (!turnSystem.isPlayerTurn) return;
        if (currentParent.name == "Hand" || currentParent.CompareTag("HandPlayer")) return;
        
        if (!IsValidBattlefieldParent(currentParent))
        {
            Debug.LogWarning($"[ATTACK BLOCK] Drag rejected for '{gameObject.name}'");
            return;
        }

        isAttackingAllowed = true;

        // 4. Setup targeting line (your existing code)
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
        
        if (linePrefab != null && canvas != null)
        {
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
        if (!isAttackingAllowed || activeLine == null || lineRect == null) return;
        
        Vector2 startPos = transform.position;
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

        // Lógica de Detecção de Alvo
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
            AttackOpponentFace();
            return;
        }

        // Ataque a monstros
        CardDisplay targetCard = hitObject.GetComponentInParent<CardDisplay>();
        if (targetCard != null && targetCard.transform.parent != null)
        {
            if (targetCard.transform.parent.name == "BattlefieldOpponent" || targetCard.transform.parent.CompareTag("BattlefieldOpponent"))
            {
                AttackEnemyMonster(targetCard);
            }
        }
    }

    private void AttackOpponentFace()
    {
        OpponentHP opponent = FindFirstObjectByType<OpponentHP>();
        if (opponent != null && cardDisplay != null)
        {
            Debug.Log($"[COMBAT] Ataque direto para {cardDisplay.currentAttack} de dano!");
            opponent.TakeDamage(cardDisplay.currentAttack);
        }
    }

    private void AttackEnemyMonster(CardDisplay enemy)
    {
        if (cardDisplay == null || enemy == null) return;
        Debug.Log($"[COMBAT] Combate entre monstros!");
        
        int pDmg = cardDisplay.currentAttack;
        int eDmg = enemy.currentAttack;

        enemy.TakeDamage(pDmg);
        cardDisplay.TakeDamage(eDmg);
    }

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