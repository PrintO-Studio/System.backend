using Microsoft.EntityFrameworkCore.Query;
using PrintO.Models.Integrations;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Zorro.Data;
using Zorro.Data.Attributes;
using Zorro.Data.Interfaces;
using static PrintO.Models.Products.Product;

namespace PrintO.Models.Products;

public class Product : IEntity, IDTO<object>, IDTO<ProductReviewDTO>, IAddable<AddForm>, IUpdateable<UpdateForm>
{
    [Key]
    public int Id { get; set; }

    [StringLength(PRODUCT_SKU_MAX_LENGTH, MinimumLength = 1)]
    public string SKU { get; set; } = null!;
    [StringLength(PRODUCT_NAME_MAX_LENGTH, MinimumLength = 1)]
    public string name { get; set; } = null!;
    [StringLength(PRODUCT_SERIES_MAX_LENGTH)]
    public string? series { get; set; }
    [StringLength(PRODUCT_DESCRIPTION_MAX_LENGTH)]
    public string description { get; set; } = null!;
    public bool explicitContent { get; set; } = false;

    [ForeignKey(nameof(store))]
    public int storeId { get; set; }
    [IncludeWhen("INCLUDE_STORE"), InclusionDepth(1)]
    [InverseProperty(nameof(Store.products))]
    public Store store { get; set; } = null!;

    [IncludeWhen("INCLUDE_FILES")]
    public virtual ICollection<File> files { get; set; } = new List<File>();
    [IncludeWhen("INCLUDE_IMAGES")]
    public virtual ICollection<ImageReference> images { get; set; } = new List<ImageReference>();
    [IncludeWhen("INCLUDE_NOTES")]
    public virtual ICollection<Note> notes { get; set; } = new List<Note>();

    public uint version { get; set; } = 1;

    [AlwaysInclude]
    public virtual ICollection<OzonIntegrationTask> ozonIntegrations { get; set; } = new List<OzonIntegrationTask>();

    //public uint? wildberriesIntegrationVersion { get; set; }
    //public uint? yandexIntegrationVersion { get; set; }

    public bool AddFill(AddForm form)
    {
        SKU = form.userAddForm.SKU;
        name = form.userAddForm.name;
        series = form.userAddForm.series;
        description = form.userAddForm.description;
        storeId = form.storeId;
        explicitContent = form.userAddForm.explicitContent ?? false;

        return true;
    }

    public bool UpdateFill(UpdateForm form)
    {
        if (!string.IsNullOrEmpty(form.name))
            name = form.name;
        series = form.series;
        if (!string.IsNullOrEmpty(form.description))
            description = form.description;
        if (form.explicitContent.HasValue)
            explicitContent = form.explicitContent.Value;

        version++;

        return true;
    }

    object IDTO<object>.MapToDTO(Zorro.Query.QueryContext context)
    {
        IEnumerable<object>? files = null;
        
        Func<File, object> fileBuilder = (f) =>
        {
            return f.MapToDTO(context);
        };
        files = this.files.Select(fileBuilder);

        var ozonLastTask = ozonIntegrations.Count > 0 ? 
            ozonIntegrations.OrderBy(i => i.executionDate).Last()?.MapToDTO(context) : 
            null;

        return new
        {
            Id,
            SKU,
            name,
            series,
            description,
            store = store?.MapToDTO(context),
            explicitContent,
            files,
            images = images.OrderBy(i => i.index).Select(i => i.MapToDTO(context)),
            versions = new
            {
                version,
                ozon = new
                {
                    lastTask = ozonLastTask
                },
                wildberries = new
                {

                },
                yandex = new
                {

                }
            },
            notes = notes.Select(n => n.MapToDTO(context)),
        };
    }

    ProductReviewDTO IDTO<ProductReviewDTO>.MapToDTO(Zorro.Query.QueryContext context)
    {
        ImageReference? primaryImageRef = images.OrderBy(i => i.index).FirstOrDefault();
        object? primaryImage = null;
        if (primaryImageRef is not null)
        {
            var primaryImageFile = files.FirstOrDefault(f => f.Id == primaryImageRef.fileId);

            if (primaryImageFile is not null)
            {
                primaryImage = primaryImageFile.MapToDTO(context);
            }
        }

        var ozonLastTask = ozonIntegrations.Count > 0 ?
            ozonIntegrations.OrderBy(i => i.executionDate).Last()?.MapToDTO(context) :
            null;

        return new ProductReviewDTO()
        {
            id = Id,
            SKU = SKU,
            name = name,
            series = series,
            primaryImage = primaryImage,
            versions = new
            {
                version,
                ozon = new
                {
                    lastTask = ozonLastTask,
                },
                wildberries = new
                {

                },
                yandex = new
                {

                }
            }
        };
    }

    public const int PRODUCT_SKU_MAX_LENGTH = 20;
    public const int PRODUCT_NAME_MAX_LENGTH = 100;
    public const int PRODUCT_DESCRIPTION_MAX_LENGTH = 5000;
    public const int PRODUCT_SERIES_MAX_LENGTH = 50;

    public struct AddForm
    {
        public UserAddForm userAddForm { get; set; }
        public int storeId { get; set; }

        public AddForm(UserAddForm userAddForm, int storeId)
        {
            this.userAddForm = userAddForm;
            this.storeId = storeId;
        }
    }

    public struct UserAddForm
    {
        [StringLength(PRODUCT_SKU_MAX_LENGTH, MinimumLength = 1)]
        public string SKU { get; set; }
        [StringLength(PRODUCT_NAME_MAX_LENGTH, MinimumLength = 1)]
        public string name { get; set; }
        [StringLength(PRODUCT_SERIES_MAX_LENGTH)]
        public string? series { get; set; }
        [StringLength(PRODUCT_DESCRIPTION_MAX_LENGTH)]
        public string description { get; set; }
        public bool? explicitContent { get; set; }
    }

    public struct UpdateForm
    {
        [StringLength(PRODUCT_NAME_MAX_LENGTH, MinimumLength = 1)]
        public string? name { get; set; }
        [StringLength(PRODUCT_SERIES_MAX_LENGTH)]
        public string? series { get; set; }
        [StringLength(PRODUCT_DESCRIPTION_MAX_LENGTH)]
        public string? description { get; set; }
        public bool? explicitContent { get; set; }
    }

    public struct ProductReviewDTO
    {
        public int id { get; set; }
        public string SKU { get; set; }
        public string name { get; set; }
        public string? series { get; set; }
        public object? primaryImage { get; set; }
        public object? versions { get; set; }
    }
}