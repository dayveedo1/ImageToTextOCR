using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ImgToText.Data
{
    public class TextData
    {
        [Key]
        public int Id { get; set; }
        public string? Text { get; set; }
        //Modification
        public string? Courier { get; set; }
        public string? From { get; set; }
        public string? To { get; set; }
        [Column("TrackingID")]
        public string? TrackingId { get; set; }
    }
}
