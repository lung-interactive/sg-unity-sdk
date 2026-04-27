using System.Threading.Tasks;
using HMSUnitySDK;
using HMSUnitySDK.LauncherInteroperations;
using SocketIOClient;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SGUnitySDK
{
    public class InitializeClient : MonoBehaviour
    {
        private HMSLauncherInteropsService _launcherInterops;
        private HMSAuth _hmsAuth;
        private bool _hasRequiredServices;
        private string _requiredServicesReason;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Awake()
        {
            _hasRequiredServices = TryResolveRequiredServices(out _requiredServicesReason);
        }

        void Start()
        {
            if (!_hasRequiredServices)
            {
                var reason = string.IsNullOrWhiteSpace(_requiredServicesReason)
                    ? "Required HMS services are unavailable."
                    : _requiredServicesReason;
                Debug.LogError(reason);
                Failed(reason);
                return;
            }

            _ = InitializationRoutine();
        }

        /// <summary>
        /// Tries to resolve required HMS services without throwing exceptions.
        /// </summary>
        /// <param name="reason">Diagnostic reason when resolution fails.</param>
        /// <returns>True when all required services are available.</returns>
        private bool TryResolveRequiredServices(out string reason)
        {
            if (!HMSLocator.TryGet(out _launcherInterops, out var interopsReason))
            {
                reason =
                    $"Required HMS service {nameof(HMSLauncherInteropsService)} " +
                    $"is unavailable: {interopsReason}";
                return false;
            }

            if (!HMSLocator.TryGet(out _hmsAuth, out var authReason))
            {
                reason =
                    $"Required HMS service {nameof(HMSAuth)} is unavailable: " +
                    authReason;
                return false;
            }

            reason = string.Empty;
            return true;
        }

        private async Awaitable InitializationRoutine()
        {
            try
            {
                await ConnectToLauncher();
                await InitializeAuth();
                _launcherInterops.Interops.Socket.Emit("sg-client.ready");
                await Success();
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
                Failed(ex.Message);
            }
        }

        private async Awaitable ConnectToLauncher()
        {
            try
            {
                // _textMeshPro.text = "Connecting to Launcher...";
                await _launcherInterops.Init();
                HandleEditorRuntime();
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                throw new System.Exception("Failed connecting to launcher");
            }
        }

        private async Awaitable InitializeAuth()
        {
            try
            {
                // _textMeshPro.text = "Fetching authentication data...";
                var authData = await _launcherInterops
                    .Interops
                    .Socket
                    .EmitWithAck<HMSAuthData>("auth.get-hms-data");

                _hmsAuth.RegisterAuthData(authData);

                Debug.Log($"Got auth data: {authData.user.username}");
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                throw new System.Exception("Failed to retrieve authentication data");
            }
        }

        private async Awaitable Success()
        {
            // _textMeshPro.text = "Success! Preparing game scene...";
            var runtimeInfo = HMSRuntimeInfo.Get();
            if (!runtimeInfo.IsEditorRuntime)
            {
                await Task.Delay(2000);
            }
            SceneManager.LoadScene("Game");
        }

        private void Failed(string message = null)
        {
            var finalMessage = string.IsNullOrEmpty(message) ? "Failed to initialize" : message;
            // _textMeshPro.text = finalMessage;
        }

        private void HandleEditorRuntime()
        {
            var runtimeInfo = HMSRuntimeInfo.Get();
            if (runtimeInfo == null ||
                runtimeInfo.Profile == null ||
                runtimeInfo.Profile.RuntimeMode != HMSRuntimeMode.Editor)
            {
                return;
            }

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
                        id = 1,
                        email = "gMnV9@example.com",
                        username = "dummy-user",
                    }
                };
            });

            dummySocket.RegisterEmissionHandler("sg-client.ready", () =>
            {
                Debug.Log("sg-client.ready");
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
                        id = 1,
                        email = "gMnV9@example.com",
                        username = "dummy-user",
                    }
                }
            });
        }

        private void OnTestEvent(SocketIOResponse response)
        {
            var data = response.GetValue<TestEvent>();
            Debug.Log($"Received test event: {data.authData.access_token}");
        }

        private struct TestEvent
        {
            public HMSAuthData authData;
        }
    }
}
