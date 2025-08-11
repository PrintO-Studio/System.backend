using PrintO.Models.Products;
using System.ComponentModel.DataAnnotations.Schema;
using Zorro.Data.Attributes;
using Zorro.Data.Interfaces;
using static PrintO.Models.Integrations.WbIntegrationTask;

namespace PrintO.Models.Integrations;

public class WbIntegrationTask : IntegrationTask, IAddable<AddForm>
{
    [ForeignKey(nameof(product))]
    public new int productId { get; set; }
    [InverseProperty(nameof(Product.wbIntegrations))]
    [IncludeWhen("INCLUDE_PRODUCT")]
    public new Product product { get; set; } = null!;

    public bool AddFill(AddForm form)
    {
        base.AddFill(new IntegrationTask.AddForm(form.exectionUserId, form.productId));
        productId = form.productId;

        return true;
    }

    public new struct AddForm
    {
        public int exectionUserId { set; get; }
        public int productId { set; get; }

        public AddForm(int exectionUserId, int productId)
        {
            this.exectionUserId = exectionUserId;
            this.productId = productId;
        }
    }
}