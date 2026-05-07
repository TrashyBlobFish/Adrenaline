using System.IO;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveJSONData : MonoBehaviour
{
    private GameObject gc;
    private DataProcessing dataProcessing;
    private string path = "";

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gc = GameObject.Find("GameManager");
        SetPath();
    }

    private void SetPath()
    {
        path = Application.persistentDataPath + "/" + System.DateTime.UtcNow.ToLocalTime().
            ToString("M-d-yy-HH-mm") + ".json";
    }
    // Update is called once per frame
    public void SaveAndExit()
    {
        Debug.Log("[SaveJSONData] Starting SaveAndExit process...");
        CreateDataToSave();
        SaveData();
        StartCoroutine(ExitPause());
    }

    private IEnumerator ExitPause()
    {
        yield return new WaitForSeconds(2f);
        Debug.Log("[SaveJSONData] Quitting application...");

        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            Debug.Log("[SaveJSONData] Editor detected - stopping play mode instead of quitting.");
        #else
            Application.Quit();
        #endif
    }

    private void CreateDataToSave()
    {
        Debug.Log("[SaveJSONData] Creating data to save...");
        dataProcessing = new DataProcessing(
            GetComponent<UserProfileData>().NumberOfFalls,
            GetComponent<UserProfileData>().TimesShieldUsed,
            GetComponent<UserProfileData>().TimeSpentAFK,
            GetComponent<UserProfileData>().Timespent3rdPerson,
            GetComponent<UserProfileData>().Timespent1stPerson);
    }

    private void SaveData()
    {
        try
        {
            Debug.Log($"[SaveJSONData] Saving data to: {path}");
            string json = JsonUtility.ToJson(dataProcessing);
            StreamWriter writer = new StreamWriter(path);
            writer.Write(json);
            writer.Close();
            Debug.Log("[SaveJSONData] Data saved successfully!");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveJSONData] Error saving data: {e.Message}");
        }
    }
}
