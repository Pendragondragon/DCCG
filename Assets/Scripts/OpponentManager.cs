using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OpponentManager : MonoBehaviour
{
    private TurnSystem turnSystem;
    private GameObject opponentHand;
    private GameObject opponentBattlefield;
    private GameObject playerBattlefield;
    private Will opponentWillPool;

    [Header("Token Configuration for AI Spawning")]
    public GameObject cardPrefab;      
    public ScriptableObject ratSO;     
    public ScriptableObject mastiffSO; 
    public ScriptableObject skeletonSO; 
    
    // ADDED: Define your missing variable here
    public int spawnCost = 7; 

    [Header("Opponent's Turn Rules Tracking")]
    private HashSet<int> monstersThatAttackedThisTurn = new HashSet<int>();
    private Dictionary<int, int> monsterBirthTurns = new Dictionary<int, int>();
    private HashSet<int> monstersWithSlumber = new HashSet<int>();

    void Start()
    {
        turnSystem = FindFirstObjectByType<TurnSystem>();
        FindCrucialFields();
    }

    private void FindCrucialFields()
    {
        if (opponentHand == null) opponentHand = GameObject.Find("OpponentHand");
        if (opponentBattlefield == null) opponentBattlefield = GameObject.Find("BattlefieldOpponent");
        if (playerBattlefield == null) playerBattlefield = GameObject.Find("BattlefieldPlayer");
        
        if (opponentWillPool == null)
        {
            GameObject oppGO = GameObject.Find("Opponent") ?? GameObject.Find("OpponentWill");
            if (oppGO != null) opponentWillPool = oppGO.GetComponentInChildren<Will>();
        }
    }

    public void StartOpponentTurn()
    {
        StartCoroutine(ExecuteTurnRoutine());
    }

    private IEnumerator ExecuteTurnRoutine()
    {
        yield return new WaitForSeconds(1.0f); 
        FindCrucialFields(); 

        if (turnSystem != null) turnSystem.UpdateUIElements();

        monstersThatAttackedThisTurn.Clear();
        monstersWithSlumber.Clear(); 

        // PHASE 0: AVATAR REINCARNATION
        Will oppWill = FindFirstObjectByType<Will>(); 

        if (oppWill != null && oppWill.SpendWill(spawnCost))
        {
            AvatarReincarnationButton aiReincarnation = FindFirstObjectByType<AvatarReincarnationButton>();
            if (aiReincarnation != null)
            {
                aiReincarnation.TryExecuteReincarnationForAI();
            }
        }

        // PHASE 1: PLAY CARDS FROM HAND
        bool playedACard = true;
        int loopSanityCheck = 0; 
        
        while (playedACard && loopSanityCheck < 10)
        {
            playedACard = false;
            loopSanityCheck++;

            if (opponentHand == null || (opponentBattlefield != null && opponentBattlefield.transform.childCount >= 7)) break;

            LayoutRebuilder.ForceRebuildLayoutImmediate(opponentHand.GetComponent<RectTransform>());
            CardDisplay[] cardsInHand = opponentHand.GetComponentsInChildren<CardDisplay>();
            if (cardsInHand.Length == 0) break;

            CardDisplay cardToPlay = null;

            foreach (CardDisplay card in cardsInHand)
            {
                if (card != null && card.displayCard != null)
                {
                    int cost = card.displayCard.essence;
                    if (turnSystem.opponentEssenceCurrent >= cost)
                    {
                        if (cardToPlay == null || cost > cardToPlay.displayCard.essence) cardToPlay = card;
                    }
                }
            }

            if (cardToPlay != null)
            {
                turnSystem.SpendEssence(cardToPlay.displayCard.essence);
                if (opponentBattlefield != null) cardToPlay.transform.SetParent(opponentBattlefield.transform);

                CardCover cover = cardToPlay.GetComponent<CardCover>();
                if (cover != null)
                {
                    cover.isCoverActive = false; 
                    cover.UpdateCoverState();
                }
                
                if (cardToPlay.displayCard != null)
                {
                    string aiPlayedName = cardToPlay.displayCard.name.ToLower();
                    if (aiPlayedName.Contains("lancelot") || aiPlayedName.Contains("griffin"))
                    {
                        cardToPlay.hasDash = true;
                    }
                }

                cardToPlay.RefreshCardUI();

                int cardInstanceID = cardToPlay.gameObject.GetInstanceID();
                if (!monsterBirthTurns.ContainsKey(cardInstanceID)) monsterBirthTurns.Add(cardInstanceID, turnSystem.opponentTurn);
                monstersWithSlumber.Add(cardInstanceID);

                CardInteraction playerInteractionComp = cardToPlay.GetComponent<CardInteraction>();
                if (playerInteractionComp != null) Destroy(playerInteractionComp);

                if (cardToPlay.GetComponent<CardInteractionOpponent>() == null) cardToPlay.gameObject.AddComponent<CardInteractionOpponent>();
                
                RectTransform cardRect = cardToPlay.GetComponent<RectTransform>();
                if (cardRect != null)
                {
                    cardRect.localScale = Vector3.one;
                    cardRect.localPosition = Vector3.zero;
                    cardRect.anchoredPosition = Vector2.zero;
                    cardRect.localRotation = Quaternion.Euler(25, 0, 0);
                }

                ExecuteOpponentETBEffects(cardToPlay);
                if (opponentBattlefield != null) LayoutRebuilder.ForceRebuildLayoutImmediate(opponentBattlefield.GetComponent<RectTransform>());
                
                playedACard = true; 
                yield return new WaitForSeconds(1.2f); 
            }
        }

        // PHASE 1.5: AVATAR POWER
        AvatarPower aiPower = null;
        GameObject opponentObj = GameObject.Find("Opponent") ?? GameObject.Find("OpponentWill") ?? GameObject.Find("OpponentAvatar");
        if (opponentObj != null)
        {
            aiPower = opponentObj.GetComponentInChildren<AvatarPower>();
        }
        else
        {
            AvatarPower[] allPowers = FindObjectsByType<AvatarPower>(FindObjectsSortMode.None);
            foreach (var power in allPowers)
            {
                if (power != null && power.gameObject.transform.root.name != "Player" && power.gameObject.name.IndexOf("Player", System.StringComparison.OrdinalIgnoreCase) < 0) 
                {
                    aiPower = power;
                    break;
                }
            }
        }

        if (aiPower != null && !aiPower.hasBeenUsedThisTurn && turnSystem.opponentEssenceCurrent >= aiPower.essenceCost)
        {
            GameObject target = null;
            if (playerBattlefield != null && playerBattlefield.transform.childCount > 0)
            {
                CardDisplay[] targets = playerBattlefield.GetComponentsInChildren<CardDisplay>();
                foreach (var c in targets) if (c != null && !c.hasShadowveil) { target = c.gameObject; break; }
            }
            if (target == null)
            {
                PlayerHP face = FindFirstObjectByType<PlayerHP>();
                if (face != null) target = face.gameObject;
            }

            if (target != null)
            {
                turnSystem.SpendEssence(aiPower.essenceCost);
                aiPower.hasBeenUsedThisTurn = true;
                if (target.GetComponent<CardDisplay>() != null) target.GetComponent<CardDisplay>().TakeDamage(aiPower.damageAmount);
                else if (target.GetComponent<PlayerHP>() != null) target.GetComponent<PlayerHP>().TakeDamage(aiPower.damageAmount, false); 
                yield return new WaitForSeconds(1.0f);
            }
        }

        // PHASE 2: ATTACK SELECTION
        if (opponentBattlefield != null)
        {
            CardDisplay[] opponentAttackers = opponentBattlefield.GetComponentsInChildren<CardDisplay>();

            foreach (CardDisplay attacker in opponentAttackers)
            {
                if (attacker == null || attacker.currentAttack <= 0) continue;

                int attackerID = attacker.gameObject.GetInstanceID();
                if (monsterBirthTurns.ContainsKey(attackerID) && monsterBirthTurns[attackerID] == turnSystem.opponentTurn && !attacker.hasDash) continue;
                if (monstersThatAttackedThisTurn.Contains(attackerID)) continue;

                monstersThatAttackedThisTurn.Add(attackerID);

                CardDisplay provokeDefender = GetActivePlayerProvokeDefender();
                bool playerHasProvokeActive = (provokeDefender != null);

                if (attacker.hasShadowveil)
                {
                    attacker.hasShadowveil = false;
                    attacker.RefreshCardUI();
                }

                if (attacker.displayCard != null)
                {
                    string effectText = attacker.displayCard.effect;
                    string cardNameLower = attacker.displayCard.name.ToLower();

                    if (cardNameLower.Contains("djinn") || (!string.IsNullOrEmpty(effectText) && effectText.Contains("on attack it deals 2 damage"))) TriggerOpponentDjinnAttackStrike();
                    if (cardNameLower.Contains("drunken monk") || (!string.IsNullOrEmpty(effectText) && effectText.Contains("on attack it deals 3 damage"))) TriggerOpponentDrunkenMonkAttackStrike();
                    if (!string.IsNullOrEmpty(effectText))
                    {
                        if (effectText.Contains("Thunder")) TriggerOpponentThunderEffect();
                        if (effectText.IndexOf("Corrosive", System.StringComparison.OrdinalIgnoreCase) >= 0) TriggerOpponentCorrosiveEffect();
                    }
                }

                CardDisplay targetDefender = null;
                if (playerHasProvokeActive) targetDefender = provokeDefender;
                else if (playerBattlefield != null)
                {
                    CardDisplay[] potentialDefenders = playerBattlefield.GetComponentsInChildren<CardDisplay>();
                    foreach (CardDisplay defender in potentialDefenders) if (defender != null && !defender.hasShadowveil) { targetDefender = defender; break; }
                }

                if (targetDefender != null)
                {
                    if (Effect.Instance != null) Effect.Instance.ResolveMonsterCombat(attacker, targetDefender, false);
                    else
                    {
                        targetDefender.TakeDamage(attacker.currentAttack);
                        attacker.TakeDamage(targetDefender.currentAttack);
                    }
                }
                else
                {
                    PlayerHP playerFace = FindFirstObjectByType<PlayerHP>();
                    if (playerFace != null) playerFace.TakeDamage(attacker.currentAttack, true);
                }
                yield return new WaitForSeconds(1.0f); 
            }
        }

        yield return new WaitForSeconds(0.8f);
        turnSystem.EndOpponentTurn();
    }

    private CardDisplay GetActivePlayerProvokeDefender()
    {
        FindCrucialFields();
        if (playerBattlefield == null) return null;

        CardDisplay[] playerMonsters = playerBattlefield.GetComponentsInChildren<CardDisplay>();
        foreach (CardDisplay monster in playerMonsters)
        {
            if (monster != null && !monster.hasShadowveil && monster.displayCard != null && !string.IsNullOrEmpty(monster.displayCard.effect))
            {
                if (monster.displayCard.effect.IndexOf("Provoke", System.StringComparison.OrdinalIgnoreCase) >= 0) return monster;
            }
        }
        return null;
    }

    private void ExecuteOpponentETBEffects(CardDisplay cardDisplay)
    {
        if (cardDisplay.displayCard == null || string.IsNullOrEmpty(cardDisplay.displayCard.effect)) return;

        string currentEffect = cardDisplay.displayCard.effect;
        
        // Fix: Add logic to process drawing for the Opponent
        if (currentEffect.Contains("Draw 2 cards")) 
        {
            OpponentDeck od = FindFirstObjectByType<OpponentDeck>();
            if (od != null) { od.DrawSingleCardDirectly(); od.DrawSingleCardDirectly(); }
        }
        else if (currentEffect.Contains("Draw a card"))
        {
            OpponentDeck od = FindFirstObjectByType<OpponentDeck>();
            if (od != null) od.DrawSingleCardDirectly();
        }

        if (currentEffect.Contains("Heal 2")) FindFirstObjectByType<OpponentHP>()?.Heal(2);
        if (currentEffect.Contains("Heal 1")) FindFirstObjectByType<OpponentHP>()?.Heal(1);
        if (currentEffect.Contains("Deals 2 damage")) FindFirstObjectByType<PlayerHP>()?.TakeDamage(2, false); 
        if (currentEffect.Contains("Deals 3 damage")) FindFirstObjectByType<PlayerHP>()?.TakeDamage(3, false);
        if (currentEffect.Contains("Thunder")) TriggerOpponentThunderEffect();
    }

    public void SpawnTokensForAI(ScriptableObject tokenSO, int amount)
    {
        FindCrucialFields();
        if (opponentBattlefield == null || cardPrefab == null || tokenSO == null) return;

        for (int i = 0; i < amount; i++)
        {
            if (opponentBattlefield.transform.childCount >= 7) break;

            GameObject tokenInstance = Instantiate(cardPrefab, opponentBattlefield.transform);
            CardDisplay tokenDisplay = tokenInstance.GetComponent<CardDisplay>();
            
            if (tokenDisplay != null)
            {
                tokenDisplay.displayCard = (Card)tokenSO;
                
                if (CardDatabase.cardList != null)
                {
                    for (int x = 0; x < CardDatabase.cardList.Count; x++)
                    {
                        if (CardDatabase.cardList[x] != null && CardDatabase.cardList[x].name.Equals(tokenSO.name, System.StringComparison.OrdinalIgnoreCase))
                        {
                            tokenDisplay.displayId = x;
                            break;
                        }
                    }
                }

                tokenDisplay.currentAttack = tokenDisplay.displayCard.attack;
                tokenDisplay.currentVigor = tokenDisplay.displayCard.vigor;
                tokenDisplay.cardCover = false;
                tokenDisplay.hasSlumber = true;
                tokenDisplay.RefreshCardUI();
            }

            CardInteraction pInt = tokenInstance.GetComponent<CardInteraction>();
            if (pInt != null) Destroy(pInt);

            if (tokenInstance.GetComponent<CardInteractionOpponent>() == null) tokenInstance.AddComponent<CardInteractionOpponent>();

            int tokenID = tokenInstance.GetInstanceID();
            if (!monsterBirthTurns.ContainsKey(tokenID)) monsterBirthTurns.Add(tokenID, turnSystem.opponentTurn);
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(opponentBattlefield.GetComponent<RectTransform>());
    }

    public void TriggerZephyrEffectForAI()
    {
        GameObject battlefield = GameObject.Find("BattlefieldOpponent");
        if (battlefield == null) return;

        foreach (Transform child in battlefield.transform)
        {
            CardDisplay cd = child.GetComponent<CardDisplay>();
            if (cd != null && cd.displayCard != null && cd.displayCard.name.Contains("Zephyr"))
            {
                // Calculate how many more skeletons we can fit
                int currentCount = battlefield.transform.childCount;
                int slotsRemaining = 7 - currentCount;

                if (slotsRemaining > 0 && skeletonSO != null)
                {
                    // Spawn exactly the number of tokens needed to hit 7
                    SpawnTokensForAI(skeletonSO, slotsRemaining);
                }
            }
        }
    }

    private void TriggerOpponentDjinnAttackStrike()
    {
        List<object> validTargets = new List<object>();
        FindCrucialFields();
        if (playerBattlefield != null)
        {
            CardDisplay[] playerMonsters = playerBattlefield.GetComponentsInChildren<CardDisplay>();
            foreach (var m in playerMonsters) if (m != null) validTargets.Add(m);
        }
        PlayerHP playerFace = FindFirstObjectByType<PlayerHP>();
        if (playerFace != null) validTargets.Add(playerFace);
        if (validTargets.Count == 0) return;

        object chosen = validTargets[Random.Range(0, validTargets.Count)];
        if (chosen is CardDisplay monster) monster.TakeDamage(2);
        else if (chosen is PlayerHP face) face.TakeDamage(2, false);
    }

    private void TriggerOpponentDrunkenMonkAttackStrike()
    {
        List<object> validTargets = new List<object>();
        FindCrucialFields();
        if (playerBattlefield != null)
        {
            CardDisplay[] playerMonsters = playerBattlefield.GetComponentsInChildren<CardDisplay>();
            foreach (var m in playerMonsters) if (m != null) validTargets.Add(m);
        }
        PlayerHP playerFace = FindFirstObjectByType<PlayerHP>();
        if (playerFace != null) validTargets.Add(playerFace);
        if (validTargets.Count == 0) return;

        object chosen = validTargets[Random.Range(0, validTargets.Count)];
        if (chosen is CardDisplay monster) monster.TakeDamage(3);
        else if (chosen is PlayerHP face) face.TakeDamage(3, false);
    }

    private void TriggerOpponentThunderEffect()
    {
        if (playerBattlefield != null)
        {
            CardDisplay[] playerMonsters = playerBattlefield.GetComponentsInChildren<CardDisplay>();
            foreach (CardDisplay monster in playerMonsters) if (monster != null) monster.TakeDamage(1);
        }
        FindFirstObjectByType<PlayerHP>()?.TakeDamage(1, false);
    }

    private void TriggerOpponentCorrosiveEffect()
    {
        List<object> validTargets = new List<object>();
        FindCrucialFields();
        if (playerBattlefield != null)
        {
            CardDisplay[] playerMonsters = playerBattlefield.GetComponentsInChildren<CardDisplay>();
            foreach (var monster in playerMonsters) if (monster != null) validTargets.Add(monster);
        }
        PlayerHP playerFace = FindFirstObjectByType<PlayerHP>();
        if (playerFace != null) validTargets.Add(playerFace);

        if (validTargets.Count == 0) return;

        for (int i = 0; i < 2; i++)
        {
            if (validTargets.Count == 0) break;
            object chosen = validTargets[Random.Range(0, validTargets.Count)];
            if (chosen is CardDisplay monster) monster.TakeDamage(2);
            else if (chosen is PlayerHP face) face.TakeDamage(2, false);
        }
    }

    public void ResetTurnAttackRestrictions()
    {
        if (monstersThatAttackedThisTurn != null) monstersThatAttackedThisTurn.Clear();
    }
}