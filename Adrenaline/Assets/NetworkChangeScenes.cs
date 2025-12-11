using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkChangeScenes : MonoBehaviour
{
    public void ChangeScene(string sceneName)
    {
        NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }
}
