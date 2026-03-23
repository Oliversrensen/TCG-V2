using UnityEngine;

namespace TcgClient
{
    /// <summary>
    /// Store API base URL and Neon Auth URL. Create via Assets > Create > TCG > Api Settings.
    /// </summary>
    [CreateAssetMenu(fileName = "TcgApiSettings", menuName = "TCG/Api Settings")]
    public class TcgApiSettings : ScriptableObject
    {
        [Header("API")]
        public string ApiBaseUrl = "http://localhost:5000";

        [Header("Neon Auth")]
        public string NeonAuthUrl = "";
    }
}
