namespace Pos_System.API.Payload.Response.User;

public class CheckMemberResponse
{
    public string Message { get; set; } = null!;
    public string SignInMethod { get; set; } = null!;
}