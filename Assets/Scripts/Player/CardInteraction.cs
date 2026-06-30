using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(RectTransform))]
public class CardInteraction : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public enum InteractionMode { None, HandDrag, BattlefieldAttack, ETBTargeting }
    private InteractionMode currentMode = InteractionMode.None;

    [Header("Hand Drag Settings")]
    [HideInInspector] public Transform parentToReturnTo = null;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Canvas canvas;

    [Header("Battlefield Attack Settings")]
    public GameObject linePrefab; 
    private GameObject activeLine;
    private RectTransform lineRect;

    [Header("Token Summon Settings")]
    public GameObject cardPrefab;    
    public ScriptableObject Mastiff;
    public ScriptableObject Skeleton;

    [Header("Turn Rules")]
    private int turnPlayed = -1;
    public bool hasAttackedThisTurn = false; 

    private TurnSystem turnSystem;
    private CardDisplay cardDisplay;
    private bool isInitialized = false;

    void Start()
    {
        ForceInit();
    }

    public void ForceInit()
    {
        if (isInitialized) return;

        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
        
        canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindFirstObjectByType<Canvas>();

        cardDisplay = GetComponent<CardDisplay>();
        FindTurnSystem();

        if (cardDisplay != null)
        {
            if (cardDisplay.displayCard != null)
            {
                if (!string.IsNullOrEmpty(cardDisplay.displayCard.effect) && 
                    cardDisplay.displayCard.effect.IndexOf("dash", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    cardDisplay.hasDash = true;
                }

                if (cardDisplay.displayCard.name.ToLower().Contains("griffin"))
                {
                    cardDisplay.hasDash = true;
                }
            }
        }

        isInitialized = true;
    }

    private void FindTurnSystem()
    {
        if (turnSystem == null)
        {
            GameObject turnSystemGO = GameObject.Find("TurnSystem");
            if (turnSystemGO != null)
            {
                turnSystem = turnSystemGO.GetComponent<TurnSystem>();
            }
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        ForceInit();

        currentMode = InteractionMode.None;
        Transform currentParent = this.transform.parent;
        if (currentParent == null) return;

        FindTurnSystem();
        if (turnSystem == null || !turnSystem.isPlayerTurn)
        {
            eventData.pointerDrag = null;
            return; 
        }

        if (currentParent.name == "Hand" || currentParent.CompareTag("HandPlayer") || currentParent.name == "HandPlayer")
        {
            if (cardDisplay != null && cardDisplay.displayCard != null)
            {
                if (turnSystem.essenceCurrent < cardDisplay.displayCard.essence)
                {
                    Debug.LogWarning("[INTERACTION] Not enough Essence to play this card!");
                    eventData.pointerDrag = null;
                    return;
                }
            }

            currentMode = InteractionMode.HandDrag;
            parentToReturnTo = currentParent;
            this.transform.SetParent(canvas.transform);
            canvasGroup.blocksRaycasts = false; 
        }
        else if (currentParent.name == "BattlefieldPlayer" || 
                 currentParent.CompareTag("BattlefieldPlayer") || 
                 FindBattlefieldInAncestors(currentParent) != null)
        {
            if (cardDisplay != null && cardDisplay.displayCard != null)
            {
                if (!string.IsNullOrEmpty(cardDisplay.displayCard.effect) && 
                    cardDisplay.displayCard.effect.IndexOf("dash", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    cardDisplay.hasDash = true;
                }
                if (cardDisplay.displayCard.name.ToLower().Contains("griffin"))
                {
                    cardDisplay.hasDash = true;
                }
            }

            bool canBypassSlumber = (cardDisplay != null && cardDisplay.hasDash);

            if (!canBypassSlumber && turnSystem != null && turnPlayed == turnSystem.playerTurn)
            {
                Debug.LogWarning($"[RULES] {gameObject.name} is in Slumber because it was played this turn ({turnPlayed})!");
                eventData.pointerDrag = null;
                return;
            }
            
            if (hasAttackedThisTurn)
            {
                Debug.LogWarning($"[RULES] {gameObject.name} has already attacked this turn!");
                eventData.pointerDrag = null;
                return;
            }

            currentMode = InteractionMode.BattlefieldAttack;

            if (linePrefab != null && canvas != null)
            {
                activeLine = Instantiate(linePrefab, canvas.transform);
                lineRect = activeLine.GetComponent<RectTransform>();
                
                Transform backgroundTransform = canvas.transform.Find("Background");
                if (backgroundTransform != null)
                {
                    activeLine.transform.SetSiblingIndex(backgroundTransform.GetSiblingIndex() + 1);
                }
                else
                {
                    activeLine.transform.SetAsLastSibling();
                }
            }
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (canvas == null || currentMode == InteractionMode.None) return;

        if (currentMode == InteractionMode.HandDrag)
        {
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                rectTransform.position = eventData.position;
            }
            else
            {
                if (RectTransformUtility.ScreenPointToWorldPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out Vector3 globalMousePos))
                {
                    rectTransform.position = globalMousePos;
                }
            }
        }
        else if (currentMode == InteractionMode.BattlefieldAttack && activeLine != null && lineRect != null)
        {
            Vector2 startPos = transform.position;
            Vector2 endPos = eventData.position;

            lineRect.position = startPos;
            Vector2 direction = endPos - startPos;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            lineRect.rotation = Quaternion.Euler(0, 0, angle - 90);
            
            float distance = Vector2.Distance(startPos, endPos);
            lineRect.sizeDelta = new Vector2(lineRect.sizeDelta.x, distance);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (currentMode == InteractionMode.BattlefieldAttack)
        {
            currentMode = InteractionMode.None;
            if (activeLine != null) Destroy(activeLine);

            HandleAttackResolution(eventData);
            return;
        }

        if (currentMode == InteractionMode.HandDrag)
        {
            currentMode = InteractionMode.None;
            canvasGroup.blocksRaycasts = true;
            HandleHandDragResolution(eventData);
            return;
        }

        currentMode = InteractionMode.None;
        canvasGroup.blocksRaycasts = true;
    }

    public void SetSummonTurn(int currentTurn)
    {
        turnPlayed = currentTurn;
    }

    private void HandleHandDragResolution(PointerEventData eventData)
    {
        GameObject hitObject = eventData.pointerCurrentRaycast.gameObject;

        if (hitObject != null)
        {
            Transform targetBattlefield = null;
            bool isPlayerSide = true;

            // 1. Identify the target
            if (hitObject.name == "BattlefieldPlayer" || hitObject.CompareTag("BattlefieldPlayer"))
            {
                targetBattlefield = hitObject.transform;
                isPlayerSide = true;
            }
            else if (hitObject.name == "BattlefieldOpponent" || hitObject.CompareTag("BattlefieldOpponent"))
            {
                // NEW: Explicitly reject the opponent's side for player plays
                Debug.LogWarning("[RULES] You cannot play cards on the opponent's battlefield!");
                ReturnToHome();
                return; 
            }
            else
            {
                targetBattlefield = FindBattlefieldInAncestors(hitObject.transform);
                if (targetBattlefield != null)
                {
                    // Check if the ancestor found is actually the player side
                    bool isPlayer = (targetBattlefield.name == "BattlefieldPlayer" || targetBattlefield.CompareTag("BattlefieldPlayer"));
                    
                    if (!isPlayer) 
                    {
                        Debug.LogWarning("[RULES] You cannot play cards on the opponent's battlefield!");
                        ReturnToHome();
                        return;
                    }
                }
            }

            if (targetBattlefield != null && isPlayerSide)
            {
                if (targetBattlefield.childCount >= 7)
                {
                    Debug.LogWarning($"[RULES] {targetBattlefield.name} is full! Maximum of 7 creatures allowed.");
                    ReturnToHome();
                    return;
                }

                int cost = cardDisplay.displayCard.essence;
                if (!turnSystem.SpendEssence(cost))
                {
                    ReturnToHome();
                    return;
                }

                if (turnSystem != null)
                {
                    turnPlayed = turnSystem.playerTurn; 
                }

                parentToReturnTo = targetBattlefield;
                SetAtCalculatedIndex(targetBattlefield, eventData.position);

                if (cardDisplay.displayCard != null)
                {
                    string currentEffect = cardDisplay.displayCard.effect;
                    
                    Debug.Log($"[ETB TRIGGER CHECK] Card '{cardDisplay.displayCard.name}' played on PlayerSide={isPlayerSide}! Effect field reads: \"{currentEffect}\"");

                    if (!string.IsNullOrEmpty(currentEffect) && currentEffect.Contains("Heal 2"))
                    {
                        if (isPlayerSide) { PlayerHP player = FindFirstObjectByType<PlayerHP>(); if (player != null) player.Heal(2); }
                        else { OpponentHP opp = FindFirstObjectByType<OpponentHP>(); if (opp != null) opp.Heal(2); }
                    }
                    else if (!string.IsNullOrEmpty(currentEffect) && currentEffect.Contains("Heal 1"))
                    {
                        if (isPlayerSide) { PlayerHP player = FindFirstObjectByType<PlayerHP>(); if (player != null) player.Heal(1); }
                        else { OpponentHP opp = FindFirstObjectByType<OpponentHP>(); if (opp != null) opp.Heal(1); }
                    }
                    
                    if (!string.IsNullOrEmpty(currentEffect) && currentEffect.Contains("Deals 3 damage"))
                    {
                        if (isPlayerSide) { OpponentHP opponent = FindFirstObjectByType<OpponentHP>(); if (opponent != null) opponent.TakeDamage(3); }
                        else { PlayerHP player = FindFirstObjectByType<PlayerHP>(); if (player != null) player.TakeDamage(3); }
                    }
                    else if (!string.IsNullOrEmpty(currentEffect) && currentEffect.Contains("Deals 2 damage"))
                    {
                        if (isPlayerSide) { OpponentHP opponent = FindFirstObjectByType<OpponentHP>(); if (opponent != null) opponent.TakeDamage(2); }
                        else { PlayerHP player = FindFirstObjectByType<PlayerHP>(); if (player != null) player.TakeDamage(2); }
                    }
                    
                    if (!string.IsNullOrEmpty(currentEffect) && currentEffect.Contains("Thunder"))
                    {
                        TriggerThunderEffect();
                    }
                    
                    if (!string.IsNullOrEmpty(currentEffect) && currentEffect.Contains("Summon 2 Mastiffs"))
                    {
                        if (cardPrefab != null && Mastiff != null)
                        {
                            // Cast directly down to Card if it inherits from Card, otherwise look it up dynamically via reflection/casting
                            Card mastiffSO = Mastiff as Card;

                            for (int i = 0; i < 2; i++)
                            {
                                if (targetBattlefield.childCount >= 7) break; 

                                GameObject tokenCard = Instantiate(cardPrefab, targetBattlefield);
                                tokenCard.name = "Mastiff_Token";
                                tokenCard.tag = "Untagged"; 

                                CanvasGroup cg = tokenCard.GetComponent<CanvasGroup>();
                                if (cg != null) { cg.interactable = true; cg.blocksRaycasts = true; }

                                Attack oldAttackComp = tokenCard.GetComponent<Attack>();
                                if (oldAttackComp != null) Destroy(oldAttackComp);

                                CardDisplay tokenDisplay = tokenCard.GetComponent<CardDisplay>();
                                if (tokenDisplay != null)
                                {
                                    // 🛠️ DIRECT ASSIGNMENT MECHANIC: 
                                    // Bypass CardDatabase lookup indexes entirely since it doesn't live there.
                                    tokenDisplay.displayId = -99; 
                                    if (mastiffSO != null)
                                    {
                                        tokenDisplay.displayCard = mastiffSO;
                                        tokenDisplay.currentAttack = mastiffSO.attack;
                                        tokenDisplay.currentVigor = mastiffSO.vigor;
                                    }
                                    tokenDisplay.RefreshCardUI();
                                }

                                CardCover cover = tokenCard.GetComponent<CardCover>();
                                if (cover != null)
                                {
                                    cover.isCoverActive = false; 
                                    cover.UpdateCoverState();    
                                }

                                CardInteraction tokenInteraction = tokenCard.GetComponent<CardInteraction>();
                                if (tokenInteraction == null) tokenInteraction = tokenCard.AddComponent<CardInteraction>();

                                if (tokenInteraction != null)
                                {
                                    tokenInteraction.linePrefab = this.linePrefab;
                                    tokenInteraction.ForceInit();
                                    tokenInteraction.parentToReturnTo = targetBattlefield;
                                    
                                    if (turnSystem != null) tokenInteraction.turnPlayed = turnSystem.playerTurn; 
                                    tokenInteraction.hasAttackedThisTurn = false;
                                }

                                RectTransform tokenRect = tokenCard.GetComponent<RectTransform>();
                                if (tokenRect != null)
                                {
                                    tokenRect.localScale = Vector3.one;
                                    tokenRect.localPosition = Vector3.zero;
                                    tokenRect.anchoredPosition = Vector2.zero;
                                    tokenRect.localRotation = Quaternion.Euler(25, 0, 0);
                                }
                            }
                            LayoutRebuilder.ForceRebuildLayoutImmediate(targetBattlefield.GetComponent<RectTransform>());
                        }
                        else if (Mastiff == null)
                        {
                            Debug.LogError("[ASSET ERROR] The Mastiff slot on CardInteraction is Empty! Drop Assets/SOCards/Mastiff.asset into the inspector field.");
                        }
                    }

                    if (!string.IsNullOrEmpty(currentEffect) && currentEffect.Contains("Draw 2 cards"))
                    {
                        if (isPlayerSide)
                        {
                            PlayerDeck playerDeckScript = FindFirstObjectByType<PlayerDeck>();
                            if (playerDeckScript != null) { playerDeckScript.DrawSingleCardDirectly(); playerDeckScript.DrawSingleCardDirectly(); }
                        }
                    }
                    else if (!string.IsNullOrEmpty(currentEffect) && currentEffect.Contains("Draw a card"))
                    {
                        if (isPlayerSide)
                        {
                            PlayerDeck playerDeckScript = FindFirstObjectByType<PlayerDeck>();
                            if (playerDeckScript != null) playerDeckScript.DrawSingleCardDirectly();
                        }
                    }
                    
                    if (!string.IsNullOrEmpty(currentEffect) && currentEffect.Contains("Other monsters you control get +1/+1"))
                    {
                        foreach (Transform cardTransform in targetBattlefield)
                        {
                            if (cardTransform.gameObject == this.gameObject) continue;
                            CardDisplay creatureDisplay = cardTransform.GetComponent<CardDisplay>();
                            if (creatureDisplay != null)
                            {
                                creatureDisplay.currentAttack += 1;
                                creatureDisplay.currentVigor += 1;
                                creatureDisplay.isBuffed = true;
                                creatureDisplay.RefreshCardUI();
                            }
                        }
                    }
                    
                    if (!string.IsNullOrEmpty(currentEffect) && currentEffect.Contains("Other monsters you control get +0/+1"))
                    {
                        foreach (Transform cardTransform in targetBattlefield)
                        {
                            if (cardTransform.gameObject == this.gameObject) continue;
                            CardDisplay creatureDisplay = cardTransform.GetComponent<CardDisplay>();
                            if (creatureDisplay != null)
                            {
                                creatureDisplay.currentVigor += 1;
                                creatureDisplay.isBuffed = true;
                                creatureDisplay.RefreshCardUI();
                            }
                        }
                    }
                    
                    if (!string.IsNullOrEmpty(currentEffect) && currentEffect.Contains("Shadowveil"))
                    {
                        if (cardDisplay != null) cardDisplay.hasShadowveil = true;
                    }
                    
                    if (!string.IsNullOrEmpty(currentEffect) && currentEffect.Contains("each turn you played"))
                    {
                        if (turnSystem != null)
                        {
                            int turnsPlayedCount = turnSystem.playerTurn; 
                            cardDisplay.currentAttack += turnsPlayedCount;
                            cardDisplay.currentVigor += turnsPlayedCount;
                            cardDisplay.isBuffed = true;
                        }
                    }
                    
                    if (!string.IsNullOrEmpty(currentEffect) && currentEffect.Contains("enters and dies draw a card"))
                    {
                        if (isPlayerSide)
                        {
                            PlayerDeck playerDeckScript = FindFirstObjectByType<PlayerDeck>();
                            if (playerDeckScript != null) playerDeckScript.DrawSingleCardDirectly();
                        }
                    }
                    
                    if (!string.IsNullOrEmpty(currentEffect) && currentEffect.ToLower().Contains("deals 2 damage when") && currentEffect.ToLower().Contains("enters or dies"))
                    {
                        if (isPlayerSide) { OpponentHP opponentFace = FindFirstObjectByType<OpponentHP>(); if (opponentFace != null) opponentFace.TakeDamage(2); }
                        else { PlayerHP playerFace = FindFirstObjectByType<PlayerHP>(); if (playerFace != null) playerFace.TakeDamage(2); }
                    }

                    if (!string.IsNullOrEmpty(currentEffect) && currentEffect.Contains("Deals 1 damage to 3 random foes"))
                    {
                        List<object> validTargets = new List<object>();
                        string opposingFieldTag = isPlayerSide ? "BattlefieldOpponent" : "BattlefieldPlayer";
                        GameObject enemyBattlefield = GameObject.FindWithTag(opposingFieldTag);
                        if (enemyBattlefield == null) enemyBattlefield = GameObject.Find(opposingFieldTag);

                        if (enemyBattlefield != null)
                        {
                            CardDisplay[] enemyMonsters = enemyBattlefield.GetComponentsInChildren<CardDisplay>();
                            foreach (var monster in enemyMonsters) if (monster != null) validTargets.Add(monster);
                        }

                        if (isPlayerSide) { OpponentHP face = FindFirstObjectByType<OpponentHP>(); if (face != null) validTargets.Add(face); }
                        else { PlayerHP face = FindFirstObjectByType<PlayerHP>(); if (face != null) validTargets.Add(face); }

                        if (validTargets.Count > 0)
                        {
                            for (int i = 0; i < 3; i++)
                            {
                                int randomIndex = Random.Range(0, validTargets.Count);
                                object chosenFoe = validTargets[randomIndex];
                                if (chosenFoe is CardDisplay monster) monster.TakeDamage(1);
                                else if (chosenFoe is OpponentHP faceOpp) faceOpp.TakeDamage(1);
                                else if (chosenFoe is PlayerHP facePl) facePl.TakeDamage(1);
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(currentEffect) && 
                        (cardDisplay.displayCard.name.ToLower().Contains("arcane mage") || 
                        cardDisplay.displayCard.name.ToLower().Contains("mana elemental") ||
                        currentEffect.ToLower().Contains("grants 2 essence") ||
                        currentEffect.ToLower().Contains("grants 1 essence")))
                    {
                        int essenceToRestore = 0;
                        if (currentEffect.ToLower().Contains("grants 2 essence") || cardDisplay.displayCard.name.ToLower().Contains("arcane mage")) essenceToRestore = 2;
                        else if (currentEffect.ToLower().Contains("grants 1 essence") || cardDisplay.displayCard.name.ToLower().Contains("mana elemental")) essenceToRestore = 1;

                        if (essenceToRestore > 0 && turnSystem != null)
                        {
                            if (isPlayerSide)
                            {
                                turnSystem.playerEssenceCurrent = Mathf.Min(turnSystem.playerEssenceCurrent + essenceToRestore, turnSystem.playerEssenceMax);
                            }
                            else
                            {
                                turnSystem.opponentEssenceCurrent = Mathf.Min(turnSystem.opponentEssenceCurrent + essenceToRestore, turnSystem.opponentEssenceMax);
                            }
                            turnSystem.UpdateUIElements();
                        }
                    }
                } 

                if (cardDisplay == null || cardDisplay.displayCard == null) 
                {
                    Debug.LogWarning("Card spawned without data! Forcing refresh.");
                    return; 
                }

                if (cardDisplay != null) cardDisplay.RefreshCardUI();
                return;
            } 
        } 

        ReturnToHome();
    }

    private void HandleAttackResolution(PointerEventData eventData)
    {
        if (activeLine != null) Destroy(activeLine);

        GameObject hitObject = eventData.pointerCurrentRaycast.gameObject;
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

        bool enemyHasProvokeActive = false;
        GameObject enemyBattlefield = GameObject.FindWithTag("BattlefieldOpponent");
        if (enemyBattlefield == null) enemyBattlefield = GameObject.Find("BattlefieldOpponent");

        if (enemyBattlefield != null)
        {
            CardDisplay[] enemyMonsters = enemyBattlefield.GetComponentsInChildren<CardDisplay>();
            foreach (CardDisplay monster in enemyMonsters)
            {
                if (monster != null && monster.displayCard != null && !string.IsNullOrEmpty(monster.displayCard.effect))
                {
                    if (monster.displayCard.effect.IndexOf("Provoke", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        enemyHasProvokeActive = true;
                        break;
                    }
                }
            }
        }

        if (cardDisplay != null && cardDisplay.displayCard != null)
        {
            string effectText = cardDisplay.displayCard.effect;

            if (!string.IsNullOrEmpty(effectText) && effectText.Contains("Deals 2 damage to a random opponent target when attacking."))
            {
                TriggerDjinnAttackStrike();
            }

            if (!string.IsNullOrEmpty(effectText))
            {
                if (effectText.Contains("Thunder")) TriggerThunderEffect();
                if (effectText.IndexOf("Corrosive", System.StringComparison.OrdinalIgnoreCase) >= 0) TriggerCorrosiveEffect(true);
            }
        }

        if (cardDisplay != null && cardDisplay.hasShadowveil)
        {
            cardDisplay.hasShadowveil = false;
            cardDisplay.RefreshCardUI();
        }

        Transform currentCheck = hitObject.transform;
        bool hitOpponentFace = false;
        while (currentCheck != null)
        {
            if (currentCheck.name == "OpponentHP" || currentCheck.gameObject.name == "OpponentHP" || currentCheck.CompareTag("Opponent"))
            {
                hitOpponentFace = true;
                break;
            }
            currentCheck = currentCheck.parent;
        }

        if (hitOpponentFace)
        {
            if (enemyHasProvokeActive) return;

            OpponentHP opponent = FindFirstObjectByType<OpponentHP>();
            if (opponent != null)
            {
                if (cardDisplay.displayCard != null && !string.IsNullOrEmpty(cardDisplay.displayCard.effect))
                {
                    if (cardDisplay.displayCard.effect.Trim().Equals("Vampirism", System.StringComparison.OrdinalIgnoreCase))
                    {
                        int faceDamage = (int)cardDisplay.currentAttack;
                        int actualDamage = Mathf.Min(faceDamage, (int)opponent.hp);
                        if (actualDamage > 0)
                        {
                            PlayerHP player = FindFirstObjectByType<PlayerHP>();
                            if (player != null) player.Heal(actualDamage);
                        }
                    }
                }

                opponent.TakeDamage(cardDisplay.currentAttack, true);
                hasAttackedThisTurn = true; 
            }
            return;
        }

        CardDisplay targetCard = hitObject.GetComponentInParent<CardDisplay>();
        if (targetCard != null && targetCard.transform.parent != null)
        {
            if (targetCard.transform.parent.name == "BattlefieldOpponent" || targetCard.transform.parent.CompareTag("BattlefieldOpponent"))
            {
                if (targetCard.hasShadowveil) return; 

                if (enemyHasProvokeActive)
                {
                    bool targetHasProvoke = false;
                    if (targetCard.displayCard != null && !string.IsNullOrEmpty(targetCard.displayCard.effect))
                    {
                        if (targetCard.displayCard.effect.IndexOf("Provoke", System.StringComparison.OrdinalIgnoreCase) >= 0) targetHasProvoke = true;
                    }
                    if (!targetHasProvoke) return;
                }

                if (Effect.Instance != null)
                {
                    Effect.Instance.ResolveMonsterCombat(cardDisplay, targetCard, true);
                }
                else
                {
                    // Use CURRENT stats, not base stats from the ScriptableObject
                    int playerDamageAmount = cardDisplay.currentAttack; 
                    int enemyDamageAmount = targetCard.currentAttack; 
                    
                    targetCard.TakeDamage(playerDamageAmount);
                    cardDisplay.TakeDamage(enemyDamageAmount);
                }

                if (targetCard.currentVigor <= 0)
                {
                    Will[] allWillPools = FindObjectsByType<Will>(FindObjectsSortMode.None);
                    foreach (Will pool in allWillPools)
                    {
                        if (pool != null && pool.gameObject.name.IndexOf("Opponent", System.StringComparison.OrdinalIgnoreCase) < 0)
                        {
                            pool.GainWill(1);
                            break;
                        }
                    }
                }

                hasAttackedThisTurn = true; 
            }
        }
    }

    private void TriggerDjinnAttackStrike()
    {
        List<object> validTargets = new List<object>();
        GameObject enemyBattlefield = GameObject.FindWithTag("BattlefieldOpponent");
        if (enemyBattlefield == null) enemyBattlefield = GameObject.Find("BattlefieldOpponent");

        if (enemyBattlefield != null)
        {
            CardDisplay[] enemyMonsters = enemyBattlefield.GetComponentsInChildren<CardDisplay>();
            foreach (var monster in enemyMonsters) if (monster != null) validTargets.Add(monster);
        }

        OpponentHP opponentFace = FindFirstObjectByType<OpponentHP>();
        if (opponentFace != null) validTargets.Add(opponentFace);

        if (validTargets.Count == 0) return;

        int randomIndex = Random.Range(0, validTargets.Count);
        object chosenTarget = validTargets[randomIndex];

        if (chosenTarget is CardDisplay targetMonster) targetMonster.TakeDamage(2);
        else if (chosenTarget is OpponentHP face) face.TakeDamage(2);
    }

    private void TriggerThunderEffect()
    {
        GameObject enemyBattlefield = GameObject.FindWithTag("BattlefieldOpponent");
        if (enemyBattlefield == null) enemyBattlefield = GameObject.Find("BattlefieldOpponent");

        if (enemyBattlefield != null)
        {
            CardDisplay[] enemyMonsters = enemyBattlefield.GetComponentsInChildren<CardDisplay>();
            foreach (CardDisplay monster in enemyMonsters) if (monster != null) monster.TakeDamage(1);
        }

        OpponentHP opponentAvatar = FindFirstObjectByType<OpponentHP>();
        if (opponentAvatar != null) opponentAvatar.TakeDamage(1);
    }

    private void TriggerCorrosiveEffect(bool targetOpponentSide)
    {
        List<object> validTargets = new List<object>();

        if (targetOpponentSide)
        {
            GameObject enemyBattlefield = GameObject.FindWithTag("BattlefieldOpponent");
            if (enemyBattlefield == null) enemyBattlefield = GameObject.Find("BattlefieldOpponent");

            if (enemyBattlefield != null)
            {
                CardDisplay[] enemyMonsters = enemyBattlefield.GetComponentsInChildren<CardDisplay>();
                foreach (var monster in enemyMonsters) if (monster != null) validTargets.Add(monster);
            }

            OpponentHP opponentFace = FindFirstObjectByType<OpponentHP>();
            if (opponentFace != null) validTargets.Add(opponentFace);
        }

        if (validTargets.Count == 0) return;

        for (int i = 0; i < 2; i++)
        {
            if (validTargets.Count == 0) break;
            int randomIndex = Random.Range(0, validTargets.Count);
            object chosenTarget = validTargets[randomIndex];

            if (chosenTarget is CardDisplay monster) monster.TakeDamage(2);
            else if (chosenTarget is OpponentHP face) face.TakeDamage(2);
        }
    }

    private Transform FindBattlefieldInAncestors(Transform current)
    {
        while (current != null)
        {
            if (current.name == "BattlefieldPlayer" || current.CompareTag("BattlefieldPlayer") || 
                current.name == "BattlefieldOpponent" || current.CompareTag("BattlefieldOpponent")) return current;
            current = current.parent;
        }
        return null;
    }

    private void SetAtCalculatedIndex(Transform battlefield, Vector2 mousePosition)
    {
        this.transform.SetParent(battlefield);
        int targetIndex = battlefield.childCount;

        for (int i = 0; i < battlefield.childCount; i++)
        {
            Transform child = battlefield.GetChild(i);
            if (child == this.transform) continue;

            RectTransform childRect = child.GetComponent<RectTransform>();
            if (childRect != null)
            {
                if (mousePosition.x < childRect.position.x)
                {
                    targetIndex = i;
                    break; 
                }
            }
        }

        this.transform.SetSiblingIndex(targetIndex);
        ReturnToHome();
        LayoutRebuilder.ForceRebuildLayoutImmediate(battlefield.GetComponent<RectTransform>());
    }

    private void ReturnToHome()
    {
        if (parentToReturnTo == null) return;
        this.transform.SetParent(parentToReturnTo);
        rectTransform.localPosition = Vector3.zero;
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.localEulerAngles = new Vector3(25, 0, 0);
    }
}