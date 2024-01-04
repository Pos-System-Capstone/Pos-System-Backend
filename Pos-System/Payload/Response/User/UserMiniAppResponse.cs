namespace Pos_System.API.Payload.Response.User;

public class UserMiniAppResponse
{
    public Data Data { get; set; }
    public int Error { get; set; }
    public string Message { get; set; }
}

public class Data
{
    public string Number { get; set; }
}