using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public string victorySceneName = "Victory";
    public string defeatSceneName = "Defeat";
    private bool isGameOver = false;

    public void EndGame(bool isPlayerWinner)
    {
        if (isGameOver) return; 
        isGameOver = true;

        Debug.Log("Game Over! Result: " + (isPlayerWinner ? "Victory" : "Defeat"));

        // Start the Coroutine to wait, then load the scene
        StartCoroutine(TransitionToScene(isPlayerWinner));
    }

    private System.Collections.IEnumerator TransitionToScene(bool isPlayerWinner)
    {
        // Wait 2 seconds so the player sees their HP hit 0
        yield return new WaitForSeconds(2.0f); 
        
        if (isPlayerWinner)
            SceneManager.LoadScene(victorySceneName);
        else
            SceneManager.LoadScene(defeatSceneName);
    }

    public static void ResetGameStats()
    {
        PlayerHP.staticHP = 30;
        OpponentHP.staticHP = 30;
        
        // Force find the objects and refresh them immediately
        PlayerHP pHP = FindFirstObjectByType<PlayerHP>();
        if (pHP != null) { pHP.hp = 30; pHP.UpdateVisuals(); }
        
        OpponentHP oHP = FindFirstObjectByType<OpponentHP>();
        if (oHP != null) { oHP.hp = 30; oHP.UpdateVisuals(); }
    }
}