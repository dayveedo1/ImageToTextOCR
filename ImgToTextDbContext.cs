using ImgToText.Data;
using Microsoft.EntityFrameworkCore;

namespace ImgToText
{
    public class ImgToTextDbContext: DbContext
    {
        private ImgToTextDbContext()
        {
                
        }

        public ImgToTextDbContext(DbContextOptions<ImgToTextDbContext> options) : base(options)
        {
            
        }

        #region EF Entities
        public DbSet<TextData> TextData { get; set; }

        #endregion

    }
}
