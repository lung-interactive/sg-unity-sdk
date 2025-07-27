using UnityEngine;

namespace SGUnitySDK.Utils
{
    public static class AwaitableUtility
    {
        public static Awaitable<TResult> FromResult<TResult>(TResult result)
        => Result<TResult>.From(result);

        static class Result<TResult>
        {
            static readonly AwaitableCompletionSource<TResult> completionSource = new();

            public static Awaitable<TResult> From(TResult result)
            {
                completionSource.SetResult(result);
                var awaitable = completionSource.Awaitable;
                completionSource.Reset();
                return awaitable;
            }
        }
    }
}