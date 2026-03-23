using Microsoft.IdentityModel.Tokens;
using ScottBrady.IdentityModel.Crypto;
using ScottBrady.IdentityModel.Tokens;

namespace TCG.Server.Auth;

/// <summary>Loads EdDSA signing keys from Neon Auth JWKS (Microsoft.IdentityModel doesn't support EdDSA natively).</summary>
internal static class NeonEdDsaKeyLoader
{
    public static IList<SecurityKey> LoadFromJwks(string jwksJson)
    {
        var jwks = new JsonWebKeySet(jwksJson);
        var keys = new List<SecurityKey>();
        foreach (var key in jwks.Keys)
        {
            if (key.Alg != "EdDSA" || key.Crv != "Ed25519")
                continue;
            if (string.IsNullOrEmpty(key.X))
                continue;
            try
            {
                var publicKeyBytes = Base64UrlEncoder.DecodeBytes(key.X);
                var parameters = new EdDsaParameters(ExtendedSecurityAlgorithms.Curves.Ed25519)
                {
                    X = publicKeyBytes
                };
                var edDsa = EdDsa.Create(parameters);
                keys.Add(new EdDsaSecurityKey(edDsa));
            }
            catch
            {
                // Skip keys that fail to load
            }
        }
        return keys;
    }
}
