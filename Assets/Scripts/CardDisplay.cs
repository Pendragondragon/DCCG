using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

public class CardDisplay : MonoBehaviour
{
    //Needed Variables
    public Card displayCard;
    public int displayId;

    [Header("Text Features")]
    public TMP_Text nameText;
    public TMP_Text effectText;
    public TMP_Text essenceText;
    public TMP_Text attackText;
    public TMP_Text vigorText;

    [Header("Image Components")]
    public Image rarityImage;
    public Image artImage;
    public Image borderImage;
    
    [Header("Deck & Hand Settings")]
    public bool cardCover;
    public bool staticCardCover;
    public GameObject Hand;

    [Header("Live Combat Stats")]
    public int currentAttack;
    public int currentVigor;
    
    [Header("Status Conditions")]
    public bool isBuffed;
    public bool hasShadowveil; 

    [Header("Continuous Aura Stat Modifiers")]
    [HideInInspector] public int auraAttackBonus = 0;
    [HideInInspector] public int auraVigorBonus = 0;

    [Header("Turn Rules")]
    public bool hasSlumber = true;          
    public bool hasAttackedThisTurn = false; 
    public bool hasDash = false;

    private int lastWillGainFrame = -1;
    public int turnSummoned = -1;
    public int turnSpawned = -1;
    
    void Awake()
    {
        Hand = GameObject.Find("Hand");
    }

    void Start()
    {
        if (displayCard == null && CardDatabase.cardList != null && CardDatabase.cardList.Count > displayId)
        {
            displayCard = CardDatabase.cardList[displayId];
        }

        if (displayCard != null && currentVigor == 0 && currentAttack == 0)
        {
            currentAttack = displayCard.attack;
            currentVigor = displayCard.vigor;
        }

        RefreshCardUI();
    }

    void Update()
    {
        if (this.transform.parent != null)
        {
            string parentName = this.transform.parent.name;

            // what is revealed 
            if (parentName == "Hand" || 
                parentName == "BattlefieldPlayer" || 
                parentName == "BattlefieldOpponent" ||
                this.transform.parent.CompareTag("BattlefieldPlayer") ||
                this.transform.parent.CompareTag("BattlefieldOpponent"))
            {
                cardCover = false;
            }
            //what isnt revealed
            else if (parentName == "Deck" || parentName == "OpponentHand" || parentName == "OpponentDeck")
            {
                cardCover = true;
            }
        }
     
        staticCardCover = cardCover;
    }

    public void InitializeInjectedCard(Card targetedCardData)
    {
        displayCard = targetedCardData;
        
        if (displayCard != null)
        {
            currentAttack = displayCard.attack;
            currentVigor = displayCard.vigor;
        }

        RefreshCardUI();
    }

    //Buffs
    public void RecalculateAuraBonuses()
    {
        auraAttackBonus = 0;
        auraVigorBonus = 0;

        if (transform.parent == null) return;
        
        Transform parent = transform.parent;
        // Ensure we are only calculating for cards on the battlefield
        if (parent.name != "BattlefieldPlayer" && parent.name != "BattlefieldOpponent" &&
            !parent.CompareTag("BattlefieldPlayer") && !parent.CompareTag("BattlefieldOpponent"))
        {
            return;
        }

        foreach (Transform sibling in parent)
        {
            if (sibling.gameObject == this.gameObject) continue;

            CardDisplay siblingDisplay = sibling.GetComponent<CardDisplay>();
            if (siblingDisplay != null && siblingDisplay.displayCard != null)
            {
                string effect = siblingDisplay.displayCard.effect ?? "";
                string name = siblingDisplay.displayCard.name ?? "";

                // Logic for Sir Lancelot: +1/+1
                if (name.Contains("Sir Lancelot"))
                {
                    auraAttackBonus += 1;
                    auraVigorBonus += 1;
                }
                // Logic for Tavern Bard: +0/+1
                else if (name.Contains("Tavern Bard"))
                {
                    auraVigorBonus += 1;
                }
                // Existing logic for other effects
                else if (effect.Contains("Other monsters you control get +1/+1"))
                {
                    auraAttackBonus += 1;
                    auraVigorBonus += 1;
                }
                else if (effect.Contains("Other monsters you control get +0/+1"))
                {
                    auraVigorBonus += 1;
                }
            }
        }
    }

    public void RefreshAllBattlefieldCards()
    {
        string[] battlefields = { "BattlefieldPlayer", "BattlefieldOpponent" };
        foreach (string fieldName in battlefields)
        {
            GameObject field = GameObject.Find(fieldName);
            if (field == null) field = GameObject.FindWithTag(fieldName);

            if (field != null)
            {
                foreach (Transform child in field.transform)
                {
                    CardDisplay card = child.GetComponent<CardDisplay>();
                    if (card != null)
                    {
                        card.RefreshCardUI();
                    }
                }
            }
        }
    }

    //Trigger for Merlin
    public void TriggerMerlinEndTurnEffect(bool isPlayerSide)
    {
        List<object> validTargets = new List<object>();

        if (isPlayerSide)
        {
            GameObject enemyBattlefield = GameObject.FindWithTag("BattlefieldOpponent");
            if (enemyBattlefield == null) enemyBattlefield = GameObject.Find("BattlefieldOpponent");

            if (enemyBattlefield != null)
            {
                foreach (Transform child in enemyBattlefield.transform)
                {
                    CardDisplay monster = child.GetComponent<CardDisplay>();
                    if (monster != null) validTargets.Add(monster);
                }
            }
            
            OpponentHP opponentFace = FindFirstObjectByType<OpponentHP>();
            if (opponentFace != null) validTargets.Add(opponentFace);
        }
        else
        {
            GameObject playerBattlefield = GameObject.FindWithTag("BattlefieldPlayer");
            if (playerBattlefield == null) playerBattlefield = GameObject.Find("BattlefieldPlayer");

            if (playerBattlefield != null)
            {
                foreach (Transform child in playerBattlefield.transform)
                {
                    CardDisplay monster = child.GetComponent<CardDisplay>();
                    if (monster != null) validTargets.Add(monster);
                }
            }

            PlayerHP playerFace = FindFirstObjectByType<PlayerHP>();
            if (playerFace != null) validTargets.Add(playerFace);
        }

        if (validTargets.Count > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, validTargets.Count);
            object chosenFoe = validTargets[randomIndex];

            if (chosenFoe is CardDisplay monster)
            {
                Debug.Log($"Merlin: Magic spell deals 4 damage to monster: {monster.gameObject.name}");
                // No damageSource provided: won't trigger Will points
                monster.TakeDamage(4, null); 
            }
            else if (chosenFoe is OpponentHP opponentFace)
            {
                opponentFace.TakeDamage(4);
            }
            else if (chosenFoe is PlayerHP playerFace)
            {
                playerFace.TakeDamage(4);
            }
        }
    }

    //Trigger for Mutated Hunter
    public void TriggerMutatedHunterGrowth()
    {
        currentAttack += 1;
        currentVigor += 1;
        isBuffed = true; 
        RefreshCardUI();
    }

    //Trigger for Pan Piper
    public void TriggerPanPiperSummon(bool isPlayerSide)
    {
        GameObject battlefield = isPlayerSide ? GameObject.Find("BattlefieldPlayer") : GameObject.Find("BattlefieldOpponent");
        if (battlefield == null && isPlayerSide) battlefield = GameObject.FindWithTag("BattlefieldPlayer");
        if (battlefield == null && !isPlayerSide) battlefield = GameObject.FindWithTag("BattlefieldOpponent");

        if (battlefield == null) return;
        if (battlefield.transform.childCount >= 7) return;

        ScriptableObject ratSO = null;
        if (CardDatabase.cardList != null)
        {
            foreach (var card in CardDatabase.cardList)
            {
                if (card != null && card.name.Equals("Rat", System.StringComparison.OrdinalIgnoreCase))
                {
                    ratSO = card;
                    break;
                }
            }
        }

        if (ratSO == null) return;

        CardInteraction creatorInteraction = GetComponent<CardInteraction>();
        if (creatorInteraction == null || creatorInteraction.cardPrefab == null) return;

        GameObject tokenCard = Instantiate(creatorInteraction.cardPrefab, battlefield.transform);
        tokenCard.name = "Rat_Token";

        CanvasGroup cg = tokenCard.GetComponent<CanvasGroup>();
        if (cg != null) { cg.interactable = true; cg.blocksRaycasts = true; }

        Attack oldAttackComp = tokenCard.GetComponent<Attack>();
        if (oldAttackComp != null) Destroy(oldAttackComp);

        CardDisplay tokenDisplay = tokenCard.GetComponent<CardDisplay>();
        if (tokenDisplay != null)
        {
            tokenDisplay.displayCard = (Card)ratSO;
            tokenDisplay.currentAttack = tokenDisplay.displayCard.attack;
            tokenDisplay.currentVigor = tokenDisplay.displayCard.vigor;
            tokenDisplay.cardCover = false; 
            tokenDisplay.RefreshCardUI();
        }

        CardInteraction tokenInteraction = tokenCard.GetComponent<CardInteraction>();
        if (tokenInteraction == null) tokenInteraction = tokenCard.AddComponent<CardInteraction>();
        if (tokenInteraction != null)
        {
            tokenInteraction.linePrefab = creatorInteraction.linePrefab;
            tokenInteraction.cardPrefab = creatorInteraction.cardPrefab;
            tokenInteraction.Mastiff = creatorInteraction.Mastiff;
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

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(battlefield.GetComponent<RectTransform>());
        RefreshAllBattlefieldCards();
    }

    //Trigger for Lyra
    public void TriggerLyraDamageEffect(bool isPlayerSide)
    {
        if (isPlayerSide)
        {
            OpponentHP opponentFace = FindFirstObjectByType<OpponentHP>();
            if (opponentFace != null) opponentFace.TakeDamage(2);
        }
        else
        {
            PlayerHP playerFace = FindFirstObjectByType<PlayerHP>();
            if (playerFace != null) playerFace.TakeDamage(2);
        }
    }

    //Trigger for Bloodlust
    public void TriggerBloodlust()
    {
        currentAttack += 1;
        isBuffed = true; 
        RefreshCardUI();
    }

    public void RefreshCardUI()
    {
        if (displayCard == null) return;

        RecalculateAuraBonuses();

        int finalUIDisplayedAttack = currentAttack + auraAttackBonus;
        int finalUIDisplayedVigor = currentVigor + auraVigorBonus;

        if (nameText != null) nameText.text = displayCard.name;
        if (effectText != null) effectText.text = displayCard.effect;
        if (essenceText != null) essenceText.text = displayCard.essence.ToString();
        
        if (attackText != null) attackText.text = finalUIDisplayedAttack.ToString();
        if (vigorText != null) vigorText.text = finalUIDisplayedVigor.ToString();
        
        Color blessedGreen = new Color(0.2f, 0.85f, 0.3f); 

        //Defining colors: greens as buff, red as wound
        if (attackText != null)
        {
            if (isBuffed || auraAttackBonus > 0 || finalUIDisplayedAttack > displayCard.attack)
                attackText.color = blessedGreen;
            else
                attackText.color = Color.white;
        }
        
        if (vigorText != null)
        {
            int baseMaxVigorWithAura = displayCard.vigor + auraVigorBonus;
            if (finalUIDisplayedVigor > baseMaxVigorWithAura || isBuffed)
                vigorText.color = blessedGreen;
            else if (finalUIDisplayedVigor < baseMaxVigorWithAura)
                vigorText.color = Color.red; 
            else
                vigorText.color = Color.white;
        }

        if (artImage != null) artImage.sprite = displayCard.art;
        if (borderImage != null) borderImage.sprite = displayCard.border;
        if (rarityImage != null) rarityImage.sprite = displayCard.rarity;

        if (displayCard != null && !string.IsNullOrEmpty(displayCard.effect) && displayCard.effect.Contains("Dash"))
        {
            hasDash = true;
        }

        //Effect for Shadowveil
        if (hasShadowveil)
        {
            if (artImage != null) artImage.color = new Color(0.4f, 0.3f, 0.6f, 0.6f);
            if (borderImage != null) borderImage.color = new Color(0.4f, 0.3f, 0.6f, 0.8f);
            if (rarityImage != null) rarityImage.color = Color.white; 
        }
        else
        {
            if (artImage != null) artImage.color = Color.white;
            if (borderImage != null) borderImage.color = Color.white;
            if (rarityImage != null) rarityImage.color = Color.white;
        }
    }

    //Taking damage
    public void TakeDamage(int damageAmount, CardDisplay damageSource = null)
    {
        if (damageSource != null && damageSource.displayCard != null && !string.IsNullOrEmpty(damageSource.displayCard.effect))
        {
            if (damageSource.displayCard.effect.IndexOf("lethal", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                damageAmount = currentVigor + auraVigorBonus; 
                Debug.Log($"Lethal: {damageSource.displayCard.name} triggered an instant-kill blow on {displayCard.name}!");
            }
        }

        currentVigor -= damageAmount;
        Debug.Log($"Damage: {displayCard.name} took {damageAmount} damage. Core Health Remaining: {currentVigor}");
        
        RefreshCardUI();

        // Check if this hit is fatal
        if (currentVigor <= 0)
        {
            DestroyMonster(damageSource); 
        }
    }

    // Associating a destroyed creature to gain of will points
    private void AwardCombatKillWill(CardDisplay attacker)
    {
        bool attackerIsPlayer = false;
        if (attacker.transform.parent != null)
        {
            string pName = attacker.transform.parent.name;
            if (pName == "BattlefieldPlayer" || attacker.transform.parent.CompareTag("BattlefieldPlayer"))
            {
                attackerIsPlayer = true;
            }
        }

        Will[] allWillPools = FindObjectsByType<Will>(FindObjectsSortMode.None);
        foreach (Will pool in allWillPools)
        {
            if (pool == null) continue;
            bool isOpponentPool = pool.gameObject.name.Contains("Opponent");

            if (attackerIsPlayer && !isOpponentPool)
            {
                lastWillGainFrame = Time.frameCount;
                pool.GainWill(1);
                break;
            }
            else if (!attackerIsPlayer && isOpponentPool)
            {
                lastWillGainFrame = Time.frameCount;
                pool.GainWill(1);
                break;
            }
        }
    }

    public void ModifyAttack(int amount)
    {
        currentAttack += amount;
        if (currentAttack < 0) currentAttack = 0; 
        RefreshCardUI();
    }

    private void DestroyMonster(CardDisplay attacker = null)
    {
        bool belongsToPlayerField = false;

        if (transform.parent != null)
        {
            if (transform.parent.name == "BattlefieldPlayer" || transform.parent.CompareTag("BattlefieldPlayer"))
            {
                belongsToPlayerField = true;
            }
        }

        //Effect for Contrabandist
        if (displayCard != null && !string.IsNullOrEmpty(displayCard.effect))
        {
            if (displayCard.name.ToLower().Contains("contrabandist") || 
                displayCard.effect.Contains("enters and dies draw a card"))
            {
                if (belongsToPlayerField)
                {
                    PlayerDeck playerDeckScript = FindFirstObjectByType<PlayerDeck>();
                    if (playerDeckScript != null) playerDeckScript.DrawSingleCardDirectly();
                }
            }
        }

        //Effect for Lyra
        if (displayCard != null)
        {
            string currentEffect = displayCard.effect;
            if (!string.IsNullOrEmpty(currentEffect) && 
                (displayCard.name.ToLower().Contains("lyra") || 
                currentEffect.ToLower().Contains("2 damage") && currentEffect.ToLower().Contains("enters or dies")))
            {
                TriggerLyraDamageEffect(belongsToPlayerField);
            }
        }

        //Effect for Imp
        if (displayCard != null)
        {
            string currentEffect = displayCard.effect;
            if (!string.IsNullOrEmpty(currentEffect) && 
                (displayCard.name.ToLower().Contains("imp") || 
                currentEffect.ToLower().Contains("1 damage to all") || currentEffect.ToLower().Contains("dies deals 1 damage to all")))
            {
                PlayerHP playerFace = FindFirstObjectByType<PlayerHP>();
                if (playerFace != null) playerFace.TakeDamage(1);

                OpponentHP opponentFace = FindFirstObjectByType<OpponentHP>();
                if (opponentFace != null) opponentFace.TakeDamage(1);

                GameObject playerField = GameObject.FindWithTag("BattlefieldPlayer") ?? GameObject.Find("BattlefieldPlayer");
                if (playerField != null)
                {
                    CardDisplay[] playerMonsters = playerField.GetComponentsInChildren<CardDisplay>();
                    foreach (CardDisplay monster in playerMonsters)
                    {
                        if (monster != null && monster.gameObject != this.gameObject) monster.TakeDamage(1, null);
                    }
                }

                GameObject opponentField = GameObject.FindWithTag("BattlefieldOpponent") ?? GameObject.Find("BattlefieldOpponent");
                if (opponentField != null)
                {
                    CardDisplay[] opponentMonsters = opponentField.GetComponentsInChildren<CardDisplay>();
                    foreach (CardDisplay monster in opponentMonsters)
                    {
                        if (monster != null && monster.gameObject != this.gameObject) monster.TakeDamage(1, null);
                    }
                }
            }
        }

        //Effect for Bloodlust
        if (displayCard != null)
        {
            GameObject playerField = GameObject.FindWithTag("BattlefieldPlayer") ?? GameObject.Find("BattlefieldPlayer");
            if (playerField != null)
            {
                foreach (Transform child in playerField.transform)
                {
                    CardDisplay monster = child.GetComponent<CardDisplay>();
                    if (monster != null && monster.gameObject != this.gameObject && monster.displayCard != null)
                    {
                        string effectLower = monster.displayCard.effect != null ? monster.displayCard.effect.ToLower() : "";
                        if (monster.displayCard.name.ToLower().Contains("bloodlust") || effectLower.Contains("bloodlust"))
                        {
                            monster.TriggerBloodlust();
                        }
                    }
                }
            }

            GameObject opponentField = GameObject.FindWithTag("BattlefieldOpponent") ?? GameObject.Find("BattlefieldOpponent");
            if (opponentField != null)
            {
                foreach (Transform child in opponentField.transform)
                {
                    CardDisplay monster = child.GetComponent<CardDisplay>();
                    if (monster != null && monster.gameObject != this.gameObject && monster.displayCard != null)
                    {
                        string effectLower = monster.displayCard.effect != null ? monster.displayCard.effect.ToLower() : "";
                        if (monster.displayCard.name.ToLower().Contains("bloodlust") || effectLower.Contains("bloodlust"))
                        {
                            monster.TriggerBloodlust();
                        }
                    }
                }
            }
            
        }

        if (attacker != null)
        {
            AwardCombatKillWill(attacker);
        }

        transform.SetParent(null);
        RefreshAllBattlefieldCards();
        Destroy(gameObject);
    }

    //Resets flags by turn manager at the end of turn
    public void ResetTurnRestrictions()
    {
        hasSlumber = false;           
        hasAttackedThisTurn = false;

        CardInteraction interaction = GetComponent<CardInteraction>();
        if (interaction != null)
        {
            interaction.hasAttackedThisTurn = false;
        }
    }
}
