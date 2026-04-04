using System;
using System.Threading.Tasks;
using SGUnitySDK.Utils;
using UnityEngine;
using UnityEditor;

namespace SGUnitySDK.Editor.Core.Utils
{
    /// <summary>
    /// Adapter helpers to convert System.Threading.Tasks.Task into the project's Awaitable types.
    /// This allows existing Task-based implementations to be awaited from Awaitable-based code.
    /// </summary>
    public static class TaskAwaitableAdapter
    {
        /// <summary>
        /// Converts a <see cref="Task{TResult}"/> into an <c>Awaitable{TResult}</c> by
        /// wiring completion callbacks to an AwaitableCompletionSource.
        /// </summary>
        public static Awaitable<T> FromTask<T>(Task<T> task)
        {
            var cs = new AwaitableCompletionSource<T>();
            task.ContinueWith(t =>
            {
                // Ensure continuation that completes the Awaitable runs on the
                // Unity main thread. `EditorApplication.delayCall` schedules
                // the provided delegate to be invoked on the main thread.
                EditorApplication.delayCall += () =>
                {
                    if (t.IsCanceled)
                        cs.SetCanceled();
                    else if (t.IsFaulted)
                        cs.SetException(t.Exception ?? new Exception("Task faulted without exception"));
                    else
                        cs.SetResult(t.Result);
                };
            }, TaskScheduler.Default);
            return cs.Awaitable;
        }

        /// <summary>
        /// Converts a non-generic <see cref="Task"/> into an <c>Awaitable</c> by
        /// returning an Awaitable that completes when the task completes.
        /// </summary>
        public static Awaitable FromTask(Task task)
        {
            var cs = new AwaitableCompletionSource();
            task.ContinueWith(t =>
            {
                EditorApplication.delayCall += () =>
                {
                    if (t.IsCanceled)
                        cs.SetCanceled();
                    else if (t.IsFaulted)
                        cs.SetException(t.Exception ?? new Exception("Task faulted without exception"));
                    else
                        cs.SetResult();
                };
            }, TaskScheduler.Default);
            return cs.Awaitable;
        }
    }
}
