
using FirebaseAdmin.Auth;
using Pos_System.API.Enums;

namespace Pos_System.API.Services.Interfaces
{
  public interface IFirebaseService
  {
    Task<bool> SendMessage(string token, NotificationType type, string targetId, string title, string content);
    Task<FirebaseToken?> VerifyIdToken(string token);
  }
}