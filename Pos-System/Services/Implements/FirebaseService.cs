using FirebaseAdmin;
using FirebaseAdmin.Auth;
using FirebaseAdmin.Messaging;
using Pos_System.API.Enums;
using Pos_System.API.Services.Interfaces;

namespace Pos_System.API.Services.Implements
{
  public class FirebaseService :  IFirebaseService
  {
    private IConfiguration _config;
    FirebaseApp _firebaseApp;
    FirebaseAuth _firebaseAuth;
    FirebaseMessaging _firebaseMessaging;

    public FirebaseService(IConfiguration config)
    {
      _config = config;
      _firebaseApp = FirebaseApp.GetInstance("[DEFAULT]");
      _firebaseAuth = FirebaseAuth.GetAuth(_firebaseApp);
      _firebaseMessaging = FirebaseMessaging.GetMessaging(_firebaseApp);
    }

    public async Task<FirebaseToken?> VerifyIdToken(string token)
    {
      FirebaseToken? result = null;
      try
      {
        result = await _firebaseAuth.VerifyIdTokenAsync(token);
      }
      catch
      {
        result = null;
      }
      return result;
    }

    public async Task<bool> SendMessage(string token, NotificationType type, string targetId, string title, string content)
    {
        try
        {
            Message msg = new Message()
            {
                Token = token,
                // Topic = "All",
                Data = new Dictionary<string, string>()
                {
                    { "Type", type.ToString() },
                    { "Target", targetId },
                    { "Title", title },
                    { "Content", content },
                },
                Notification = new Notification
                {
                    Title = title,
                    Body = content
                }
            };

            var result = await _firebaseMessaging.SendAsync(msg);
            return result != null;
        }
        catch
        {
            return false;
        }
    }
  }
}