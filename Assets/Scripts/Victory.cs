using UnityEngine;
using UnityEngine.SceneManagement;

public class Victory : MonoBehaviour
{
    public void LoadMenuFromVictoryScene()
    {
        SceneManager.LoadScene("MainMenu");
    }
}