using HMSUnitySDK.LauncherInteroperations;
using SGUnitySDK.Events;
using SocketIOClient;
using UnityEngine;

namespace SGUnitySDK
{
    public class CrowdActionsHandler : HMSLauncherInteropsHandler
    {
        #region  Launcher 

        private static class InteropsNames
        {
            public static readonly string CrowdAction = "crowd.action";
        }

        public override void Deploy(HMSLauncherSocket interops)
        {
            interops.OnUnityThread(InteropsNames.CrowdAction, OnGameAction);
        }

        public override void Dispose(HMSLauncherSocket interops)
        {
            interops.Off(InteropsNames.CrowdAction);
        }

        private void OnGameAction(SocketIOResponse response)
        {
            var gameAction = response.GetValue<CrowdAction>();
            Debug.Log($"-SGUnitySDK | CrowdActionsHandler | Received crowd action: {gameAction.identifier}");

            EventBus<CrowdActionTriggered>.Raise(new CrowdActionTriggered
            {
                Action = gameAction
            });
        }

        #endregion
    }
}