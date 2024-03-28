using Pos_System.API.Enums;
using Pos_System.API.Utils;

namespace Pos_System.API.Payload.Response.Stores;

public class GetStoreResponse
{
    public Guid Id { get; set; }
    public Guid BrandId { get; set; }
    public string Name { get; set; }
    public string ShortName { get; set; }
    public string Code { get; set; }
    public string Email { get; set; }
    public string? Address { get; set; }
    public string? LocationNearby { get; set; }
    public StoreStatus Status { get; set; }
    public string? WifiName { get; set; }
    public string? WifiPassword { get; set; }
    public string? Lat { get; set; }
    public string? Long { get; set; }

    public GetStoreResponse(Guid id, Guid brandId, string name, string shortname, string code, string email,
        string address, string locationNearby,
        string status, string wifiName, string wifiPassword, string lat, string lon)
    {
        Id = id;
        BrandId = brandId;
        Name = name;
        ShortName = shortname;
        Code = code;
        Email = email;
        Address = address;
        LocationNearby = locationNearby;
        Status = EnumUtil.ParseEnum<StoreStatus>(status);
        WifiName = wifiName;
        WifiPassword = wifiPassword;
        Lat = lat;
        Long = lon;
    }
}