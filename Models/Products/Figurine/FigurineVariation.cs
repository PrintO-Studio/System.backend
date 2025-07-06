using PrintO.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Zorro.Data.Interfaces;
using static PrintO.Models.Products.Figurine.FigurineVariation;

namespace PrintO.Models.Products.Figurine;

public class FigurineVariation : IEntity, ISellable, IDataTransferObject<object>, IAddable<AddForm>, IUpdateable<UpdateForm>
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(figurine))]
    public int figurineId { get; set; }
    [InverseProperty(nameof(FigurineReference.variations))]
    public FigurineReference figurine { get; set; } = null!;

    public bool isActive { get; set; } = true;

    [StringLength(FIGURINE_VARIATION_NAME_MAX_LENGTH)]
    public string name { get; set; } = null!;
    [StringLength(FIGURINE_VARIATION_SERIES_MAX_LENGTH)]
    public string? series { get; set; }
    public Scale? scale { get; set; }
    public Color color { get; set; }
    [Range(FIGURINE_VARIATION_WEIGHT_GR_MIN, FIGURINE_VARIATION_WEIGHT_GR_MAX)]
    public uint weightGr { get; set; }

    [Range(FIGURINE_VARIATION_DIMENSION_MM_MIN, FIGURINE_VARIATION_DIMENSION_MM_MAX)]
    public uint heightMm { get; set; }
    [Range(FIGURINE_VARIATION_DIMENSION_MM_MIN, FIGURINE_VARIATION_DIMENSION_MM_MAX)]
    public uint? widthMm { get; set; }
    [Range(FIGURINE_VARIATION_DIMENSION_MM_MIN, FIGURINE_VARIATION_DIMENSION_MM_MAX)]
    public uint? depthMm { get; set; }

    [Range(FIGURINE_VARIATION_DIMENSION_MM_MIN, FIGURINE_VARIATION_DIMENSION_MM_MAX)]
    public uint? minHeightMm { get; set; }
    [Range(FIGURINE_VARIATION_DIMENSION_MM_MIN, FIGURINE_VARIATION_DIMENSION_MM_MAX)]
    public uint? averageHeightMm { get; set; }
    [Range(FIGURINE_VARIATION_DIMENSION_MM_MIN, FIGURINE_VARIATION_DIMENSION_MM_MAX)]
    public uint? maxHeightMm { get; set; }

    [Range(FIGURINE_VARIATION_PRICE_RUB_MIN, FIGURINE_VARIATION_PRICE_RUB_MAX)]
    public ulong priceRub { get; set; }
    [Range(FIGURINE_VARIATION_PRICE_RUB_MIN, FIGURINE_VARIATION_PRICE_RUB_MAX)]
    public ulong priceBeforeSaleRub { get; set; }
    [Range(FIGURINE_VARIATION_PRICE_RUB_MIN, FIGURINE_VARIATION_PRICE_RUB_MAX)]
    public ulong minimalPriceRub { get; set; }

    public Integrity integrity { get; set; } = Integrity.Solid;
    [Range(FIGURINE_VARIATION_QUANTITY_MIN, FIGURINE_VARIATION_QUANTITY_MAX)]
    public uint quantity { get; set; }

    public bool AddFill(AddForm form)
    {
        isActive = form.addForm.isActive ?? true;
        figurineId = form.figurineId;

        name = form.addForm.name;
        series = form.addForm.series;
        scale = form.addForm.scale;
        color = form.addForm.color;
        weightGr = form.addForm.weightGr;

        heightMm = form.addForm.heightMm;
        widthMm = form.addForm.widthMm;
        depthMm = form.addForm.depthMm;

        maxHeightMm = form.addForm.maxHeightMm;
        averageHeightMm = form.addForm.averageHeightMm;
        minHeightMm = form.addForm.minHeightMm;

        priceRub = form.addForm.priceRub;
        priceBeforeSaleRub = form.addForm.priceBeforeSaleRub;
        minimalPriceRub = form.addForm.minimalPriceRub;

        integrity = form.addForm.integrity;
        quantity = form.addForm.quantity;

        return true;
    }

    public bool UpdateFill(UpdateForm form)
    {
        if (form.isActive.HasValue)
            isActive = form.isActive.Value;
        if (!string.IsNullOrEmpty(form.name))
            name = form.name;
        series = form.series;
        scale = form.scale;
        if (form.color.HasValue)
            color = form.color.Value;
        if (form.weightGr.HasValue)
            weightGr = form.weightGr.Value;
        if (form.heightMm.HasValue)
            heightMm = form.heightMm.Value;
        widthMm = form.widthMm;
        depthMm = form.depthMm;

        maxHeightMm = form.maxHeightMm;
        averageHeightMm = form.averageHeightMm;
        minHeightMm = form.minHeightMm;

        if (form.priceRub.HasValue)
            priceRub = form.priceRub.Value;
        if (form.priceBeforeSaleRub.HasValue)
            priceBeforeSaleRub = form.priceBeforeSaleRub.Value;
        if (form.minimalPriceRub.HasValue)
            minimalPriceRub = form.minimalPriceRub.Value;

        integrity = form.integrity ?? Integrity.Solid;
        if (form.quantity.HasValue)
            quantity = form.quantity.Value;

        return true;
    }

    public object MapToDTO(object? argsObject = null)
    {
        return new
        {
            Id,

            isActive,
            name,
            series,
            scale,
            color,
            weightGr,

            heightMm,
            widthMm,
            depthMm,

            minHeightMm,
            averageHeightMm,
            maxHeightMm,

            priceRub,
            priceBeforeSaleRub,
            minimalPriceRub,

            integrity,
            quantity
        };
    }

    public const int FIGURINE_VARIATION_NAME_MAX_LENGTH = 50;
    public const int FIGURINE_VARIATION_SERIES_MAX_LENGTH = 50;
    public const int FIGURINE_VARIATION_WEIGHT_GR_MAX = 10000; // 10Kg
    public const int FIGURINE_VARIATION_WEIGHT_GR_MIN = 1;

    public const int FIGURINE_VARIATION_DIMENSION_MM_MAX = 1000; // 1m
    public const int FIGURINE_VARIATION_DIMENSION_MM_MIN = 1;

    public const ulong FIGURINE_VARIATION_PRICE_RUB_MAX = 100000000; // 100m rubs
    public const ulong FIGURINE_VARIATION_PRICE_RUB_MIN = 10;

    public const ulong FIGURINE_VARIATION_QUANTITY_MAX = 1000;
    public const ulong FIGURINE_VARIATION_QUANTITY_MIN = 1;

    public struct AddForm
    {
        public UserAddForm addForm { get; set; }
        public int figurineId { get; set; }

        public AddForm(UserAddForm addForm, int figurineId)
        {
            this.addForm = addForm;
            this.figurineId = figurineId;
        }
    }

    public struct UserAddForm
    {
        public bool? isActive { get; set; }
        [StringLength(FIGURINE_VARIATION_NAME_MAX_LENGTH)]
        public string name { get; set; }
        [StringLength(FIGURINE_VARIATION_SERIES_MAX_LENGTH)]
        public string? series { get; set; }
        public Scale? scale { get; set; }
        public Color color { get; set; }
        [Range(FIGURINE_VARIATION_WEIGHT_GR_MIN, FIGURINE_VARIATION_WEIGHT_GR_MAX)]
        public uint weightGr { get; set; }

        [Range(FIGURINE_VARIATION_DIMENSION_MM_MIN, FIGURINE_VARIATION_DIMENSION_MM_MAX)]
        public uint heightMm { get; set; }
        [Range(FIGURINE_VARIATION_DIMENSION_MM_MIN, FIGURINE_VARIATION_DIMENSION_MM_MAX)]
        public uint? widthMm { get; set; }
        [Range(FIGURINE_VARIATION_DIMENSION_MM_MIN, FIGURINE_VARIATION_DIMENSION_MM_MAX)]
        public uint? depthMm { get; set; }

        [Range(FIGURINE_VARIATION_DIMENSION_MM_MIN, FIGURINE_VARIATION_DIMENSION_MM_MAX)]
        public uint? minHeightMm { get; set; }
        [Range(FIGURINE_VARIATION_DIMENSION_MM_MIN, FIGURINE_VARIATION_DIMENSION_MM_MAX)]
        public uint? averageHeightMm { get; set; }
        [Range(FIGURINE_VARIATION_DIMENSION_MM_MIN, FIGURINE_VARIATION_DIMENSION_MM_MAX)]
        public uint? maxHeightMm { get; set; }

        [Range(FIGURINE_VARIATION_PRICE_RUB_MIN, FIGURINE_VARIATION_PRICE_RUB_MAX)]
        public ulong priceRub { get; set; }
        [Range(FIGURINE_VARIATION_PRICE_RUB_MIN, FIGURINE_VARIATION_PRICE_RUB_MAX)]
        public ulong priceBeforeSaleRub { get; set; }
        [Range(FIGURINE_VARIATION_PRICE_RUB_MIN, FIGURINE_VARIATION_PRICE_RUB_MAX)]
        public ulong minimalPriceRub { get; set; }

        public Integrity integrity { get; set; }
        [Range(FIGURINE_VARIATION_QUANTITY_MIN, FIGURINE_VARIATION_QUANTITY_MAX)]
        public uint quantity { get; set; }
    }

    public struct UpdateForm
    {
        public int id { get; set; }

        public bool? isActive { get; set; }
        [StringLength(FIGURINE_VARIATION_NAME_MAX_LENGTH)]
        public string? name { get; set; }
        [StringLength(FIGURINE_VARIATION_SERIES_MAX_LENGTH)]
        public string? series { get; set; }
        public Scale? scale { get; set; }
        public Color? color { get; set; }
        [Range(FIGURINE_VARIATION_WEIGHT_GR_MIN, FIGURINE_VARIATION_WEIGHT_GR_MAX)]
        public uint? weightGr { get; set; }

        [Range(FIGURINE_VARIATION_DIMENSION_MM_MIN, FIGURINE_VARIATION_DIMENSION_MM_MAX)]
        public uint? heightMm { get; set; }
        [Range(FIGURINE_VARIATION_DIMENSION_MM_MIN, FIGURINE_VARIATION_DIMENSION_MM_MAX)]
        public uint? widthMm { get; set; }
        [Range(FIGURINE_VARIATION_DIMENSION_MM_MIN, FIGURINE_VARIATION_DIMENSION_MM_MAX)]
        public uint? depthMm { get; set; }

        [Range(FIGURINE_VARIATION_DIMENSION_MM_MIN, FIGURINE_VARIATION_DIMENSION_MM_MAX)]
        public uint? minHeightMm { get; set; }
        [Range(FIGURINE_VARIATION_DIMENSION_MM_MIN, FIGURINE_VARIATION_DIMENSION_MM_MAX)]
        public uint? averageHeightMm { get; set; }
        [Range(FIGURINE_VARIATION_DIMENSION_MM_MIN, FIGURINE_VARIATION_DIMENSION_MM_MAX)]
        public uint? maxHeightMm { get; set; }

        [Range(FIGURINE_VARIATION_PRICE_RUB_MIN, FIGURINE_VARIATION_PRICE_RUB_MAX)]
        public ulong? priceRub { get; set; }
        [Range(FIGURINE_VARIATION_PRICE_RUB_MIN, FIGURINE_VARIATION_PRICE_RUB_MAX)]
        public ulong? priceBeforeSaleRub { get; set; }
        [Range(FIGURINE_VARIATION_PRICE_RUB_MIN, FIGURINE_VARIATION_PRICE_RUB_MAX)]
        public ulong? minimalPriceRub { get; set; }

        public Integrity? integrity { get; set; }
        [Range(FIGURINE_VARIATION_QUANTITY_MIN, FIGURINE_VARIATION_QUANTITY_MAX)]
        public uint? quantity { get; set; }
    }
}