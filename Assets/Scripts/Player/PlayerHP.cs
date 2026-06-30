using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHP : MonoBehaviour
{
    public static float maxHP = 30;
    public static float staticHP = 30;
    public float hp;
    public Image Health; 
    public TMP_Text hpText;

    void Start()
    {
        if (staticHP <= 0) 
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
            FindFirstObjectByType<GameManager>()?.EndGame(false);
        }
    }

    public void TakeDamage(float damageAmount, bool isDirectCombatAttack)
    {
        hp -= damageAmount;
        if (hp < 0) hp = 0;

        staticHP = hp;
        UpdateVisuals();

        Debug.Log($"[PLAYER FACE] Took {damageAmount} damage! Current HP: {hp}");

        if (hp <= 0)
        {
            Debug.Log("Player Defeated! Triggering GameManager...");
            FindFirstObjectByType<GameManager>()?.EndGame(false);
        }

        // Only give Will points to the Opponent if this damage was delivered during active combat unit actions
        if (isDirectCombatAttack)
        {
            Will[] allWillPools = FindObjectsByType<Will>(FindObjectsSortMode.None);
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

    public void Heal(float healAmount)
    {
        float oldHP = hp;
        hp += healAmount;
        if (hp > maxHP) hp = maxHP;
        staticHP = hp;
        
        // Debug syncing logs preserved from your source configuration file
        string objectPath = gameObject.name;
        Transform currentParent = transform.parent;
        while (currentParent != null)
        {
            objectPath = currentParent.name + "/" + objectPath;
            currentParent = currentParent.parent;
        }

        Debug.LogWarning($"====== [UI CEILING SYNCHRONIZATION CHECK] ======\n" +
                         $"Object Path: {objectPath}\n" +
                         $"Object Tag: {gameObject.tag}\n" +
                         $"Old HP: {oldHP} -> New HP: {hp}\n" +
                         $"Image Connected? {(Health != null ? "YES" : "NO")}\n" +
                         $"================================================");

        UpdateVisuals();
    }

    public void UpdateVisuals()
    {
        // If the link is lost, try to re-find the object by name/tag
        if (Health == null) 
        {
            GameObject healthBar = GameObject.Find("PlayerHealthBar"); // Use your actual object name
            if (healthBar != null) Health = healthBar.GetComponent<Image>();
        }
        
        if (Health != null) 
        {
            float targetFill = (float)hp / (float)maxHP;
            Health.fillAmount = targetFill; 
        }
        
        // Do the same for text
        if (hpText == null)
        {
            // Try to re-find if lost
            hpText = GameObject.Find("PlayerHPText")?.GetComponent<TMP_Text>();
        }
        
        if (hpText != null) 
        {
            hpText.text = hp + "HP";
        }
    }
}