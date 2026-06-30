using UnityEngine;
using UnityEngine.SceneManagement;

public class EnchanterChoice : MonoBehaviour
{
    // Make sure these names match your Build Settings scene names exactly
    public void LoadEnchanterDeck1Scene()
    {
        SceneManager.LoadScene("EnchanterDeck1"); 
    }

    public void LoadEnchanterDeck2Scene()
    {
        SceneManager.LoadScene("EnchanterDeck2");
    }

    public void LoadMenuFromEnchanterScene()
    {
        SceneManager.LoadScene("MainMenu");
    }
}