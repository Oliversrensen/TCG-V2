using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace TcgClient
{
    /// <summary>
    /// REST client for TCG API. Uses UnityWebRequest.
    /// </summary>
    public class TcgRestClient
    {
        private readonly TcgApiSettings _settings;
        private string? _jwt;
        private string? _devUserId;

        public TcgRestClient(TcgApiSettings settings)
        {
            _settings = settings;
        }

        public void SetJwt(string jwt) => _jwt = jwt;
        public void SetDevUserId(string userId) => _devUserId = userId;

        public IEnumerator Get(string path, Action<string> onSuccess, Action<string> onError)
        {
            using var req = CreateRequest(path, "GET", null);
            yield return req.SendWebRequest();
            HandleResponse(req, onSuccess, onError);
        }

        public IEnumerator Post(string path, object? body, Action<string> onSuccess, Action<string> onError)
        {
            var json = body != null ? JsonUtility.ToJson(body) : "{}";
            using var req = CreateRequest(path, "POST", json);
            yield return req.SendWebRequest();
            HandleResponse(req, onSuccess, onError);
        }

        public IEnumerator Put(string path, object body, Action<string> onSuccess, Action<string> onError)
        {
            var json = body != null ? JsonUtility.ToJson(body) : "{}";
            using var req = CreateRequest(path, "PUT", json);
            yield return req.SendWebRequest();
            HandleResponse(req, onSuccess, onError);
        }

        public IEnumerator Delete(string path, Action onSuccess, Action<string> onError)
        {
            using var req = CreateRequest(path, "DELETE", null);
            yield return req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success && req.responseCode >= 200 && req.responseCode < 300)
                onSuccess?.Invoke();
            else
                onError?.Invoke(req.error ?? req.downloadHandler?.text ?? "Unknown error");
        }

        private UnityWebRequest CreateRequest(string path, string method, string? body)
        {
            var url = _settings.ApiBaseUrl.TrimEnd('/') + path;
            var req = new UnityWebRequest(url, method);

            if (!string.IsNullOrEmpty(_jwt))
                req.SetRequestHeader("Authorization", "Bearer " + _jwt);
            else if (!string.IsNullOrEmpty(_devUserId))
                req.SetRequestHeader("X-User-Id", _devUserId);

            req.downloadHandler = new DownloadHandlerBuffer();
            if (!string.IsNullOrEmpty(body))
            {
                req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
                req.SetRequestHeader("Content-Type", "application/json");
            }

            return req;
        }

        private static void HandleResponse(UnityWebRequest req, Action<string> onSuccess, Action<string> onError)
        {
            if (req.result == UnityWebRequest.Result.Success && req.responseCode >= 200 && req.responseCode < 300)
                onSuccess?.Invoke(req.downloadHandler?.text ?? "{}");
            else
                onError?.Invoke(req.error ?? req.downloadHandler?.text ?? "Unknown error");
        }
    }
}
