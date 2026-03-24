using UnityEngine;

public class PersistantGO : MonoBehaviour
{
    public static PersistantGO instance;

    void Awake()
    {
        
        DontDestroyOnLoad(this.gameObject);
    }
}
