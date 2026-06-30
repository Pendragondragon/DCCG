using UnityEngine;
using UnityEngine.SceneManagement;

public class FactionChoice : MonoBehaviour
{
    // Make sure these names match your Build Settings scene names exactly
    public void LoadEnchanterScene()
    {
        SceneManager.LoadScene("Enchanter"); 
    }

    public void LoadMonsterSlayerScene()
    {
        SceneManager.LoadScene("MonsterSlayer");
    }

    public void LoadMenuScene()
    {
        SceneManager.LoadScene("MainMenu");
    }
}