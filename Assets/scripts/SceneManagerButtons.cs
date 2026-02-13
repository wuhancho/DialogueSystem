using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneManagerButtons : MonoBehaviour
{
    public void LoadScene(string sceneName)
    {
        if (sceneName == "Exit")
        {
            Application.Quit();
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }
}
