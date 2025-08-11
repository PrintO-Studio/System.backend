using PrintO.Models.Products;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Zorro.Data.Interfaces;
using static PrintO.Models.IntegrationTask;

namespace PrintO.Models;

public class IntegrationTask : IEntity, IDTO<object>, IAddable<AddForm>, IUpdateable<UpdateForm>, IUpdateable<AppendLogsForm>
{
    [Key]
    public int Id { get; set; }

    public bool inProgress { get; set; }
    public bool? success { get; set; }
    public string logs { get; set; } = string.Empty;
    public uint? version { get; set; } = null;

    [ForeignKey(nameof(executionUser))]
    public int exectionUserId { get; set; }
    public User executionUser { get; set; } = null!;

    public DateTime executionDate { get; set; }

    [ForeignKey(nameof(product))]
    public int productId { get; set; }
    public Product product { get; set; } = null!;

    public virtual bool AddFill(AddForm form)
    {
        exectionUserId = form.exectionUserId;
        inProgress = true;
        success = null;
        logs = string.Empty;
        productId = form.productId;
        executionDate = DateTime.UtcNow;

        return true;
    }

    public bool UpdateFill(UpdateForm form)
    {
        inProgress = false;
        success = form.success;
        version = form.version;

        return true;
    }

    public bool UpdateFill(AppendLogsForm form)
    {
        logs += form.logs;

        return true;
    }

    public object MapToDTO(Zorro.Query.HttpQueryContext context)
    {
        return new
        {
            Id,
            inProgress,
            success,
            version
            //logs,
        };
    }

    public struct AddForm
    {
        public int exectionUserId { set; get; }
        public int productId { set; get; }

        public AddForm(int exectionUserId, int productId)
        {
            this.exectionUserId = exectionUserId;
            this.productId = productId;
        }
    }

    public struct UpdateForm
    {
        public bool success { get; set; }
        public uint version { get; set; }

        public UpdateForm(bool success, uint version)
        {
            this.success = success;
            this.version = version;
        }
    }

    public struct AppendLogsForm
    {
        public string logs { set; get; }

        public AppendLogsForm(string logs)
        {
            this.logs = logs;
        }
    }
}