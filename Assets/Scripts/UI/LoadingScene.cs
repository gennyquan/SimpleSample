using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Networking; // Required for REST API calls
using TMPro;
using System.Collections.Generic;
using Newtonsoft.Json;

public class GameInitializer : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Image progressBarFill;
    [SerializeField] private Button startButton;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private TextMeshProUGUI buttonText;

    [Header("Settings")]
    [SerializeField] private string gameplaySceneName = "GameplayScene"; // Change to your exact gameplay scene name

    private AsyncOperation sceneLoadOperation;
    private bool apiLoadingDataComplete = false;
    private bool apiLoadingDataFailed = false;

    private void Start()
    {
        // Explicitly hide UI elements at start to be safe
        startButton.gameObject.SetActive(false);

        // Start loading scene and calling API at the same time
        StartCoroutine(StartLoadingSequence());
    }

    private IEnumerator StartLoadingSequence()
    {
         // 1. Kick off the REST API call in the background
        StartCoroutine(FetchAccountInformation());

        // 2. Kick off the Gameplay Scene asset loading in the background
        sceneLoadOperation = SceneManager.LoadSceneAsync(gameplaySceneName);
        
        // Prevent the game from instantly opening the scene when it finishes downloading
        sceneLoadOperation.allowSceneActivation = false;

        // Calculate how much to fill the bar during each step
        float progressPerStep = 1.0f / 100;

        DateTime startTime = DateTime.Now;

        for (int i = 1; i <= 100; i++)
        {
            // 1. Wait for 200 milliseconds
            yield return new WaitForSeconds(0.1f);

            // 2. Increase the progress bar value
            float currentProgress = i * progressPerStep;
            if (progressBarFill != null)
            {
                progressBarFill.fillAmount = currentProgress;
            }
            // 3. Update text to show percentage (e.g., "Loading... 40%")
            if (messageText != null)
            {
                int percentage = Mathf.RoundToInt(currentProgress * 100f);
                messageText.text = "Loading... " + percentage + "%";
            }
            if (sceneLoadOperation.progress >= 0.9f && (apiLoadingDataComplete || apiLoadingDataFailed)&&  DateTime.Now.Subtract(startTime).TotalSeconds > 2)
            {
                progressBarFill.fillAmount = 1f; // Force fill to 100%
                
                // Show the start button to the player!
                startButton.gameObject.SetActive(true);
                //startButton.gameObject
                messageText.text = "Loading Complete! Click Start to Play!";
                buttonText.text = "Start";
                SessionTimerManager.Instance.StartSessionTimer(150f); // Start the 150-second timer
                CentralHubManager.Instance.Connect();
                yield break;
            }
        }
            yield return null;
   
    }

    // This handles the REST API call to initialize account information
    private IEnumerator FetchAccountInformation()
    {
        string deviceId = SystemInfo.deviceUniqueIdentifier;

        using (UnityWebRequest webRequest = UnityWebRequest.Get($"https://aspnetauth-977736336619.us-west1.run.app/Account/Init/{deviceId}/ios"))
        {
            webRequest.certificateHandler = new BypassCertificate();
            // Send request and wait for a response without freezing the game UI
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.ConnectionError || 
                webRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Account Initialization Failed: " + webRequest.error);
                apiLoadingDataFailed = true;
            }
            else
            {
                // API success!
                string jsonResponse = webRequest.downloadHandler.text;
                try
                {
                    Debug.Log("Received API response: " + jsonResponse);

                    if (UserSessionManager.Instance == null)
                    {
                        Debug.LogError("UserSessionManager instance is null. Ensure it is initialized before API calls.");
                        apiLoadingDataFailed = true;
                        yield break;
                    }
                    KeyCloakAuthResponse authToken = JsonConvert.DeserializeObject<KeyCloakAuthResponse>(jsonResponse);
                    Debug.Log("AuthToken parsed: " + authToken.AccessToken + ", " + authToken.RefreshToken);

                    UserSessionManager.Instance.InitializeAuthTokens(authToken);
                    Debug.Log("Account loaded successfully: " + authToken);
                }catch (Exception ex)
                {
                    Debug.LogError("Failed to parse account initialization response: " + ex.Message);
                    apiLoadingDataFailed = true;
                    yield break;
                }

                yield return FetchAccoutInventory();

                apiLoadingDataComplete = true;
            }
        }
    }

    private IEnumerator FetchAccoutInventory()
    {
        string deviceId = SystemInfo.deviceUniqueIdentifier;
        var inventoryLoadURL=$"https://aspnetapplicationhub-977736336619.us-west1.run.app/api/inventory/loadInventory?accountId={UserSessionManager.Instance.ActiveSession.Username}&deviceId={deviceId}&platform=ios";
        Debug.Log("Inventory Load URL: " + inventoryLoadURL);

        using (UnityWebRequest webRequest = UnityWebRequest.Get(inventoryLoadURL))
        {
            webRequest.certificateHandler = new BypassCertificate();
            // Send request and wait for a response without freezing the game UI
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.ConnectionError || 
                webRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Account Inventory loading Failed: " + webRequest.error);
                apiLoadingDataFailed = true;
            }
            else
            {
                // API success!
                string jsonResponse = webRequest.downloadHandler.text;
                try
                {
                    Debug.Log("Received API response: " + jsonResponse);

                    if (UserSessionManager.Instance == null)
                    {
                        Debug.LogError("UserSessionManager instance is null. Ensure it is initialized before API calls.");
                        apiLoadingDataFailed = true;
                        yield break;
                    }
                    List<AccountInventory> inventory = JsonConvert.DeserializeObject<List<AccountInventory>>(jsonResponse);
                    if (inventory.Count > 0)
                    {
                        foreach (var item in inventory)
                        {
                            switch(item.ItemId)
                            {
                                case 1:
                                    UserSessionManager.Instance.ActiveSession.CollectorBoosterCount = item.Amount;
                                    Debug.Log("Account inventory parsed: " + item.ItemId + ", " + item.Name + ", " + item.Amount);
                                    break;
                                case 2:
                                    UserSessionManager.Instance.ActiveSession.DeathRemovalCount = item.Amount;
                                    Debug.Log("Account inventory parsed: " + item.ItemId + ", " + item.Name + ", " + item.Amount);
                                    break;
                                case 3:
                                    UserSessionManager.Instance.ActiveSession.FreezeTimeCount = item.Amount;
                                    Debug.Log("Account inventory parsed: " + item.ItemId + ", " + item.Name + ", " + item.Amount);
                                    break;
                                case 4:
                                    UserSessionManager.Instance.ActiveSession.InvisibleCount = item.Amount;
                                    Debug.Log("Account inventory parsed: " + item.ItemId + ", " + item.Name + ", " + item.Amount);
                                    break;
                                default:
                                    Debug.LogWarning("Unknown item ID: " + item.ItemId);
                                    break;
                            }
                        Debug.Log("Account inventory parsed: " + item.ItemId + ", " + item.Name + ", " + item.Amount);
                        }
                    }
                    else
                    {
                        Debug.Log("Account inventory parsed: empty.");
                    }

                    UserSessionManager.Instance.ActiveSession.Inventory = inventory;
                    Debug.Log("Account loaded successfully: " + inventory);   
                }catch (Exception ex)
                {
                    Debug.LogError("Failed to parse account initialization response: " + ex.Message);
                    apiLoadingDataFailed = true;
                    yield break;
                }
                

                apiLoadingDataComplete = true;
            }
        }
    }
    // This public function will be called when the player clicks the Start Button
    public void OnStartButtonClicked()
    {
        if (sceneLoadOperation != null)
        {
            // Unlocks the gate and instantly moves the player into the gameplay scene
            sceneLoadOperation.allowSceneActivation = true;
        }
    }
}


public class BypassCertificate : CertificateHandler
{
    // WARNING: This accepts ALL certificates. Do not use in production.
    protected override bool ValidateCertificate(byte[] certificateData)
    {
        return true; 
    }
}