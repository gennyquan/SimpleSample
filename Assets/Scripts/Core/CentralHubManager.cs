using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using UnityEngine;

public class CentralHubManager : MonoBehaviour
{
    public static CentralHubManager Instance { get; private set; }

    private const string HubUrl = "https://aspnetapplicationhub-977736336619.us-west1.run.app/centralHub";

    private HubConnection hubConnection;
    private readonly ConcurrentQueue<Action> mainThreadQueue = new();

    public bool IsConnected => hubConnection != null && hubConnection.State == HubConnectionState.Connected;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public async void Connect()
    {
        if (hubConnection != null)
        {
            Debug.LogWarning("CentralHub connection already exists.");
            return;
        }

        string userId = UserSessionManager.Instance?.ActiveSession.Username+"-app";
        Debug.Log($"UserID to connect to SignalRHub:{userId}");
        string hubUrlWithUserId = $"{HubUrl}?accountId={Uri.EscapeDataString(userId?? string.Empty)}";

        hubConnection = new HubConnectionBuilder()
            .WithUrl(hubUrlWithUserId)
            .WithAutomaticReconnect()
            .Build();

        RegisterHandlers();

        try
        {
            await hubConnection.StartAsync();
            Debug.Log("Connected to CentralHub.");
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to connect to CentralHub: " + ex.Message);
        }
    }

    private void RegisterHandlers()
    {
        hubConnection.On<AccountInventoryAdd>("ReceiveItemAdded", OnItemAdded);
    }

    // Runs on a SignalR transport thread, not Unity's main thread — must not touch Unity APIs directly.
    private void OnItemAdded(AccountInventoryAdd itemAdd)
    {
        Debug.Log($"ReceiveItemAdded raw message: AccountHash={itemAdd.AccountHash}, DeviceId={itemAdd.DeviceId}, Platform={itemAdd.Platform}, ItemID={itemAdd.ItemID}, ItemName={itemAdd.ItemName}, Amount={itemAdd.Amount}");

        mainThreadQueue.Enqueue(() =>
        {
            Debug.Log($"Item added: {itemAdd.ItemName} x{itemAdd.Amount}");
            ItemBarController.Instance?.AddItemCount(itemAdd.ItemID, itemAdd.Amount);
        });
    }

    private void Update()
    {
        while (mainThreadQueue.TryDequeue(out var action))
        {
            action();
        }
    }

    public async void Disconnect()
    {
        if (hubConnection == null) return;

        try
        {
            await hubConnection.StopAsync();
            await hubConnection.DisposeAsync();
        }
        catch (Exception ex)
        {
            Debug.LogError("Error disconnecting from CentralHub: " + ex.Message);
        }
        finally
        {
            hubConnection = null;
        }
    }

    private void OnApplicationQuit()
    {
        Disconnect();
    }
}


[Serializable]
public class AccountInventoryAdd
{
    public string AccountHash{get;set;}
    public string DeviceId {get;set;}
    public string Platform{get;set;}
    public int ItemID{get;set;}
    public string ItemName{get;set;}
    public int Amount{get;set;}
}