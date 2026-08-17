using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Networking; // Required for REST API calls
using TMPro;                  // Required for TextMeshPro

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
                yield break;
            }
        }
            yield return null;
   
    }

    // This handles the REST API call to initialize account information
    private IEnumerator FetchAccountInformation()
    {
        string deviceId = SystemInfo.deviceUniqueIdentifier;

        using (UnityWebRequest webRequest = UnityWebRequest.Get($"https://192.168.1.71:9001/Account/Init/{deviceId}"))
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
                    KeyCloakAuthResponse authToken = JsonUtility.FromJson<KeyCloakAuthResponse>(jsonResponse);
                    Debug.Log("AuthToken parsed: " + authToken.AccessToken + ", " + authToken.RefreshToken);

                    UserSessionManager.Instance.InitializeAuthTokens(authToken);
                    Debug.Log("Account loaded successfully: " + authToken);   
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