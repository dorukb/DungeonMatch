using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AutoSceneChanger : MonoBehaviour
{
    [Scene]
    public string sceneToLoad;
    public float delaySeconds;
    void Start()
    {
        Invoke("LoadScene", delaySeconds);
    }

    private void LoadScene()
    {
        SceneManager.LoadScene(sceneToLoad);
    }
}
