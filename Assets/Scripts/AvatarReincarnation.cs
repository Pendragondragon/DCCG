using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class AvatarReincarnationButton : MonoBehaviour, IPointerClickHandler
{
    // Variables Needed
    public enum AvatarType { Zephyr, Dragonslayer }

    [Header("Required References")]
    public Will playerWill;      
    public TurnSystem turnSystem;

    [Header("Avatar Settings")]
    public AvatarType avatarType;
    public int willCost = 7;
    public GameObject cardPrefab;          
    public GameObject linePrefab;

    [Header("Zephyr Configuration")]
    public ScriptableObject zephyrCardData; 
    public ScriptableObject mastiffSO;

    [Header("Dragonslayer Configuration")]
    public ScriptableObject ragnarkCardData; 
    public ScriptableObject blackDragonSO; 

    void Start()
    {
        // Automatically find the TurnSystem if it's lost
        if (turnSystem == null) 
        {
            turnSystem = FindFirstObjectByType<TurnSystem>();
        }

        // Automatically find the Will pool based on the name
        if (playerWill == null)
        {
            // Check if this button is inside the Player or Opponent hierarchy
            bool isPlayer = transform.root.name.Contains("Player");
            string searchName = isPlayer ? "PlayerWill" : "OpponentWill";
            
            GameObject willObj = GameObject.Find(searchName);
            if (willObj != null) playerWill = willObj.GetComponentInChildren<Will>();
        }
    }   

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            OnReincarnationClicked();
        }
    }

    public void OnReincarnationClicked()
    {
        bool isPlayer = gameObject.transform.root.name.Contains("Player");
    
        if (turnSystem == null || !turnSystem.isPlayerTurn) return;

        // Game Rule: Needs Exactly 7will points to perform
        if (playerWill != null && playerWill.currentWill == 7) 
        {
            // Reset cost once its used
            playerWill.currentWill = 0; 
            playerWill.UpdateWillVisuals();
            ExecuteReincarnation(true);
        }
        else
        {
            Debug.LogWarning("Reincarnation: Not enough Will or wrong amount!");
        }
    }

    public bool TryExecuteReincarnationForAI()
    {
        // Specifically finds the Opponent's Will
        Will oppWill = GameObject.Find("OpponentWill")?.GetComponentInChildren<Will>();
        
        // opponent logic: if Will is 7, spend to 0 and summon
        if (oppWill != null && oppWill.currentWill == 7)
        {
            oppWill.currentWill = 0;
            oppWill.UpdateWillVisuals();
            ExecuteReincarnation(false); // false = isOpponentSide
            return true;
        }
        return false;
    }

    private void ExecuteReincarnation(bool isPlayerSide)
    {
        // Setup identification
        Transform rootParent = transform;
        while (rootParent.parent != null)
        {
            rootParent = rootParent.parent;
            if (isPlayerSide && rootParent.name.Contains("Player")) break;
            if (!isPlayerSide && rootParent.name.Contains("Opponent")) break;
        }

        Will sideWill = isPlayerSide ? playerWill : GameObject.Find("OpponentWill")?.GetComponentInChildren<Will>();
        if (sideWill == null) 
        {
            Debug.LogError($"Critical: Could not find Will pool for side: {(isPlayerSide ? "Player" : "Opponent")}");
            return;
        }

        // Board Check
        string laneName = isPlayerSide ? "BattlefieldPlayer" : "BattlefieldOpponent";
        GameObject targetBattlefield = GameObject.Find(laneName) ?? GameObject.FindWithTag(laneName);

        // Check to not happen if the board already has 7 monsters
        if (targetBattlefield == null || targetBattlefield.transform.childCount >= 7) return;

        ScriptableObject targetSO = (avatarType == AvatarType.Zephyr) ? zephyrCardData : ragnarkCardData;
        if (targetSO == null) return;

        // Spend Resources
        sideWill.SpendWill(willCost);

        // Instantiate and setup variables
        GameObject bossInstance = Instantiate(cardPrefab, targetBattlefield.transform);
        CardDisplay display = bossInstance.GetComponent<CardDisplay>();
        TurnSystem ts = FindFirstObjectByType<TurnSystem>();

        // Turn State
        if (display != null && ts != null)
        {
            display.turnSpawned = isPlayerSide ? ts.playerTurn : ts.opponentTurn;
            display.turnSummoned = display.turnSpawned; // Syncing both
        }

        // Handle Components
        Attack oldAttackComp = bossInstance.GetComponent<Attack>();
        if (oldAttackComp != null) Destroy(oldAttackComp);

        CardInteraction interaction = bossInstance.GetComponent<CardInteraction>();

        // Configure Avatar Type
        if (avatarType == AvatarType.Zephyr)
        {
            bossInstance.name = "Zephyr_Necromancer";
            if (display != null) display.displayCard = (Card)targetSO;
            if (interaction != null) interaction.Mastiff = this.mastiffSO;
        }
        else if (avatarType == AvatarType.Dragonslayer)
        {
            bossInstance.name = "Ragnark_Dragonslayer";
            if (display != null) display.displayCard = (Card)targetSO;

            RagnarkTrigger deathTrigger = bossInstance.GetComponent<RagnarkTrigger>() ?? bossInstance.AddComponent<RagnarkTrigger>();
            deathTrigger.blackDragonData = blackDragonSO;
            deathTrigger.cardPrefab = cardPrefab;
            deathTrigger.linePrefab = linePrefab;
        }

        // Card UI Refresh
        if (display != null && display.displayCard != null)
        {
            display.currentAttack = display.displayCard.attack;
            display.currentVigor = display.displayCard.vigor;
            display.cardCover = false; 
            display.hasSlumber = true;
            display.RefreshCardUI();
        }

        // Interaction Setup
        if (isPlayerSide)
        {
            // Player Confguration
            if (interaction == null) interaction = bossInstance.AddComponent<CardInteraction>();
            interaction.linePrefab = this.linePrefab;
            interaction.cardPrefab = this.cardPrefab;
            interaction.ForceInit();
            interaction.parentToReturnTo = targetBattlefield.transform;
            interaction.hasAttackedThisTurn = false;
        }
        else
        {
            // opponent configuration
            if (interaction != null) Destroy(interaction);
            if (bossInstance.GetComponent<CardInteractionOpponent>() == null) 
                bossInstance.AddComponent<CardInteractionOpponent>();
        }

        // Position and Layout
        RectTransform rect = bossInstance.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.localScale = Vector3.one;
            rect.localPosition = Vector3.zero;
            rect.anchoredPosition = Vector2.zero;
            rect.localRotation = Quaternion.Euler(isPlayerSide ? 0 : 25, 0, 0); 
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(targetBattlefield.GetComponent<RectTransform>());
        Debug.Log($"Success: {bossInstance.name} summoned onto layout lane: {laneName}!");
    }
}
