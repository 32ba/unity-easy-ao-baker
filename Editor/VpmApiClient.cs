using System;
using System.Collections;
using UnityEngine.Networking;

namespace net._32ba.EasyAOBaker.Editor
{
    /// <summary>
    /// vpm.32ba.net の API からパッケージの最新バージョンを取得する。
    /// </summary>
    public class VpmApiClient
    {
        private const string ApiBaseUrl = "https://vpm.32ba.net/api/packages";
        private readonly string _packageId;

        public VpmApiClient(string packageId)
        {
            _packageId = packageId;
        }

        public IEnumerator GetLatestVersionCoroutine(Action<string> onComplete, Action<string> onError = null)
        {
            string url = $"{ApiBaseUrl}/{_packageId}/latest/version";

            using (var request = UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                    onComplete?.Invoke(request.downloadHandler.text.Trim());
                else
                    onError?.Invoke($"VPM API request failed: {request.error}");
            }
        }
    }
}
