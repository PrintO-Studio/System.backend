using PrintO.Models.Products;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Zorro.Data.Attributes;
using Zorro.Data.Interfaces;
using static PrintO.Models.Note;

namespace PrintO.Models;

public class Note : IEntity, IDTO<object>, IAddable<AddForm>, IUpdateable<UpdateForm>
{
    [Key]
    public int Id { get; set; }

    [StringLength(NOTE_TEXT_MAX_LENGTH)]
    public string text { get; set; } = null!;
    public Importance importance { get; set; }

    [ForeignKey(nameof(product))]
    public int productId { get; set; }
    [NeverInclude]
    [InverseProperty(nameof(Product.notes))]
    public Product product { get; set; } = null!;

    public bool AddFill(AddForm form)
    {
        text = form.userAddForm.text;
        importance = form.userAddForm.importance;
        productId = form.productId;

        return true;
    }

    public bool UpdateFill(UpdateForm form)
    {
        if (!string.IsNullOrEmpty(form.text))
            text = form.text;
        if (form.importance.HasValue)
            importance = form.importance.Value;

        return true;
    }

    public object MapToDTO(Zorro.Query.HttpQueryContext context)
    {
        return new
        {
            Id,
            text,
            importance
        };
    }

    public const int NOTE_TEXT_MAX_LENGTH = 10000;

    public struct AddForm
    {
        public UserAddForm userAddForm { get; set; }
        public int productId { get; set; }

        public AddForm(UserAddForm userAddForm, int productId)
        {
            this.userAddForm = userAddForm;
            this.productId = productId;
        }
    }

    public struct UserAddForm
    {
        [StringLength(NOTE_TEXT_MAX_LENGTH)]
        public string text { get; set; }
        public Importance importance { get; set; }
    }

    public struct UpdateForm
    {
        [StringLength(NOTE_TEXT_MAX_LENGTH)]
        public string? text { get; set; }
        public Importance? importance { get; set; }
    }
}
