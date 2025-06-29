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

    [StringLength(FIGURINE_VARIATION_NAME_MAX_LENGTH)]
    public string name { get; set; } = null!;
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

    public bool AddFill(AddForm form)
    {
        name = form.addForm.name;
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

        figurineId = form.figurineId;

        return true;
    }

    public bool UpdateFill(UpdateForm form)
    {
        if (form.name is not null)
            name = form.name;
        if (scale.HasValue)
            scale = form.scale!.Value;
        if (form.color.HasValue)
            color = form.color.Value;
        if (form.weightGr.HasValue)
            weightGr = form.weightGr.Value;

        if(form.heightMm.HasValue) 
            heightMm = form.heightMm.Value;
        if (form.widthMm.HasValue)
            widthMm = form.widthMm.Value;
        if (form.depthMm.HasValue)
            depthMm = form.depthMm.Value;

        if (form.maxHeightMm.HasValue)
            maxHeightMm = form.maxHeightMm.Value;
        if (form.averageHeightMm.HasValue)
            averageHeightMm = form.averageHeightMm.Value;
        if (form.minHeightMm.HasValue)
            minHeightMm = form.minHeightMm.Value;

        if (form.priceRub.HasValue)
            priceRub = form.priceRub.Value;
        if (form.priceBeforeSaleRub.HasValue)
            priceBeforeSaleRub = form.priceBeforeSaleRub.Value;
        if (form.minimalPriceRub.HasValue)
            minimalPriceRub = form.minimalPriceRub.Value;

        return true;
    }

    public object MapToDTO(object? argsObject = null)
    {
        return new
        {
            Id,

            name,
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
            minimalPriceRub
        };
    }

    public const int FIGURINE_VARIATION_NAME_MAX_LENGTH = 50;
    public const int FIGURINE_VARIATION_WEIGHT_GR_MAX = 10000; // 10Kg
    public const int FIGURINE_VARIATION_WEIGHT_GR_MIN = 1;

    public const int FIGURINE_VARIATION_DIMENSION_MM_MAX = 1000; // 1m
    public const int FIGURINE_VARIATION_DIMENSION_MM_MIN = 1;

    public const ulong FIGURINE_VARIATION_PRICE_RUB_MAX = 100000000; // 100m rubs
    public const ulong FIGURINE_VARIATION_PRICE_RUB_MIN = 10;

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
        [StringLength(FIGURINE_VARIATION_NAME_MAX_LENGTH)]
        public string name { get; set; }
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
    }

    public struct UpdateForm
    {
        public int id { get; set; }

        [StringLength(FIGURINE_VARIATION_NAME_MAX_LENGTH)]
        public string? name { get; set; }
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
    }
}