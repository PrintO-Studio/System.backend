using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Zorro.Data.Attributes;
using Zorro.Data.Interfaces;
using static PrintO.Models.File;

namespace PrintO.Models;

public class File : IEntity, IDataTransferObject<object>, IAddable<AddForm>
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

    public object MapToDTO(dynamic? argsObject = null)
    {
        return new
        {
            //filePath,
            Id,
            uploadDateTime,
            argsObject?.fullPath,
            contentType,
            length
        };
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