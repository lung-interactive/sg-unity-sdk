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

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Awake()
        {
            _launcherInterops = HMSLocator.Get<HMSLauncherInteropsService>();
            _hmsAuth = HMSLocator.Get<HMSAuth>();
        }

        void Start()
        {
            _ = InitializationRoutine();
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
            Debug.Log($"Received test event: {data.authData.access_token}");
        }

        private struct TestEvent
        {
            public HMSAuthData authData;
        }
    }
}
