using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Zorro.Data.Interfaces;
using static PrintO.Models.ImageReference;

namespace PrintO.Models;

public class ImageReference : IEntity, IDataTransferObject<object>, IAddable<AddForm>, IUpdateable<UpdateForm>
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(file))]
    public int fileId { get; set; }
    public File file { get; set; } = null!;

    [ForeignKey(nameof(product))]
    public int productId { get; set; }
    [InverseProperty(nameof(Products.Product.images))]
    public Products.Product product { get; set; } = null!;

    public int index { get; set; }

    public bool AddFill(AddForm form)
    {
        fileId = form.fileId;
        index = form.index;
        productId = form.productId;

        return true;
    }

    public bool UpdateFill(UpdateForm form)
    {
        index = form.index;
        fileId = form.fileId;

        return true;
    }

    public object MapToDTO(object? argsObject = null)
    {
        return new
        {
            Id,
            fileId,
            index,
        };
    }

    public struct AddForm
    {
        public int fileId { get; set; }
        public int productId { get; set; }
        public int index { get; set; }

        public AddForm(int fileId, int productId, int index)
        {
            this.fileId = fileId;
            this.productId = productId;
            this.index = index;
        }
    }

    public struct UpdateForm
    {
        public int index { get; set; }
        public int fileId { get; set; }

        public UpdateForm(int index, int fileId)
        {
            this.index = index;
            this.fileId = fileId;
        }
    }
}