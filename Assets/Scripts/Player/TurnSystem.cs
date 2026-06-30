using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TurnSystem : MonoBehaviour
{
    public bool isPlayerTurn;
    public int playerTurn = 1;
    public int opponentTurn = 0;
    public TMP_Text turnText;

    public int essenceCurrent => isPlayerTurn ? playerEssenceCurrent : opponentEssenceCurrent;
    public int essenceMax => isPlayerTurn ? playerEssenceMax : opponentEssenceMax;

    [Header("Essence Pools")]
    public int playerEssenceMax = 1;
    public int playerEssenceCurrent = 1;
    public TMP_Text playerEssenceText;
    public int opponentEssenceMax = 1;
    public int opponentEssenceCurrent = 1;
    public TMP_Text opponentEssenceText;


    private void Start()
    {
        isPlayerTurn = true;
        UpdateUIElements();
    }

    private void Update()
    {
        if (turnText != null)
            turnText.text = isPlayerTurn ? "Your Turn" : "Opponent's Turn";
    }

    public void EndPlayerTurn()
    {
        if (!isPlayerTurn) return;

        // 1. Process all effects on the Player board
        ProcessEndOfTurnEffects(GameObject.FindWithTag("BattlefieldPlayer"), true);

        // 2. Zephyr Special Check
        HandleZephyrSwarm(GameObject.FindWithTag("BattlefieldPlayer"), true);

        // 3. Switch Turns
        isPlayerTurn = false;
        opponentTurn++;
        opponentEssenceMax = Mathf.Min(opponentEssenceMax + 1, 10);
        opponentEssenceCurrent = opponentEssenceMax;

        // 4. Setup Opponent Board
        ResetBattlefieldRestrictions(GameObject.FindWithTag("BattlefieldOpponent"));
        FindFirstObjectByType<OpponentDeck>()?.TriggerOpponentDraw(1);
        FindFirstObjectByType<OpponentManager>()?.StartOpponentTurn();

        UpdateUIElements();
    }

    public void EndOpponentTurn()
    {
        if (isPlayerTurn) return;

        // 1. Process all effects on the Opponent board
        GameObject opponentBattlefield = GameObject.FindWithTag("BattlefieldOpponent");
        ProcessEndOfTurnEffects(opponentBattlefield, false);

        // 2. Zephyr Special Check for AI
        // We call this manually since we are in the Opponent Turn context
        if (opponentBattlefield != null)
        {
            foreach (Transform child in opponentBattlefield.transform)
            {
                var display = child.GetComponent<CardDisplay>();
                if (display != null && display.displayCard.name.ToLower().Contains("zephyr"))
                {
                    // AI path: Call the AI-safe manager method
                    FindFirstObjectByType<OpponentManager>()?.TriggerZephyrEffectForAI();
                }
            }
        }

        // 3. Switch Turns
        isPlayerTurn = true;
        playerTurn++;
        playerEssenceMax = Mathf.Min(playerEssenceMax + 1, 10);
        playerEssenceCurrent = playerEssenceMax;

        // 4. Reset Player board
        ResetBattlefieldRestrictions(GameObject.FindWithTag("BattlefieldPlayer"));
        FindFirstObjectByType<PlayerDeck>()?.TriggerPlayerDraw(1);
        
        foreach (var power in FindObjectsByType<AvatarPower>(FindObjectsSortMode.None))
            if (power != null) power.hasBeenUsedThisTurn = false;

        UpdateUIElements();
    }

    private void ProcessEndOfTurnEffects(GameObject battlefield, bool isPlayer)
    {
        if (battlefield == null) return;
        var aiManager = FindFirstObjectByType<OpponentManager>();

        foreach (Transform child in battlefield.transform)
        {
            CardDisplay card = child.GetComponent<CardDisplay>();
            if (card == null || card.displayCard == null) continue;

            string name = card.displayCard.name.ToLower();
            string effect = card.displayCard.effect?.ToLower() ?? "";

            if (name.Contains("merlin") || effect.Contains("4 damage"))
                card.TriggerMerlinEndTurnEffect(isPlayer);
            else if (name.Contains("mutated hunter") || effect.Contains("growth"))
                card.TriggerMutatedHunterGrowth();
            else if (name.Contains("pan piper") || effect.Contains("summons a rat"))
            {
                if (!isPlayer && aiManager != null) aiManager.SpawnTokensForAI(aiManager.ratSO, 1);
                else card.TriggerPanPiperSummon(isPlayer);
            }
        }
    }

    private void HandleZephyrSwarm(GameObject battlefield, bool isPlayer)
    {
        foreach (Transform child in battlefield.transform)
        {
            var display = child.GetComponent<CardDisplay>();
            if (display != null && display.displayCard.name.Contains("Zephyr"))
                TriggerZephyrSkeletonSwarm(battlefield, child.GetComponent<CardInteraction>());
        }
    }

    private void ResetBattlefieldRestrictions(GameObject battlefield)
    {
        if (battlefield == null) return;
        foreach (var monster in battlefield.GetComponentsInChildren<CardDisplay>())
            if (monster != null) monster.ResetTurnRestrictions();
    }
    
    public void PerformZephyrSwarm(GameObject targetBattlefield)
    {
        // Find any Zephyr on that battlefield to act as the "creator" for the prefab references
        foreach (Transform child in targetBattlefield.transform)
        {
            var display = child.GetComponent<CardDisplay>();
            if (display != null && display.displayCard.name.Contains("Zephyr"))
            {
                TriggerZephyrSkeletonSwarm(targetBattlefield, child.GetComponent<CardInteraction>());
                break; // Only trigger once per Zephyr or per turn
            }
        }
    }

    private void TriggerZephyrSkeletonSwarm(GameObject battlefield, CardInteraction creator)
    {
        ScriptableObject skeletonSO = null;
        if (CardDatabase.cardList != null)
        {
            foreach (var card in CardDatabase.cardList)
            {
                if (card != null && card.name.Equals("Skeleton", System.StringComparison.OrdinalIgnoreCase))
                {
                    skeletonSO = card;
                    break;
                }
            }
        }

        if (skeletonSO == null) return;

        while (battlefield.transform.childCount < 7)
        {
            GameObject tokenCard = Instantiate(creator.cardPrefab, battlefield.transform);
            tokenCard.name = "Skeleton_Token";

            CanvasGroup cg = tokenCard.GetComponent<CanvasGroup>();
            if (cg != null) { cg.interactable = true; cg.blocksRaycasts = true; }

            Attack oldAttackComp = tokenCard.GetComponent<Attack>();
            if (oldAttackComp != null) Destroy(oldAttackComp);

            CardDisplay tokenDisplay = tokenCard.GetComponent<CardDisplay>();
            if (tokenDisplay != null)
            {
                tokenDisplay.displayCard = (Card)skeletonSO;
                tokenDisplay.currentAttack = tokenDisplay.displayCard.attack;
                tokenDisplay.currentVigor = tokenDisplay.displayCard.vigor;
                tokenDisplay.cardCover = false; 
                tokenDisplay.RefreshCardUI();
            }

            CardInteraction tokenInteraction = tokenCard.GetComponent<CardInteraction>();
            if (tokenInteraction == null) tokenInteraction = tokenCard.AddComponent<CardInteraction>();
            if (tokenInteraction != null)
            {
                tokenInteraction.linePrefab = creator.linePrefab;
                tokenInteraction.cardPrefab = creator.cardPrefab;
                tokenInteraction.Skeleton = creator.Skeleton;
                tokenInteraction.ForceInit();
                tokenInteraction.parentToReturnTo = battlefield.transform;
                tokenInteraction.hasAttackedThisTurn = false;
            }

            RectTransform tokenRect = tokenCard.GetComponent<RectTransform>();
            if (tokenRect != null)
            {
                tokenRect.localScale = Vector3.one;
                tokenRect.localPosition = Vector3.zero;
                tokenRect.anchoredPosition = Vector2.zero;
                tokenRect.localRotation = Quaternion.Euler(0, 0, 0); 
            }
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(battlefield.GetComponent<RectTransform>());
    }

    public void UpdateUIElements()
    {
        if (playerEssenceText) playerEssenceText.text = $"{playerEssenceCurrent}/{playerEssenceMax}";
        if (opponentEssenceText) opponentEssenceText.text = $"{opponentEssenceCurrent}/{opponentEssenceMax}";
    }

    public bool SpendEssence(int amount)
    {
        ref int current = ref (isPlayerTurn ? ref playerEssenceCurrent : ref opponentEssenceCurrent);
        if (current >= amount)
        {
            current -= amount;
            UpdateUIElements();
            return true;
        }
        return false;
    }
}