using UnityEngine;

public class AuthManager : MonoBehaviour
{
    // Triggered by Login Button onClick()
    public void OnLoginClicked()
    {
        //StartCoroutine(SendAuthRequest("/login"));
    }

    // Triggered by Register Button onClick()
    public void OnRegisterClicked()
    {
        //StartCoroutine(SendAuthRequest("/register"));
    }

    private void RegisterNewAccount(){

    }
/*
    private IEnumerator SendAuthRequest(string endpoint)
    {
        // 1. Validate Input Fields
        if (string.IsNullOrEmpty(usernameInput.text) || string.IsNullOrEmpty(passwordInput.text))
        {
            UpdateStatus("Username and Password cannot be empty!", Color.red);
            yield break;
        }

        SetFormInteractable(false);
        UpdateStatus("Connecting to server...", Color.yellow);

        // 2. Prepare Payload
        AuthRequestData data = new AuthRequestData()
        {
            username = usernameInput.text,
            password = passwordInput.text
        };
        
        string jsonPayload = JsonUtility.ToJson(data);
        byte[] rawPayload = Encoding.UTF8.GetBytes(jsonPayload);

        // 3. Configure UnityWebRequest for POST
        string fullUrl = baseApiUrl + endpoint;
        using (UnityWebRequest request = new UnityWebRequest(fullUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(rawPayload);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            // 4. Send Request asynchronously
            yield return request.SendWebRequest();

            // 5. Evaluate response
            if (request.result == UnityWebRequest.Result.Success)
            {
                HandleServerResponse(request.downloadHandler.text, endpoint);
            }
            else
            {
                // Display network failure or HTTP errors (e.g., 400 Bad Request, 401 Unauthorized)
                UpdateStatus($"Error: {request.error} | {request.downloadHandler.text}", Color.red);
            }
        }

        SetFormInteractable(true);
*/
}

