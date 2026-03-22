using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace TcgClient
{
    /// <summary>
    /// Minimal SignalR client using WebSocket. For production, consider Microsoft.AspNetCore.SignalR.Client NuGet.
    /// This scaffold shows the expected flow: connect with auth, JoinQueue, receive MatchFound, send actions.
    /// </summary>
    public class TcgSignalRClient : MonoBehaviour
    {
        [SerializeField] private TcgApiSettings? _settings;
        private string? _jwt;
        private string? _devUserId;
        private bool _connected;

        public event Action<Guid, Guid>? OnMatchFound;
        public event Action<string>? OnStateUpdate;
        public event Action<string>? OnGameOver;
        public event Action<string>? OnError;

        public void Configure(TcgApiSettings settings, string? jwt, string? devUserId)
        {
            _settings = settings;
            _jwt = jwt;
            _devUserId = devUserId;
        }

        public void JoinQueue(Guid deckId)
        {
            // In a full implementation, use SignalR .NET client:
            // await _hub.InvokeAsync("JoinQueue", deckId);
            Debug.Log($"[TcgSignalR] JoinQueue({deckId}) - implement with SignalR client");
        }

        public void RejoinMatch(Guid matchId)
        {
            Debug.Log($"[TcgSignalR] RejoinMatch({matchId}) - implement with SignalR client");
        }

        public void PlayCard(Guid matchId, Guid cardId, Guid? targetId)
        {
            Debug.Log($"[TcgSignalR] PlayCard - implement with SignalR client");
        }

        public void EndTurn(Guid matchId)
        {
            Debug.Log($"[TcgSignalR] EndTurn - implement with SignalR client");
        }

        /// <summary>
        /// Build WebSocket URL with auth. For JWT: ?access_token=xxx. For dev: ?user_id=xxx
        /// </summary>
        public string GetHubUrl()
        {
            if (_settings == null) return "";
            var baseUrl = _settings.ApiBaseUrl.TrimEnd('/').Replace("http://", "ws://").Replace("https://", "wss://");
            var url = baseUrl + "/hubs/game";
            if (!string.IsNullOrEmpty(_jwt))
                url += "?access_token=" + Uri.EscapeDataString(_jwt);
            else if (!string.IsNullOrEmpty(_devUserId))
                url += "?user_id=" + Uri.EscapeDataString(_devUserId);
            return url;
        }
    }
}
