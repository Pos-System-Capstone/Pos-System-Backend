using System.Security.Cryptography;
using Pos_System.API.Payload.Response.User;

namespace Pos_System.API.Utils
{
    public class EnCodeBase64
    {
        public static string EncodeBase64User(string phone, string brandCode)
        {
            // mã hoá QRCode bằng userId và ngày hiện tại
            var currentTime = TimeUtils.GetCurrentSEATime();
            //var currentTimeStamp = TimeUtils.GetTimestamp(currentTime);
            var qrCode = brandCode + "_" + phone + "_" + currentTime;
            //Encode to Base64
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(qrCode);
            var base64 = Convert.ToBase64String(plainTextBytes);
            return base64;
        }

        public static DecodeBase64Response DecodeBase64Response(string base64)
        {
            //Decode from Base64
            var base64EncodedBytes = Convert.FromBase64String(base64);
            var qrCode = System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
            var qrCodeSplit = qrCode.Split("_");
            var brandCode = qrCodeSplit[0];
            var phone = qrCodeSplit[1];
            var currentTime = DateTime.Parse(qrCodeSplit[2]);
            var response = new DecodeBase64Response
            {
                BrandCode = brandCode,
                Phone = phone,
                CurrentTime = currentTime
            };
            return response;
        }

        public static string GenerateHmacSha256(string data, string secretKey)
        {
            // Convert the data and secret key to byte arrays
            var dataBytes = System.Text.Encoding.UTF8.GetBytes(data);
            var keyBytes = System.Text.Encoding.UTF8.GetBytes(secretKey);

            // Create an instance of HMACSHA256 with the secret key
            using HMACSHA256 hmac = new HMACSHA256(keyBytes);
            // Compute the hash for the data
            var hashBytes = hmac.ComputeHash(dataBytes);

            // Convert the hash to a hexadecimal string
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }
    }
}