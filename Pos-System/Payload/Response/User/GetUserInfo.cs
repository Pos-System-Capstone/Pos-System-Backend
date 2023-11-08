namespace Pos_System.API.Payload.Response.User;

public class GetUserInfo
{
    public GetUserInfo(Guid id, string? phoneNumber, string? fullName, string? gender, string? email)
    {
        Id = id;
        PhoneNumber = phoneNumber;
        FullName = fullName;
        Gender = gender;
        Email = email;
    }

    public Guid Id { get; set; }
    public string? PhoneNumber { get; set; }
    public string? FullName { get; set; }
    public string? Gender { get; set; }
    public string? Email { get; set; }
}