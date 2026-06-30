using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OpponentHP : MonoBehaviour
{
    public static float maxHP = 30;
    public static float staticHP = 30;
    public float hp;
    public Image Health;
    public TMP_Text hpText;

    void Start()
    {
        // Safety Shield
        if (Health == null) 
        {
            Debug.LogError($"[CRITICAL ERROR] OpponentHP on {gameObject.name} has no Health Image assigned! Please check the Inspector.");
        }

        if (staticHP <= 0 || staticHP > 30) 
        {
            staticHP = 30;
        }
        
        hp = staticHP;
        UpdateVisuals();
    }

    void Awake()
    {
        // If the game just started and we have less than max HP, 
        // it's probably a new game, not a continuation.
        if (staticHP != 30) 
        {
            staticHP = 30;
            hp = 30;
        }
    }

    // Overloaded to accept direct custom damage calls securely
    public void TakeDamage(float damageAmount)
    {
        TakeDamage(damageAmount, true);

        if (hp <= 0)
        {
            FindFirstObjectByType<GameManager>()?.EndGame(true);
        }
    }

    public void TakeDamage(float damageAmount, bool isDirectCombatAttack)
    {
        hp -= damageAmount;
        if (hp < 0) hp = 0;

        staticHP = hp;
        UpdateVisuals();

        Debug.Log($"[OPPONENT FACE] Took {damageAmount} damage! Current HP: {hp}");

        if (hp <= 0)
        {
            Debug.Log("Opponent Defeated! Triggering GameManager...");
            FindFirstObjectByType<GameManager>()?.EndGame(true);
        }

        // Only give Will points to the player if this was an active attack context action
        if (isDirectCombatAttack)
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
    }

    public void Heal(float healAmount)
    {
        Debug.Log($"[VAMPIRISM STEP 2] Inside OpponentHP class. Adding {healAmount} to current hp ({hp}).");

        hp += healAmount;
        if (hp > maxHP) hp = maxHP;
        staticHP = hp;

        Debug.Log($"[VAMPIRISM STEP 3] Math complete. New HP value: {hp}. Syncing UI text display fields...");
        UpdateVisuals();
    }

    public void UpdateVisuals()
    {
        // 1. Self-Healing logic for the Health Image
        if (Health == null)
        {
            // Try to find the image by a specific name (e.g., "OpponentHealthBar")
            Transform healthBarTransform = transform.Find("OpponentHealthBar"); 
            if (healthBarTransform != null) Health = healthBarTransform.GetComponent<Image>();
        }

        // 2. Self-Healing logic for the Text
        if (hpText == null)
        {
            Transform textTransform = transform.Find("OpponentHPText");
            if (textTransform != null) hpText = textTransform.GetComponent<TMP_Text>();
        }

        // 3. Update the UI
        if (Health != null) 
        {
            Health.fillAmount = (float)hp / (float)maxHP;
        }
        
        if (hpText != null) 
        {
            hpText.text = hp.ToString("F0") + "HP"; // "F0" ensures no decimal points
        }
    }
}