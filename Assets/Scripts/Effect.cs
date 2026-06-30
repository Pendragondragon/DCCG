using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Effect : MonoBehaviour
{
    public static Effect Instance;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void ResolveMonsterCombat(CardDisplay attacker, CardDisplay defender, bool isPlayerAttacking)
    {
        if (attacker == null || defender == null) return;

        // Base damage values from current attack stats
        int damageToDefender = attacker.currentAttack;
        int damageToAttacker = defender.currentAttack;

        // Track if a lethal strike is active
        bool attackerIsLethal = HasEffect(attacker, "Lethal");
        bool defenderIsLethal = HasEffect(defender, "Lethal");

        // ==========================================
        // STEP 1: CALCULATE POTENTIAL DAMAGE & LIFE-DRAIN
        // ==========================================
        // We use standard attack values to determine Vampirism life drain amounts
        int actualDamageDealtToDefender = Mathf.Max(0, Mathf.Min(damageToDefender, defender.currentVigor));
        int actualDamageDealtToAttacker = Mathf.Max(0, Mathf.Min(damageToAttacker, attacker.currentVigor));

        // Attacker Has Vampirism
        if (attacker.displayCard != null && HasEffect(attacker, "Vampirism") && actualDamageDealtToDefender > 0)
        {
            if (isPlayerAttacking)
                HealPlayerFace(actualDamageDealtToDefender);
            else
                HealOpponentFace(actualDamageDealtToDefender);
        }

        // Defender Has Vampirism (Retaliation Drain)
        if (defender.displayCard != null && HasEffect(defender, "Vampirism") && actualDamageDealtToAttacker > 0)
        {
            if (isPlayerAttacking)
                HealOpponentFace(actualDamageDealtToAttacker);
            else
                HealPlayerFace(actualDamageDealtToAttacker);
        }

        // ==========================================
        // STEP 2: LETHAL OVERRIDES & APPLY DAMAGE
        // ==========================================
        // If the attacker has Lethal and has more than 0 attack, it deals instantly fatal damage
        if (attackerIsLethal && damageToDefender > 0)
        {
            damageToDefender = defender.currentVigor;
            Debug.Log($"[LETHAL] {attacker.displayCard.name} fatal strike applied to {defender.displayCard.name}.");
        }

        // If the defender has Lethal and has more than 0 attack, its retaliation is instantly fatal
        if (defenderIsLethal && damageToAttacker > 0)
        {
            damageToAttacker = attacker.currentVigor;
            Debug.Log($"[LETHAL] {defender.displayCard.name} fatal retaliation applied to {attacker.displayCard.name}.");
        }

        // Apply final calculated damage amounts to both cards
        defender.TakeDamage(damageToDefender);
        attacker.TakeDamage(damageToAttacker);

        // Double check: If a lethal strike connected but armor/shields somehow left them with 1 HP, force drop them to 0
        if (attackerIsLethal && damageToDefender > 0 && defender.currentVigor > 0)
        {
            defender.currentVigor = 0;
            defender.RefreshCardUI(); // Assumes your CardDisplay has a UI refresh method
        }
        if (defenderIsLethal && damageToAttacker > 0 && attacker.currentVigor > 0)
        {
            attacker.currentVigor = 0;
            attacker.RefreshCardUI();
        }

        // ==========================================
        // STEP 3: WILL REWARD CALCULATIONS ON DESTRUCTION
        // ==========================================
        Will[] allWillPools = FindObjectsByType<Will>(FindObjectsSortMode.None);

        if (isPlayerAttacking)
        {
            // It's the Player's turn! If the enemy defender died, give the player 1 Will point.
            if (defender.currentVigor <= 0)
            {
                Debug.Log($"[REWARD] Player destroyed enemy creature {defender.gameObject.name}! Awarding 1 Will point.");
                foreach (Will pool in allWillPools)
                {
                    if (pool != null && pool.gameObject.name.IndexOf("Opponent", System.StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        pool.GainWill(1);
                        break;
                    }
                }
            }
        }
        else
        {
            // It's the Opponent's turn! If your player card died from the attack, give the opponent 1 Will point.
            if (attacker.currentVigor <= 0)
            {
                Debug.Log($"[REWARD] Opponent AI destroyed your creature {attacker.gameObject.name}! Awarding 1 Will point.");
                foreach (Will pool in allWillPools)
                {
                    if (pool != null && pool.gameObject.name.IndexOf("Opponent", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        pool.GainWill(1);
                        break;
                    }
                }
            }
        }
    }

    private bool HasEffect(CardDisplay card, string effectName)
    {
        if (card.displayCard == null || string.IsNullOrEmpty(card.displayCard.effect)) return false;
        
        // Checks if the effect string contains the keyword anywhere (handles multi-effect cards)
        return card.displayCard.effect.IndexOf(effectName, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void HealPlayerFace(int amount)
    {
        PlayerHP player = FindFirstObjectByType<PlayerHP>();
        if (player != null)
        {
            Debug.Log($"[VAMPIRISM STEP 1] Found PlayerHP script component. HP before heal execution: {player.hp} / {PlayerHP.maxHP}. Attacker dealt {amount} damage.");
            player.Heal(amount);
            Debug.Log($"[VAMPIRISM STEP 4] Heal method finished. Player HP is now: {player.hp} (Static tracker synced to: {PlayerHP.staticHP})");
        }
        else
        {
            Debug.LogError("[VAMPIRISM ERROR] Effect script failed to find the 'PlayerHP' script component in your scene hierarchy!");
        }
    }

    private void HealOpponentFace(int amount)
    {
        OpponentHP opponent = FindFirstObjectByType<OpponentHP>();
        if (opponent != null)
        {
            Debug.Log($"[VAMPIRISM STEP 1] Found OpponentHP script component. HP before heal execution: {opponent.hp}. Enemy dealt {amount} damage.");
            opponent.Heal(amount);
            Debug.Log($"[VAMPIRISM STEP 4] Heal method finished. Opponent HP is now: {opponent.hp}");
        }
        else
        {
            Debug.LogError("[VAMPIRISM ERROR] Effect script failed to find the 'OpponentHP' script component in your scene hierarchy!");
        }
    }
}