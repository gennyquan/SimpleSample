using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.Networking;
using System;
using System.Collections;
using Newtonsoft.Json;

public class ItemBarController : MonoBehaviour
{
    public static ItemBarController Instance { get; private set; }

    [Header("UI Text References")]
    public TextMeshProUGUI collectorBoosterText;
    public TextMeshProUGUI deathRemovalText;
    public TextMeshProUGUI freezeTimeText;
    public TextMeshProUGUI invisibleText;

    public TextMeshProUGUI ItemInUseText;
    public TextMeshProUGUI ItemRemainingTimeText;

    public GameObject ItemInUseCanvas; // Reference to the canvas that shows item in use and remaining time


    private InputAction m_useCollectorBoosterAction;
    private InputAction m_useDeathRemovalAction;
    private InputAction m_useFreezeTimeAction;
    private InputAction m_useInvisibleAction;

    private float itemRemainingTime;
    [SerializeField] 
    private float itemEffectDuration = 30f; // however long an item effect should last

    private bool itemInUsed = false; // Flag to track if an item is currently in use

    void Awake()
    {
        Instance = this;
        m_useCollectorBoosterAction = InputSystem.actions.FindAction("Player/UseCollectorBooster");
        m_useDeathRemovalAction = InputSystem.actions.FindAction("Player/UseDeathRemoval");
        m_useFreezeTimeAction = InputSystem.actions.FindAction("Player/UseFreezeTime");
        m_useInvisibleAction = InputSystem.actions.FindAction("Player/UseInvisible");

        m_useCollectorBoosterAction.Enable();
        m_useDeathRemovalAction.Enable();
        m_useFreezeTimeAction.Enable();
        m_useInvisibleAction.Enable();
        ItemInUseCanvas.SetActive(false); // Initially hide the item in use canvas
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Set up the initial text values
        UpdateUI();
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!itemInUsed)
        {
            if (m_useCollectorBoosterAction.WasPressedThisFrame())
            {
                UseCollectorBooster();
            }
            if (m_useDeathRemovalAction.WasPressedThisFrame())
            {
                UseDeathRemoval();
            }
            if (m_useFreezeTimeAction.WasPressedThisFrame())
            {
                UseFreezeTime();
            }
            if (m_useInvisibleAction.WasPressedThisFrame())
            {
                UseInvisible();
            }
        }
        else
        {
            itemRemainingTime -= Time.deltaTime;
            ItemRemainingTimeText.text = "Remaining Time: " + Mathf.Max(0, itemRemainingTime).ToString("F1");
            if (itemRemainingTime <= 0f)
            {
                ResetItemUse();
                ItemInUseText.text = "No item in use";
                ItemRemainingTimeText.text = "";
                ItemInUseCanvas.SetActive(false);
            }
        }
       
    }
     // Call this function when the player grabs an item
    public void UseCollectorBooster()
    {
        if (!itemInUsed && UserSessionManager.Instance.ActiveSession.CollectorBoosterCount > 0)
        {
            itemInUsed = true;
            itemRemainingTime = itemEffectDuration;
            StartCoroutine(SendItemUse(1, success => {
                if (success)
                {
                    UserSessionManager.Instance.ActiveSession.CollectorBoosterCount--;
                    ItemInUseText.text = "Collector Booster in use";
                    ItemInUseCanvas.SetActive(true); // Initially hide the item in use canvas
                    UserSessionManager.Instance.ActiveSession.IsDoubleCollector=true;
                    UpdateUI();
                }
                else
                {
                    ResetItemUse();
                }
            }));
        }
        else if (UserSessionManager.Instance.ActiveSession.CollectorBoosterCount <= 0)
        {
            Debug.LogWarning("No Collector Booster items left to use!");
        }
    }

    // Call this function when the player dies
    public void UseDeathRemoval()
    {
        if (!itemInUsed && UserSessionManager.Instance.ActiveSession.DeathRemovalCount > 0)
        {
             StartCoroutine(SendItemUse(2, success => {
                if (success)
                {
                    itemInUsed = true;
                    itemRemainingTime = itemEffectDuration;
                    UserSessionManager.Instance.ActiveSession.DeathRemovalCount--;
                    ItemInUseText.text = "Death Removal in use";
                    ItemInUseCanvas.SetActive(true); // Initially hide the item in use canvas
                    ScoreBarController.Instance.RemoveDeath();
                    UpdateUI();
                }
                else
                {
                    ResetItemUse();
                }
            }));
        }
        else if (UserSessionManager.Instance.ActiveSession.DeathRemovalCount <= 0)
        {
            Debug.LogWarning("No Death Removal items left to use!");
        }
    }

    public void UseFreezeTime()
    {
        if (!itemInUsed && UserSessionManager.Instance.ActiveSession.FreezeTimeCount > 0)
        {
             StartCoroutine(SendItemUse(3, success => {
                if (success)
                {
                    itemInUsed = true;
                    itemRemainingTime = itemEffectDuration;
                    UserSessionManager.Instance.ActiveSession.FreezeTimeCount--;
                    ItemInUseText.text = "Freeze Time in use";
                    ItemInUseCanvas.SetActive(true); // Initially hide the item in use canvas
                    UpdateUI();
                    ScoreBarController.Instance.IsPausingTime=true;
                
                }
                else
                {
                    ResetItemUse();
                }
            }));
        }
        else if (UserSessionManager.Instance.ActiveSession.FreezeTimeCount <= 0)
        {
            Debug.LogWarning("No Freeze Time items left to use!");
        }
    }

    public void ResetItemUse()
    {
        itemInUsed=false;
        UserSessionManager.Instance.ActiveSession.IsDoubleCollector=false; // Double Collector
        if (ScoreBarController.Instance.IsPausingTime) // Freeze time
        {
            ScoreBarController.Instance.IsPausingTime=false;
        }
        
        UserSessionManager.Instance.ActiveSession.IsInvisible=false; // Invisible
        // Death Removal effect right away
    }

    public void UseInvisible()
    {
        if (!itemInUsed && UserSessionManager.Instance.ActiveSession.InvisibleCount > 0)
        {
             StartCoroutine(SendItemUse(4, success => {
                if (success)
                {
                    itemInUsed = true;
                    itemRemainingTime = itemEffectDuration;
                    UserSessionManager.Instance.ActiveSession.InvisibleCount--;
                    ItemInUseText.text = "Invisible in use";
                    ItemInUseCanvas.SetActive(true); // Initially hide the item in use canvas
                    UserSessionManager.Instance.ActiveSession.IsInvisible=true;
                    UpdateUI();
                }
                else
                {
                    ResetItemUse();
                }
            }));
        }
        else if (UserSessionManager.Instance.ActiveSession.InvisibleCount <= 0)
        {
            Debug.LogWarning("No Invisible items left to use!");
        }
    }


    // Call this when the server pushes an inventory change (e.g. SignalR "ReceiveItemAdded").
    public void AddItemCount(int itemId, int amount)
    {
        switch (itemId)
        {
            case 1:
                UserSessionManager.Instance.ActiveSession.CollectorBoosterCount += amount;
                break;
            case 2:
                UserSessionManager.Instance.ActiveSession.DeathRemovalCount += amount;
                break;
            case 3:
                UserSessionManager.Instance.ActiveSession.FreezeTimeCount += amount;
                break;
            case 4:
                UserSessionManager.Instance.ActiveSession.InvisibleCount += amount;
                break;
            default:
                Debug.LogWarning($"AddItemCount: unknown itemId {itemId}");
                return;
        }

        UpdateUI();
    }

    void UpdateUI()
    {
        collectorBoosterText.text = "Collector Booster: " + UserSessionManager.Instance.ActiveSession.CollectorBoosterCount;
        deathRemovalText.text = "Death Removal: " + UserSessionManager.Instance.ActiveSession.DeathRemovalCount;
        freezeTimeText.text = "Freeze Time: " + UserSessionManager.Instance.ActiveSession.FreezeTimeCount;
        invisibleText.text = "Invisible: " + UserSessionManager.Instance.ActiveSession.InvisibleCount;
    }
    private IEnumerator SendItemUse(int itemId, Action<bool> onResult = null)
    {
        if (UserSessionManager.Instance == null)
        {
            Debug.LogError("UserSessionManager instance is null. Ensure it is initialized before API calls.");
            onResult?.Invoke(false);
            yield break;
        }

        AccountItemUse itemUse = new AccountItemUse
        {
            accountId = UserSessionManager.Instance.ActiveSession.Username,
            deviceId = SystemInfo.deviceUniqueIdentifier,
            platform = "ios",
            ItemId = itemId
        };
        byte[] payloadBytes = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(itemUse));

        using (UnityWebRequest webRequest = new UnityWebRequest("https://aspnetapplicationhub-977736336619.us-west1.run.app/api/inventory/useItem", "POST"))
        {
            webRequest.uploadHandler = new UploadHandlerRaw(payloadBytes);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");
            webRequest.certificateHandler = new BypassCertificate();

            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.ConnectionError ||
                webRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Item use request failed: " + webRequest.error);
                onResult?.Invoke(false);
            }
            else
            {
                Debug.Log("Item use recorded successfully: " + webRequest.downloadHandler.text);
                onResult?.Invoke(true);
            }
        }
    }
}
[Serializable]
public class AccountItemUse
{
    public string accountId {get;set;}
    public string deviceId{get;set;}
    public string platform {get;set;}
    public int ItemId { get; set; }
    }