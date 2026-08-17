using System.Collections;
using UnityEngine;
using System;
using UnityEngine.Networking; // Required for REST API calls
public class SessionTimerManager : MonoBehaviour
{
    // Static instance allows any other script to access it easily
    public static SessionTimerManager Instance { get; private set; }

    private Coroutine timerCoroutine;
    private bool isTimerRunning = false;

    private void Awake()
    {
        // Enforce Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Persists across scenes
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Call this method from any other script to start the timer
    public void StartSessionTimer(float intervalInSeconds)
    {
        Debug.LogWarning("Timer starting(" + intervalInSeconds + ")");
        if (isTimerRunning)
        {
            Debug.LogWarning("Timer is already running!");
            return;
        }

        isTimerRunning = true;
        timerCoroutine = StartCoroutine(TimerLoop(intervalInSeconds));
        Debug.LogWarning("Timer started successfully.");
    }

    // Call this if you ever need to stop it manually
    public void StopSessionTimer()
    {
        if (isTimerRunning && timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            isTimerRunning = false;
        }
    }

    private IEnumerator TimerLoop(float interval)
    {
        // Use WaitForSecondsRealtime so it runs even if Time.timeScale is 0 (e.g., paused game)
        WaitForSecondsRealtime wait = new WaitForSecondsRealtime(interval);

        while (isTimerRunning)
        {
            yield return wait;
            yield return StartCoroutine(ExecuteTimerLogic());
        }
    }

    private IEnumerator ExecuteTimerLogic()
    {
        Debug.Log($"Timer triggered at session time: {Time.unscaledTime} seconds.");

        if (UserSessionManager.Instance == null)
        {
            Debug.LogError("UserSessionManager instance is null. Ensure it is initialized before API calls.");
            yield break;
        }

        string deviceId = SystemInfo.deviceUniqueIdentifier;
        WWWForm form = new WWWForm();
        form.AddField("token", UserSessionManager.Instance.ActiveSession.RefreshToken);
        using (UnityWebRequest webRequest = UnityWebRequest.Post($"https://192.168.1.71:9001/Account/Renew/{deviceId}",form))
        {
            
            webRequest.certificateHandler = new BypassCertificate();
            // Send request and wait for a response without freezing the game UI
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.ConnectionError || 
                webRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Account renew Failed: " + webRequest.error);
            }
            else
            {
                // API success!
                string jsonResponse = webRequest.downloadHandler.text;
                try
                {
                    Debug.Log("Renew Received API response: " + jsonResponse);

                    KeyCloakAuthResponse authToken = JsonUtility.FromJson<KeyCloakAuthResponse>(jsonResponse);
                    Debug.Log("AuthToken parsed: " + authToken.AccessToken + ", " + authToken.RefreshToken);

                    UserSessionManager.Instance.InitializeAuthTokens(authToken);
                    Debug.Log("Account loaded successfully: " + authToken);   
                }catch (Exception ex)
                {
                    Debug.LogError("Failed to parse account renewal response: " + ex.Message);
                }
            }
        }   
    }
}
