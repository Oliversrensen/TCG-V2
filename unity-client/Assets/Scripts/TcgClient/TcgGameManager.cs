using System;
using System.Collections;
using UnityEngine;

namespace TcgClient
{
    /// <summary>
    /// Example game manager that orchestrates REST + SignalR. Attach to a GameObject.
    /// </summary>
    public class TcgGameManager : MonoBehaviour
    {
        [SerializeField] private TcgApiSettings? _settings;
        private TcgRestClient? _restClient;
        private TcgSignalRClient? _signalRClient;

        private void Awake()
        {
            if (_settings == null)
            {
                Debug.LogError("TcgApiSettings not assigned");
                return;
            }
            _restClient = new TcgRestClient(_settings);
            _signalRClient = gameObject.AddComponent<TcgSignalRClient>();
            _signalRClient.Configure(_settings, null, _settings.DevUserId);

            _signalRClient.OnMatchFound += (matchId, opponentDeckId) =>
                Debug.Log($"Match found: {matchId}, opponent deck: {opponentDeckId}");
            _signalRClient.OnStateUpdate += state => Debug.Log("StateUpdate: " + state);
            _signalRClient.OnGameOver += winner => Debug.Log("Game over, winner: " + winner);
            _signalRClient.OnError += err => Debug.LogError("SignalR error: " + err);
        }

        public void FetchCards()
        {
            if (_restClient == null) return;
            StartCoroutine(_restClient.Get("/api/cards/definitions",
                json => Debug.Log("Cards: " + json),
                err => Debug.LogError("Failed to fetch cards: " + err)));
        }

        public void JoinMatchmaking(Guid deckId)
        {
            _signalRClient?.JoinQueue(deckId);
        }
    }
}
