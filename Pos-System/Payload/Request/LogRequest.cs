namespace Pos_System.API.Payload.Request;

using System;
using Newtonsoft.Json;

public class LogRequest
{
    [JsonProperty("id")] public int Id { get; set; }

    [JsonProperty("project_name")] public string ProjectName { get; set; }

    [JsonProperty("content")] public string Content { get; set; }

    [JsonProperty("store_id")] public long StoreId { get; set; }

    [JsonProperty("created_date")] public DateTimeOffset CreatedDate { get; set; }
}