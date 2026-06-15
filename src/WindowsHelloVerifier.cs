using System;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Security.Credentials.UI;

namespace GXLightBrowser
{
    internal static class WindowsHelloVerifier
    {
        public static async Task<bool> VerifyAsync(string reason)
        {
            try
            {
                UserConsentVerifierAvailability availability = await AwaitOperation(UserConsentVerifier.CheckAvailabilityAsync());
                if (availability != UserConsentVerifierAvailability.Available)
                {
                    return false;
                }

                UserConsentVerificationResult result = await AwaitOperation(UserConsentVerifier.RequestVerificationAsync(reason));
                return result == UserConsentVerificationResult.Verified;
            }
            catch (Exception ex)
            {
                Logger.Error("Windows Hello verification failed: " + ex.Message);
                return false;
            }
        }

        private static Task<T> AwaitOperation<T>(IAsyncOperation<T> operation)
        {
            TaskCompletionSource<T> completion = new TaskCompletionSource<T>();
            operation.Completed = delegate(IAsyncOperation<T> asyncInfo, AsyncStatus status)
            {
                try
                {
                    if (status == AsyncStatus.Completed)
                    {
                        completion.TrySetResult(asyncInfo.GetResults());
                    }
                    else if (status == AsyncStatus.Canceled)
                    {
                        completion.TrySetCanceled();
                    }
                    else
                    {
                        completion.TrySetException(new InvalidOperationException("Windows Hello no completo la verificacion."));
                    }
                }
                catch (Exception ex)
                {
                    completion.TrySetException(ex);
                }
            };
            return completion.Task;
        }
    }
}
