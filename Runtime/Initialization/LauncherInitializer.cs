using System.Threading.Tasks;
using HMSUnitySDK;
using HMSUnitySDK.LauncherInteroperations;
using SGUnitySDK;
using SocketIOClient;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;

namespace SGUnitySDK
{
    public class LauncherInitializer : MonoBehaviour
    {
        private static TaskCompletionSource<bool> _initializationTcs;

        public static Task<bool> Init => _initializationTcs?.Task ?? Task.FromResult(true);

        [SerializeField] private TextMeshProUGUI _textMeshPro;
        [SerializeField] private GameObject _loadingEffect;

        private HMSLauncherInteropsService _launcherInterops;
        private HMSAuth _hmsAuth;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RuntimeInitialize()
        {
            // Cria um GameObject para hospedar o ClientInitializer
            var prefab = Resources.Load<GameObject>("IntializationCanvas");
            var go = Instantiate(prefab);
            DontDestroyOnLoad(go);

            // Inicializa o TaskCompletionSource
            _initializationTcs = new TaskCompletionSource<bool>();
        }

        void Awake()
        {
            _launcherInterops = HMSLocator.Get<HMSLauncherInteropsService>();
            _hmsAuth = HMSLocator.Get<HMSAuth>();
        }

        void Start()
        {
            _ = InitializationRoutine();
        }

        private async Task InitializationRoutine()
        {
            SGLogger.Log("Initializing client...");
            var runtimeInfo = HMSRuntimeInfo.Get();

            try
            {
                await ConnectToLauncher();
                await InitializeAuth();
                _launcherInterops.Interops.Socket.Emit("sg-client.ready");

                _initializationTcs?.TrySetResult(true);
                _initializationTcs = null;

                if (!runtimeInfo.IsEditorRuntime)
                {
                    await Success();
                }

                Time.timeScale = 1;
                Destroy(gameObject);
                System.GC.Collect();
            }
            catch (System.Exception ex)
            {
                SGLogger.LogException(ex);
                Failed(ex.Message);

                // Completa com erro
                _initializationTcs?.TrySetException(ex);
            }
        }

        private async Task ConnectToLauncher()
        {
            try
            {
                _textMeshPro.text = "Connecting to Launcher...";
                await _launcherInterops.Init();
                HandleEditorRuntime();
            }
            catch (System.Exception e)
            {
                SGLogger.LogException(e);
                throw new System.Exception("Failed connecting to launcher");
            }
        }

        private async Task InitializeAuth()
        {
            try
            {
                _textMeshPro.text = "Fetching authentication data...";
                var authData = await _launcherInterops
                    .Interops
                    .Socket
                    .EmitWithAck<HMSAuthData>("auth.get-hms-data");

                _hmsAuth.RegisterAuthData(authData);

                SGLogger.Log($"Got auth data: {authData.user.username}");
            }
            catch (System.Exception e)
            {
                SGLogger.LogException(e);
                throw new System.Exception("Failed to retrieve authentication data");
            }
        }

        private async Task Success()
        {
            _textMeshPro.text = "Success! Preparing game scene...";
            var runtimeInfo = HMSRuntimeInfo.Get();
            await Task.Delay(2000);
        }

        private void Failed(string message = null)
        {
            var finalMessage = string.IsNullOrEmpty(message) ? "Failed to initialize" : message;
            _textMeshPro.text = finalMessage;
            var rect = _textMeshPro.GetComponent<RectTransform>();
            rect.anchoredPosition = Vector2.zero;
            _loadingEffect.SetActive(false);
        }

        private void HandleEditorRuntime()
        {
            var runtimeInfo = HMSRuntimeInfo.Get();
            if (runtimeInfo.Profile.RuntimeMode != HMSRuntimeMode.Editor) return;
            var dummySocket = _launcherInterops.Interops.Socket as HMSDummyLauncherInteropsSocket;

            dummySocket.Init();
            dummySocket.RegisterAckEmissionHandler("auth.get-hms-data", () =>
            {
                return new HMSAuthData()
                {
                    access_token = "dummy-token",
                    refresh_token = "dummy-refresh-token",
                    user = new HMSAuthenticatedUser()
                    {
                        id = 300,
                        email = "gMnV9@example.com",
                        username = "dummy-user",
                    }
                };
            });

            dummySocket.RegisterEmissionHandler("sg-client.ready", () =>
            {
                SGLogger.Log("sg-client.ready emitted from dummy socket");
            });

            dummySocket.On("test", OnTestEvent);
            dummySocket.TriggerEvent("test", new TestEvent()
            {
                authData = new HMSAuthData()
                {
                    access_token = "dummy-token",
                    refresh_token = "dummy-refresh-token",
                    user = new HMSAuthenticatedUser()
                    {
                        id = 300,
                        email = "gMnV9@example.com",
                        username = "dummy-user",
                    }
                }
            });
        }

        private void OnTestEvent(SocketIOResponse response)
        {
            var data = response.GetValue<TestEvent>();
            SGLogger.Log($"Received test event: {data.authData.access_token}");
        }

        private struct TestEvent
        {
            public HMSAuthData authData;
        }
    }
}