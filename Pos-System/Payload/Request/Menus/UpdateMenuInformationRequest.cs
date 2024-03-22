using System.ComponentModel.DataAnnotations;
using Pos_System.API.Enums;

namespace Pos_System.API.Payload.Request.Menus
{
    public class UpdateMenuInformationRequest
    {
        public int? Priority { get; set; }
        public int? DateFilter { get; set; }

        [Range(minimum: 0, maximum: 1439, ErrorMessage = "Thời gian bắt đầu vượt quá số quy định (0 - 1439)")]
        public int? StartTime { get; set; }

        [Range(minimum: 0, maximum: 1439, ErrorMessage = "Thời gian kết thúc vượt quá số quy định (0 - 1439)")]
        public int? EndTime { get; set; }

        public MenuStatus? Status { get; set; }
    }
}