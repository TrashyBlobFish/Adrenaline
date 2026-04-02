using TMPro;
using UnityEngine;

public class AssignID : MonoBehaviour
{
    void Awake()
    {
        gameObject.GetComponentInChildren<TextMeshProUGUI>().text = GameObject.Find("GameManager").GetComponent<UserProfileData>().PlayerID;
    }

    
}
