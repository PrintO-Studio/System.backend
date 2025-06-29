using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Zorro.Data.Attributes;
using Zorro.Data.Interfaces;

namespace PrintO.Models.Products.Figurine;

public class FigurineReference : IEntity, IDataTransferObject<object>, IAddable<FigurineReference.AddForm>
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(product))]
    public int productId { get; set; }
    [AlwaysInclude]
    public Product product { get; set; } = null!;

    [IncludeWhen("INCLUDE_VARIATIONS"), InclusionDepth(1)]
    public virtual ICollection<FigurineVariation> variations { get; set; } = new List<FigurineVariation>();

    public bool AddFill(AddForm form)
    {
        productId = form.productId;

        return true;
    }

    public object MapToDTO(object? argsObject = null)
    {
        return new
        {
            Id,
            product = product.MapToDTO(argsObject),
            variations = variations.Select(v => v.MapToDTO()),
        };
    }

    public struct AddForm
    {
        public int productId { get; set; }

        public AddForm(int productId)
        {
            this.productId = productId;
        }
    }
}