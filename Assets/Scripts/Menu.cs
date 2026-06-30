using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    // Make sure these names match your Build Settings scene names exactly
    public void LoadPlayScene()
    {
        SceneManager.LoadScene("FactionChoice"); 
    }

    public void LoadCollectionScene()
    {
        SceneManager.LoadScene("Collection");
    }

    public void QuitGame()
    {
    #if UNITY_EDITOR
        // If we are in the editor, stop the play mode
        UnityEditor.EditorApplication.isPlaying = false;
    #else
        // If we are in a built game, close the application
        Application.Quit();
    #endif
        Debug.Log("Quit. Until we meet again, farewell");
    }
}