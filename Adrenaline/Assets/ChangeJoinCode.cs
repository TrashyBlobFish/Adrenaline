using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChangeJoinCode : MonoBehaviour
{
    public string newJoinCode;
    public TMP_InputField Codefield;
    public GameObject TestRelay;

    public void ChangeCode()
    {
        newJoinCode = Codefield.text;
        TestRelay.GetComponent<TestRelay>().JoinRelay(newJoinCode);
    }
}
