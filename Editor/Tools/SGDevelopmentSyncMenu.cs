using UnityEditor;
using SGUnitySDK.Editor.Core.UseCases;
using SGUnitySDK.Editor.Core.Singletons;
using SGUnitySDK.Editor.Infrastructure;
using UnityEngine;

/*
 * Editor menu helpers to run synchronization and reset the local
 * DevelopmentProcess for testing and troubleshooting.
 */
public static class SGDevelopmentSyncMenu
{
    [MenuItem("SG/Sync Development Process", priority = 100)]
    private static async void SyncDevelopmentProcess()
    {
        try
        {
            var usecase = EditorServiceProvider.Instance
                .GetService<SyncDevelopmentProcessWithServerUseCase>();
            bool changed = await usecase.ExecuteAsync();
            EditorUtility.DisplayDialog("SG Sync", changed ? "Sync applied changes." : "No changes required.", "OK");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"SG Sync failed: {ex.Message}");
            EditorUtility.DisplayDialog("SG Sync Error", ex.Message, "OK");
        }
    }

    [MenuItem("SG/Reset Local Development Process", priority = 101)]
    private static void ResetLocalProcess()
    {
        if (EditorUtility.DisplayDialog("Reset DevelopmentProcess", "This will reset the local DevelopmentProcess (clears current version and builds). Continue?", "Yes", "No"))
        {
            DevelopmentProcess.instance.ResetProcess();
            EditorUtility.DisplayDialog("Reset", "DevelopmentProcess reset.", "OK");
        }
    }
}
