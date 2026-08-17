using System;
using UnityEngine;

    /// <summary>
    /// Model container holding the active runtime state for a user's logged-in session.
    /// This stays in memory and resets or overwrites when a new user logs in or out.
    /// </summary>
    public class UserSessionData
    {
        // Authentication Tokens
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;

        // Gameplay Session Tracking State
        public int CurrentItemsCollected { get; set; } = 0;
        public float BestRunTime { get; set; } = 0f;

        // Score Boost Multiplier (e.g., 2 for x2, 3 for x3, 4 for x4)
        private int _scoreBoostMultiplier = 1;
        public int ScoreBoostMultiplier
        {
            get => _scoreBoostMultiplier;
            set
            {
                // Enforce strict boundaries for allowed score multiplier boosts
                if (value == 2 || value == 3 || value == 4 || value == 1)
                {
                    _scoreBoostMultiplier = value;
                }
                else
                {
                    Debug.LogWarning($"Invalid boost multiplier attempted: x{value}. Falling back to default baseline (x1).");
                    _scoreBoostMultiplier = 1;
                }
            }
        }

        /// <summary>
        /// Clears volatile gameplay run details without wiping persistent authentication tokens.
        /// </summary>
        public void ResetGameplaySession()
        {
            CurrentItemsCollected = 0;
            ScoreBoostMultiplier = 1;
        }

        /// <summary>
        /// Full purge clearance of all account profiles (used during explicit sign-out sequences).
        /// </summary>
        public void ClearFullSession()
        {
            AccessToken = string.Empty;
            RefreshToken = string.Empty;
            CurrentItemsCollected = 0;
            BestRunTime = 0f;
            ScoreBoostMultiplier = 1;
        }
    }

    /// <summary>
    /// Central manager governing user session state lifetimes across all Unity scene environments.
    /// Implements a persistent MonoBehaviour Singleton pattern configuration.
    /// </summary>
    public class UserSessionManager : MonoBehaviour
    {
        // Global static access hook for active game systems
        public static UserSessionManager Instance { get; private set; }

        // Encapsulated data state layer
        private UserSessionData activeSession = new UserSessionData();

        // Exposed public read-only window to view data safely without direct corruption risks
        public UserSessionData ActiveSession => activeSession;

        // Event notifications allowing decoupled UI systems and score handlers to react to data updates
        public static event Action<int> OnItemsUpdated;
        public static event Action<int> OnBoostChanged;
        public static event Action<float> OnBestRunTimeRecordBroken;
        public static event Action OnSessionTerminated;

        private void Awake()
        {
            // Establish strict lifecycle initialization bounds
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Registers a successful authentication token pair inside the runtime data layers.
        /// </summary>
        public void InitializeAuthTokens(KeyCloakAuthResponse authToken)
        {
            if (string.IsNullOrEmpty(authToken.AccessToken) || string.IsNullOrEmpty(authToken.RefreshToken))
            {
                Debug.LogError("Failed authentication initialization. Tokens cannot be empty values.");
                return;
            }

            activeSession.AccessToken = authToken.AccessToken;
            activeSession.RefreshToken = authToken.RefreshToken;
            Debug.Log("User credentials securely cached for the active session.");
        }

        /// <summary>
        /// Records an item collection event, calculating score impact based on active boost values.
        /// </summary>
        /// <param name="baseItemScoreValue">The baseline unmultiplied points for collecting this specific item.</param>
        /// <returns>The final calculated points awarded after applying active boost parameters.</returns>
        public int RecordItemCollection(int baseItemScoreValue)
        {
            activeSession.CurrentItemsCollected++;
            
            // Calculate point output using the bounded session score multiplier property
            int pointsToAward = baseItemScoreValue * activeSession.ScoreBoostMultiplier;

            // Broadcast changes out to listening UI panels or achievement engines
            OnItemsUpdated?.Invoke(activeSession.CurrentItemsCollected);

            Debug.Log($"Item collected! Total: {activeSession.CurrentItemsCollected}. Points Awarded: {pointsToAward} (x{activeSession.ScoreBoostMultiplier} Boost applied).");
            return pointsToAward;
        }

        /// <summary>
        /// Updates the score modifier values to match specific unlockable boost windows.
        /// </summary>
        /// <param name="multiplier">Value must be 2, 3, or 4.</param>
        public void UpdateBoostMultiplier(int multiplier)
        {
            activeSession.ScoreBoostMultiplier = multiplier;
            OnBoostChanged?.Invoke(activeSession.ScoreBoostMultiplier);
        }

        /// <summary>
        /// Checks a newly completed run duration against historical best markers, updating records if faster.
        /// </summary>
        /// <param name="runTimeSeconds">The time taken to complete the run in fractional seconds.</param>
        public void SubmitRunTime(float runTimeSeconds)
        {
            if (runTimeSeconds <= 0f) return;

            // If it is the first completed run, or if the time spent is shorter than previous historical record
            if (activeSession.BestRunTime <= 0f || runTimeSeconds < activeSession.BestRunTime)
            {
                activeSession.BestRunTime = runTimeSeconds;
                OnBestRunTimeRecordBroken?.Invoke(activeSession.BestRunTime);
                Debug.Log($"New Personal Best Run Time Registered: {activeSession.BestRunTime:F2} seconds!");
            }
        }

        /// <summary>
        /// Terminates the current active game profile, safely cleaning memory footprints.
        /// </summary>
        public void SignOutUser()
        {
            activeSession.ClearFullSession();
            OnSessionTerminated?.Invoke();
            Debug.Log("User session cleanly terminated. Clearing active runtime memory channels.");
            
            // Optional: Redirect the user back to the Main Menu or Login Scene layout environment
            // UnityEngine.SceneManagement.SceneManager.LoadScene("LoginScene");
        }
    }

[Serializable]
public class KeyCloakAuthResponse
{
    public string access_token;
    public string token_type;
    public int expires_in;
    public int refresh_expires_in;
    public string refresh_token;
    public string scope;

    public string AccessToken => access_token;
    public string RefreshToken => refresh_token;
}


//NOTE:
/*
When an Item is Collected:csharp
// Increments item counts and returns your boosted points value to feed straight into your UI Score counters
int finalScoreGained = UserSessionManager.Instance.RecordItemCollection(baseValue: 10);
Use code with caution.Modifying Dynamic Boosts Mid-Game:csharp
// Changes item score multiplier calculation parameters to an active x3 reward model
UserSessionManager.Instance.UpdateBoostMultiplier(3);
Use code with caution.Finishing a Level:csharp
// Submits performance logs. The system handles comparative checks against historical metrics internally.
UserSessionManager.Instance.SubmitRunTime(levelTimer);

*/