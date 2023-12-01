using Pos_System.Domain.Models;

namespace Pos_System.API.Payload.Response.Variant
{
    public class GetListVariantResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Status { get; set; }
        public ICollection<VariantOption> Options { get; set; }

        public GetListVariantResponse(Guid id, string name, string status, ICollection<VariantOption> options)
        {
            Id = id;
            Name = name;
            Status = status;
            Options = options;  
        }
    }
}
