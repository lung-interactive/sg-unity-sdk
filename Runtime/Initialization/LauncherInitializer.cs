using System;
using Cysharp.Threading.Tasks;
using HMSUnitySDK;
using HMSUnitySDK.LauncherInteroperations;
using SocketIOClient;
using TMPro;
using UnityEngine;
using UnityEngine.Scripting;

namespace SGUnitySDK
{
    /// <summary>
    /// Boots the Unity client, connects to the external launcher via interop,
    /// retrieves authentication data, and signals readiness to the launcher.
    /// Designed to run before the first scene loads and to remove its UI
    /// overlay once initialization completes or fails.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class LauncherInitializer : MonoBehaviour
    {

        #region Numeric Constants
        // ─────────────────────────────────────────────────────────────────────
        // Numeric Constants
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Delay used to give the user feedback on successful init
        /// before the overlay disappears (non-Editor runtime only).
        /// </summary>
        private const int SuccessDelayMs = 2000;

        #endregion

        #region Static State
        // ─────────────────────────────────────────────────────────────────────
        // Static State
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Tracks initialization completion for other systems to await.
        /// </summary>
        private static UniTaskCompletionSource<bool> _initUts;

        /// <summary>
        /// Guard to avoid duplicate bootstrap during domain reloads.
        /// </summary>
        private static bool _bootstrapped;

        /// <summary>
        /// Exposes the initialization UniTask. Returns a completed UniTask(true)
        /// if initialization has already finished or no bootstrap is needed.
        /// </summary>
        public static UniTask<bool> Init =>
            _initUts?.Task ?? UniTask.FromResult(true);

        #endregion

        #region Inspector
        // ─────────────────────────────────────────────────────────────────────
        // Inspector
        // ─────────────────────────────────────────────────────────────────────

        [SerializeField] private TextMeshProUGUI _textMeshPro;
        [SerializeField] private GameObject _loadingEffect;

        #endregion

        #region Services
        // ─────────────────────────────────────────────────────────────────────
        // Services
        // ─────────────────────────────────────────────────────────────────────

        private HMSLauncherInteropsService _launcherInterops;
        private HMSAuth _hmsAuth;

        #endregion

        #region Runtime Bootstrap
        // ─────────────────────────────────────────────────────────────────────
        // Runtime Bootstrap
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Entry point that runs before the first scene load. Ensures an
        /// initialization overlay exists and remains across scene loads.
        /// </summary>
        [Preserve]
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RuntimeInitialize()
        {
            if (_bootstrapped) return;

            _initUts ??= new UniTaskCompletionSource<bool>();

            /*
             * Attempts to load the initialization UI prefab.
             * Unity objects must not use null coalescing due to custom null handling.
             * Loads primary prefab first; if not found, loads legacy prefab.
             */
            GameObject prefab = Resources.Load<GameObject>(S.Resources.InitCanvasPrimary);
            if (prefab == null)
            {
                prefab = Resources.Load<GameObject>(S.Resources.InitCanvasLegacy);
            }

            GameObject root;

            if (prefab != null)
            {
                root = Instantiate(prefab);
            }
            else
            {
                // Fallback: create a minimal root to host the component.
                root = new GameObject(S.Resources.FallbackRootName);
                root.AddComponent<Canvas>();
                SGLogger.LogWarning(S.Errors.PrefabMissingWarn);
            }

            // Ensure an initializer component exists on the root.
            if (root.GetComponent<LauncherInitializer>() == null)
            {
                root.AddComponent<LauncherInitializer>();
            }

            DontDestroyOnLoad(root);
            _bootstrapped = true;
        }

        #endregion

        #region Unity Lifecycle
        // ─────────────────────────────────────────────────────────────────────
        // Unity Lifecycle
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Resolves required services from the locator on Awake.
        /// </summary>
        private void Awake()
        {
            _launcherInterops = HMSLocator.Get<HMSLauncherInteropsService>();
            _hmsAuth = HMSLocator.Get<HMSAuth>();
        }

        /// <summary>
        /// Starts the asynchronous initialization routine.
        /// </summary>
        private void Start()
        {
            InitializationRoutine().Forget();
        }

        /// <summary>
        /// Ensures the TCS is resolved if the object is destroyed prematurely.
        /// </summary>
        private void OnDestroy()
        {
            // Try cancel; harmless if already completed.
            _initUts?.TrySetCanceled();
        }

        #endregion

        #region Initialization Pipeline
        // ─────────────────────────────────────────────────────────────────────
        // Initialization Pipeline
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Orchestrates connection to the launcher, authentication fetch,
        /// readiness signal, and overlay teardown.
        /// </summary>
        private async UniTask InitializationRoutine()
        {
            SGLogger.Log(S.Status.Boot);
            var runtimeInfo = HMSRuntimeInfo.Get();

            try
            {
                await ConnectToLauncher();
                await InitializeAuth();

                // Signal readiness to the launcher.
                _launcherInterops.Interops.Socket.Emit(S.Events.ClientReady);

                // Complete the public UniTask and release the source.
                _initUts?.TrySetResult(true);
                _initUts = null;

                if (!runtimeInfo.IsEditorRuntime)
                {
                    await Success();
                }

                Time.timeScale = 1f;

                // Remove the overlay and allow GC to reclaim UI resources.
                Destroy(gameObject);
                GC.Collect();
            }
            catch (Exception ex)
            {
                SGLogger.LogException(ex);
                Failed(ex.Message);

                // Mark failure so other systems can react.
                _initUts?.TrySetException(ex);
            }
        }

        /// <summary>
        /// Connects to the external launcher using interop services.
        /// Includes Editor-mode dummy socket setup when applicable.
        /// </summary>
        /// <returns>A completed UniTask when connected.</returns>
        private async UniTask ConnectToLauncher()
        {
            try
            {
                SetStatus(S.Status.Connecting);

                // Await directly to support UnityEngine.Awaitable-based APIs
                // without converting to Task (prevents CS1503 mismatches).
                await _launcherInterops.Init();

                HandleEditorRuntime();
            }
            catch (Exception e)
            {
                SGLogger.LogException(e);
                throw new Exception(S.Errors.ConnectFailed);
            }
        }

        /// <summary>
        /// Retrieves HMS authentication data from the launcher via
        /// a Socket.IO ack-based request and registers it locally.
        /// </summary>
        /// <returns>A completed UniTask when authentication is registered.</returns>
        private async UniTask InitializeAuth()
        {
            try
            {
                SetStatus(S.Status.FetchingAuth);

                // EmitWithAck returns a UnityEngine.Awaitable&lt;T&gt; in this
                // environment. Await directly to avoid Task conversions.
                var authData =
                    await _launcherInterops.Interops.Socket
                        .EmitWithAck<HMSAuthData>(S.Events.AuthGetHmsData);

                _hmsAuth.RegisterAuthData(authData);
                SGLogger.Log(string.Format(S.Logs.GotAuthData, authData.user.username));
            }
            catch (Exception e)
            {
                SGLogger.LogException(e);
                throw new Exception(S.Errors.AuthFetchFailed);
            }
        }

        /// <summary>
        /// Displays a brief success message before the overlay is removed.
        /// Skipped in Editor runtime to avoid slowing iteration.
        /// </summary>
        /// <returns>A completed UniTask after the delay.</returns>
        private async UniTask Success()
        {
            SetStatus(S.Status.Success);
            await UniTask.Delay(SuccessDelayMs, DelayType.DeltaTime, PlayerLoopTiming.Update);
        }

        /// <summary>
        /// Displays a failure message and disables the loading indicator.
        /// </summary>
        /// <param name="message">
        /// Optional user-facing error message.
        /// </param>
        private void Failed(string message = null)
        {
            var finalMessage =
                string.IsNullOrWhiteSpace(message) ? S.Status.Failed : message;

            SetStatus(finalMessage);

            if (_textMeshPro != null)
            {
                var rect = _textMeshPro.GetComponent<RectTransform>();
                if (rect != null) rect.anchoredPosition = Vector2.zero;
            }

            if (_loadingEffect != null) _loadingEffect.SetActive(false);
        }

        #endregion

        #region Editor Runtime (Dummy Socket)
        // ─────────────────────────────────────────────────────────────────────
        // Editor Runtime (Dummy Socket)
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// In Editor runtime, configures a dummy socket so game flows can
        /// be exercised without the external launcher running.
        /// </summary>
        private void HandleEditorRuntime()
        {
            var runtimeInfo = HMSRuntimeInfo.Get();
            if (runtimeInfo.Profile.RuntimeMode != HMSRuntimeMode.Editor) return;

            var dummySocket =
                _launcherInterops.Interops.Socket as HMSDummyLauncherInteropsSocket;

            if (dummySocket == null)
            {
                SGLogger.LogWarning(S.Errors.DummyMissingWarn);
                return;
            }

            dummySocket.Init();

            dummySocket.RegisterAckEmissionHandler(S.Events.AuthGetHmsData, () =>
            {
                return new HMSAuthData
                {
                    access_token = S.Dummy.Token,
                    refresh_token = S.Dummy.RefreshToken,
                    user = new HMSAuthenticatedUser
                    {
                        id = S.Dummy.UserId,
                        email = S.Dummy.Email,
                        username = S.Dummy.Username,
                    }
                };
            });

            dummySocket.RegisterEmissionHandler(S.Events.ClientReady, () =>
            {
                SGLogger.Log(S.Logs.DummyClientReady);
            });

            dummySocket.On(S.Events.Test, OnTestEvent);
            dummySocket.TriggerEvent(S.Events.Test, new TestEvent
            {
                authData = new HMSAuthData
                {
                    access_token = S.Dummy.Token,
                    refresh_token = S.Dummy.RefreshToken,
                    user = new HMSAuthenticatedUser
                    {
                        id = S.Dummy.UserId,
                        email = S.Dummy.Email,
                        username = S.Dummy.Username,
                    }
                }
            });
        }

        /// <summary>
        /// Handler for the dummy 'test' event; logs the received token to
        /// verify the ack/emit wiring in Editor mode.
        /// </summary>
        /// <param name="response">Socket.IO response wrapper.</param>
        private void OnTestEvent(SocketIOResponse response)
        {
            var data = response.GetValue<TestEvent>();
            SGLogger.Log(string.Format(S.Logs.DummyTestReceived,
                data.authData.access_token));
        }

        #endregion

        #region UI Helpers
        // ─────────────────────────────────────────────────────────────────────
        // UI Helpers
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Updates the on-screen status text when the TextMeshPro reference
        /// is available; otherwise logs a message.
        /// </summary>
        /// <param name="message">Status message to display.</param>
        private void SetStatus(string message)
        {
            if (_textMeshPro != null)
            {
                _textMeshPro.text = message;
            }
            else
            {
                SGLogger.Log(message);
            }
        }

        #endregion

        #region Data Types
        // ─────────────────────────────────────────────────────────────────────
        // Data Types
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Payload used in Editor 'test' event to exercise dummy flows.
        /// </summary>
        private struct TestEvent
        {
            /// <summary>
            /// Authentication data echoed by the dummy socket.
            /// </summary>
            public HMSAuthData authData;
        }

        #endregion

        #region Strings (Magic Strings Container)
        // ─────────────────────────────────────────────────────────────────────
        // Strings (Magic Strings Container)
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Centralizes all string literals used across this component.
        /// Prevents scattering of "magic strings" throughout the codebase.
        /// </summary>
        private static class S
        {
            internal static class Resources
            {
                internal const string InitCanvasPrimary = "Prefabs/InitializationCanvas";
                internal const string InitCanvasLegacy = "InitializationCanvas";
                internal const string FallbackRootName =
                    "LauncherInitializationRoot";
            }

            internal static class Status
            {
                internal const string Boot = "Initializing client...";
                internal const string Connecting =
                    "Connecting to Launcher...";
                internal const string FetchingAuth =
                    "Fetching authentication data...";
                internal const string Success =
                    "Success! Preparing game scene...";
                internal const string Failed = "Failed to initialize";
            }

            internal static class Events
            {
                internal const string ClientReady = "sg-client.ready";
                internal const string AuthGetHmsData = "auth.get-hms-data";
                internal const string Test = "test";
            }

            internal static class Errors
            {
                internal const string ConnectFailed =
                    "Failed connecting to launcher";
                internal const string AuthFetchFailed =
                    "Failed to retrieve authentication data";
                internal const string PrefabMissingWarn =
                    "Initialization prefab not found. Created a minimal " +
                    "fallback object.";
                internal const string DummyMissingWarn =
                    "Editor runtime detected, but dummy socket is not " +
                    "available.";
            }

            internal static class Logs
            {
                internal const string GotAuthData = "Got auth data: {0}";
                internal const string DummyClientReady =
                    "sg-client.ready emitted from dummy socket";
                internal const string DummyTestReceived =
                    "Received test event: {0}";
            }

            internal static class Dummy
            {
                internal const string Token = "dummy-token";
                internal const string RefreshToken = "dummy-refresh-token";
                internal const int UserId = 1;
                internal const string Email = "gMnV9@example.com";
                internal const string Username = "dummy-user";
            }
        }

        #endregion
    }
}
