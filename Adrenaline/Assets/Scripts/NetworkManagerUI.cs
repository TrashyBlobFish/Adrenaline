using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetworkManagerUI : MonoBehaviour
{
    [SerializeField] private Button serverBtn;
    [SerializeField] private Button hostBtn;
    [SerializeField] private Button clientBtn;
    public TestRelay relay;
    public Camera startcam;

    private void Awake()
    {
        serverBtn.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartServer();
            startcam.gameObject.SetActive(false);
        });
        hostBtn.onClick.AddListener(() =>
        {
            //NetworkManager.Singleton.StartHost();
            relay.CreateRelay();
            startcam.gameObject.SetActive(false);
        });
        clientBtn.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartClient();
            startcam.gameObject.SetActive(false);
        });
    }
}
