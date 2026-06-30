using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Will : MonoBehaviour
{
    [Header("Will Currency Settings")]
    public int currentWill = 0;
    public int maxWill = 7;

    [Header("UI Elements")]
    public Image willBarImage;   // Drag your visual fill bar or boxes here (optional)
    public TMP_Text willText;    // Drag your "0/7" text component here

    void Start()
    {
        UpdateWillVisuals();
    }

    public void GainWill(int amount)
    {
        currentWill = Mathf.Clamp(currentWill + amount, 0, maxWill);
        Debug.Log($"[{gameObject.name} WILL] Gained {amount} Will! Balance: {currentWill}/{maxWill}");
        UpdateWillVisuals();
    }

    // ✨ THE FIX: Re-adding the missing SpendWill method for AvatarReincarnation
    public bool SpendWill(int amount)
    {
        if (currentWill >= amount)
        {
            currentWill -= amount;
            Debug.Log($"[{gameObject.name} WILL] Spent {amount} Will. Balance: {currentWill}/{maxWill}");
            UpdateWillVisuals();
            return true;
        }
        
        Debug.LogWarning($"[{gameObject.name} WILL] Failed to spend {amount} Will. Not enough resources! Balance: {currentWill}/{maxWill}");
        return false;
    }

    public void UpdateWillVisuals()
    {
        // Keep the text display fields up to date frame-by-frame
        if (willText != null)
        {
            willText.text = $"{currentWill}/{maxWill}";
        }

        // Keep fill bar graphics updated if you use an Image container
        if (willBarImage != null)
        {
            willBarImage.fillAmount = (float)currentWill / (float)maxWill;
            willBarImage.SetVerticesDirty();
            willBarImage.SetMaterialDirty();
        }
    }
}