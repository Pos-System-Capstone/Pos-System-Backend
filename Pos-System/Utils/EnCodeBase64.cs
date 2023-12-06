using Pos_System.API.Constants;
using Pos_System.API.Payload.Response.User;

namespace Pos_System.API.Utils
{
    public class EnCodeBase64
    {
        public static string EncodeBase64User(Guid userId)
        {
            // mã hoá QRCode bằng userId và ngày hiện tại
            var currentTime = TimeUtils.GetCurrentSEATime();
            //var currentTimeStamp = TimeUtils.GetTimestamp(currentTime);
            var qrCode = userId + "_" + currentTime;
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
            var userId = Guid.Parse(qrCodeSplit[0]);
            var currentTime = DateTime.Parse(qrCodeSplit[1]);
            var response = new DecodeBase64Response
            {
                UserId = userId,
                CurrentTime = currentTime
            };
            return response;
        }
    }
}
