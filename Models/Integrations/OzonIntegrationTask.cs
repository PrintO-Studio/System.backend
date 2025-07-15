using PrintO.Models.Products;
using System.ComponentModel.DataAnnotations.Schema;
using Zorro.Data.Attributes;
using Zorro.Data.Interfaces;
using static PrintO.Models.Integrations.OzonIntegrationTask;

namespace PrintO.Models.Integrations;

public class OzonIntegrationTask : IntegrationTask, IAddable<AddForm>
{
    public long taskId { get; set; }

    [ForeignKey(nameof(product))]
    public new int productId { get; set; }
    [InverseProperty(nameof(Product.ozonIntegrations))]
    [IncludeWhen("INCLUDE_PRODUCT")]
    public new Product product { get; set; } = null!;

    public bool AddFill(AddForm form)
    {
        base.AddFill(new IntegrationTask.AddForm(form.exectionUserId, form.productId));
        productId = form.productId;
        taskId = form.taskId;

        return true;
    }

    public new struct AddForm
    {
        public int exectionUserId { set; get; }
        public int productId { set; get; }
        public long taskId { set; get; }

        public AddForm(int exectionUserId, int productId, long taskId)
        {
            this.exectionUserId = exectionUserId;
            this.productId = productId;
            this.taskId = taskId;
        }
    }
}