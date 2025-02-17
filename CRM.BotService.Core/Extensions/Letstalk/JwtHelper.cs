namespace CRM.BotService.Core.Extensions.Letstalk
{
    using System.Collections.Generic;
    using System.Text;
    using Jose;

    public static class JwtHelper
    {
        public static string Decode(string secretKey, string token)
        {
            var signingKey = Encoding.UTF8.GetBytes(secretKey);

            var encodedPayload = JWT.Decode(
                token,
                signingKey,
                JwsAlgorithm.HS512);

            return encodedPayload;
        }

        public static string Encode(string secretKey, IDictionary<string, object> data)
        {
            var signingKey = Encoding.UTF8.GetBytes(secretKey);

            var encodedPayload = JWT.Encode(
                data,
                signingKey,
                JwsAlgorithm.HS512);

            return encodedPayload;
        }
    }
}