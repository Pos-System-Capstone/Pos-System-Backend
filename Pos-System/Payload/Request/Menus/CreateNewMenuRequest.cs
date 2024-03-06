using System.ComponentModel.DataAnnotations;

namespace Pos_System.API.Payload.Request.Menus
{
    public class CreateNewMenuRequest
    {
        [MaxLength(20,ErrorMessage = "Tên Menu phải dưới 20 kí tự")]
        public string Code { get; set; }
        public int Priority { get; set; }
        public int DateFilter { get; set; }
        [Range(minimum: 0,maximum: 1439, ErrorMessage = "Thời gian bắt đầu vượt quá số quy định (0 - 1439)")]
		public int StartTime { get; set; }
		[Range(minimum: 0, maximum: 1439, ErrorMessage = "Thời gian kết thúc vượt quá số quy định (0 - 1439)")]
        public int EndTime { get; set; }
		public bool IsBaseMenu { get; set; }
        public bool IsUseBaseMenu { get; set; }
    }
}
