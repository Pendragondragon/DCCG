using UnityEngine;
using UnityEngine.SceneManagement;

public class Defeat : MonoBehaviour
{
    public void LoadMenuFromDefeatScene()
    {
        SceneManager.LoadScene("MainMenu");
    }
}