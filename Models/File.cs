using PrintO.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Zorro.Data;
using Zorro.Data.Attributes;
using Zorro.Data.Interfaces;
using Zorro.Query;
using static PrintO.Models.File;

namespace PrintO.Models;

public class File : IEntity, IDTO<object>, IDTO<string>, IAddable<AddForm>
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(authorUser))]
    public int authorUserId { get; set; }
    [NeverInclude]
    public User authorUser { get; set; } = null!;

    [ForeignKey(nameof(product))]
    public int productId { get; set; }
    [NeverInclude]
    [InverseProperty(nameof(Products.Product.files))]
    public Products.Product product { get; set; } = null!;

    public string filePath { get; set; } = null!;
    public DateTime uploadDateTime { get; set; }
    public FileType fileType { get; set; }
    public string contentType { get; set; } = null!;
    public long length { get; set; }

    [IncludeWhen("INCLUDE_TAGS")]
    public virtual ICollection<FileTag> tags { get; set; } = new List<FileTag>();

    public bool AddFill(AddForm form)
    {
        filePath = form.filePath;
        authorUserId = form.authorUserId;
        uploadDateTime = DateTime.UtcNow;
        productId = form.productId;
        contentType = form.contentType;
        length = form.length;

        return true;
    }

    public object MapToDTO(Zorro.Query.QueryContext context)
    {
        MinIORepository minIORepo = context.GetService<MinIORepository>();
        string fullPath = minIORepo.GetFullPath(filePath);

        return new
        {
            //filePath,
            Id,
            uploadDateTime,
            fullPath,
            contentType,
            length,
            tags = tags.Select(t => t.MapToDTO(context)),
        };
    }

    string IDTO<string>.MapToDTO(QueryContext context)
    {
        MinIORepository minIORepo = context.GetService<MinIORepository>();
        return minIORepo.GetFullPath(filePath);
    }

    public struct AddForm
    {
        public string filePath { get; set; }
        public int authorUserId { get; set; }
        public int productId { get; set; }
        public string contentType { get; set; }
        public long length { get; set; }

        public AddForm(string filePath, int authorUserId, int productId, string contentType, long length)
        {
            this.filePath = filePath;
            this.authorUserId = authorUserId;
            this.productId = productId;
            this.contentType = contentType;
            this.length = length;
        }
    }
}