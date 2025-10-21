using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DelayedSceneChanger : MonoBehaviour
{  
    [Scene]
    public string MainMenuScene;
    public float DelaySecs = 1.5f;
    void Start()
    {
        Invoke("LoadMainMenu", DelaySecs);
    }

    private void LoadMainMenu()
    {
        SceneManager.LoadScene(MainMenuScene);
    }

}
