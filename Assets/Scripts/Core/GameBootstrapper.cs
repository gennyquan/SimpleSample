using UnityEngine;

public static class GameBootstrapper
{
    // The attribute tells Unity to run this method before the initial scene loads
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ExecuteBootstrap()
    {
        // Check if an instance already exists in memory
    if (UserSessionManager.Instance == null)
    {
        // Automatically create a new persistent manager object before ANY scene loads
        GameObject managerGo = new GameObject("Runtime_UserSessionManager");
        managerGo.AddComponent<UserSessionManager>();
        
        Debug.Log("UserSessionManager dynamically bootstrapped before initial scene layout load.");
    }
    }
}
