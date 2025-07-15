using Dumpify;
using PrintO.Intergrations.Interfaces;
using PrintO.Models;
using PrintO.Models.Integrations;
using PrintO.Models.Products;
using PrintO.Models.Products.Figurine;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using Zorro.Data;
using Zorro.Middlewares;
using static Zorro.Secrets.GetSecretValueUtility;

using CATEGORY = (int CATEGORY_ID, int TYPE_ID);

namespace PrintO.Intergrations;

public class OzonIntegration : IIntegradable<FigurineReference, FigurineVariation>
{
    public static HttpClient client { get; private set; } = null!;

    public const string BASE_ADDRESS = "https://api-seller.ozon.ru/";

    static readonly CATEGORY FIGURINE_CATEGORY = (17028973, 115944384);
    static readonly CATEGORY TABLE_GAME_ACCESSORY_CATEGORY = (88076545, 92885);

    private readonly ModelRepository<Product> _productRepo;
    private readonly ModelRepository<FigurineReference> _fRefRepo;
    private readonly ModelRepository<FigurineVariation> _fVarRepo;
    private readonly ModelRepository<Models.File> _fFileRepo;
    private readonly ModelRepository<ImageReference> _fImageRepo;
    private readonly ModelRepository<OzonIntegrationTask> _taskRepo;
    private readonly MinIORepository _minIORepo;

    public OzonIntegration(ModelRepository<Product> productRepo,
        ModelRepository<FigurineReference> fRefRepo,
        ModelRepository<FigurineVariation> fVarRepo,
        ModelRepository<Models.File> fFileRepo,
        ModelRepository<ImageReference> fImageRepo,
        ModelRepository<OzonIntegrationTask> taskRepo,
        MinIORepository minIORepo)
    {
        string API_KEY = GetSecretValue("/OZON", "API_KEY");
        string CLIENT_ID = GetSecretValue("/OZON", "CLIENT_ID");

        client = new HttpClient();
        client.DefaultRequestHeaders.Add("Api-Key", API_KEY);
        client.DefaultRequestHeaders.Add("Client-Id", CLIENT_ID);
        client.BaseAddress = new Uri(BASE_ADDRESS);

        _productRepo = productRepo;
        _fRefRepo = fRefRepo;
        _fVarRepo = fVarRepo;
        _fFileRepo = fFileRepo;
        _fImageRepo = fImageRepo;
        _taskRepo = taskRepo;
        _minIORepo = minIORepo;
    }

    public void AddTaskAndInspect(int executorId, int productId, long taskId)
    {
        OzonIntegrationTask task = new();
        task.AddFill(new OzonIntegrationTask.AddForm(executorId, productId, taskId));
        _taskRepo.Add(task);
    }

    public bool HasFigurine(string SKU, out JsonElement root)
    {
        object pullRequest = new
        {
            filter = new
            {
                offer_id = new[] { SKU },
                visibility = "ALL"
            },
            limit = 1,
            sort_dir = "ASC"
        };

        bool isNewVariation = false;
        string responseJson = "{}";
        try
        {
            var pullTask = client.POST("/v4/product/info/attributes", JsonSerializer.Serialize(pullRequest));
            responseJson = pullTask.GetAwaiter().GetResult();
        }
        catch (QueryException)
        {
            isNewVariation = true;
            /*
            if (pullDoc.RootElement.TryGetProperty("code", out JsonElement errorCodeElement))
            {
                int errorCode = errorCodeElement.GetInt32();
                if (errorCode == 5)
                else
                    throw new QueryException($"Unexpected error code from ozon API - error code: {errorCode}");
            }
            */
        }

        JsonDocument pullDoc = JsonDocument.Parse(responseJson);
        root = pullDoc.RootElement;

        return isNewVariation;
    }

    public bool UpdateFigurine(User executor, FigurineReference figurine)
    {
        var images = figurine.product.images
            .OrderBy(i => i.index)
            .Select(i =>
            {
                var file = figurine.product.files.FirstOrDefault(f => f.Id == i.fileId);
                if (file is null)
                    throw new Exception("File not found.");
                return _minIORepo.GetFullPath(file.filePath);
            })
            .ToList();

        string? primaryImage = null;
        var otherImages = new List<string>();
        if (images.Count > 0)
        {
            primaryImage = images.FirstOrDefault();
            otherImages = images[1..];
        }

        List<object> items = new List<object>();

        foreach (FigurineVariation variation in figurine.variations)
        {
            object? request = FormUpdateFigurineRequest(figurine, variation, otherImages, primaryImage);
            if (request is null)
                continue;
            items.Add(request);
        }

        long taskId = SendUpdateFigurinesRequest(items);
        AddTaskAndInspect(executor.Id, figurine.productId, taskId);

        return true;
    }

    public bool UpdateAllFigurines(User executor, int storeId)
    {
        IDictionary<string, bool?> context = new Dictionary<string, bool?>
        {
            { "INCLUDE_FILES", true },
            { "INCLUDE_IMAGES", true },
            { "INCLUDE_VARIATIONS", true }
        };

        var figurines = _fRefRepo.GetAll(context).Where(f => f.product.storeId == storeId);

        List<(int productId, object request)> items = new();

        foreach (FigurineReference figurine in figurines)
        {
            var images = figurine.product.images
            .OrderBy(i => i.index)
            .Select(i =>
            {
                var file = figurine.product.files.FirstOrDefault(f => f.Id == i.fileId);
                if (file is null)
                    throw new Exception("File not found.");
                return _minIORepo.GetFullPath(file.filePath);
            })
            .ToList();

            string? primaryImage = null;
            var otherImages = new List<string>();
            if (images.Count > 0)
            {
                primaryImage = images.FirstOrDefault();
                otherImages = images[1..];
            }

            foreach (FigurineVariation variation in figurine.variations)
            {
                object? request = FormUpdateFigurineRequest(figurine, variation, otherImages, primaryImage);
                if (request is null)
                    continue;
                items.Add((figurine.productId, request));
            }
        }

        int pageCounter = 0;
        const int PAGE_SIZE = 1;
        int figurinesCount = figurines.Count();
        do
        {
            var requestTuplesPage = items.Skip(PAGE_SIZE * pageCounter++).Take(PAGE_SIZE);
            var requestPage = requestTuplesPage.Select(r => r.request);
            long taskId = SendUpdateFigurinesRequest(requestPage);

            foreach (var request in requestTuplesPage)
            {
                AddTaskAndInspect(executor.Id, request.productId, taskId);
            }
        }
        while (pageCounter * PAGE_SIZE < figurinesCount);

        return true;
    }

    private long SendUpdateFigurinesRequest(IEnumerable<object> items)
    {
        JsonSerializerOptions options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic),
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        };

        string json = JsonSerializer.Serialize(new { items }, options);

        //System.IO.File.WriteAllText($"./test_upload.json", json, Encoding.UTF8);

        string response = client.POST("/v3/product/import", json).GetAwaiter().GetResult();
        JsonDocument postDoc = JsonDocument.Parse(response);

        if (!postDoc.RootElement.TryGetProperty("result", out JsonElement postResultElement))
            throw new Exception("No 'result' property in response.");

        if (!postResultElement.TryGetProperty("task_id", out JsonElement taskIdElement))
            throw new Exception("No 'items' property in 'result'.");

        return taskIdElement.GetInt64();
    }

    private object? FormUpdateFigurineRequest(FigurineReference figurine, FigurineVariation variation, List<string> otherImages, string? primaryImage)
    {
        if (variation.isActive is false)
            return null;

        string variationSKU = variation.separateSKU;
        CATEGORY selectedCategory = FIGURINE_CATEGORY;

        bool isNewVariation = HasFigurine(variationSKU, out JsonElement rootElement);

        if (rootElement.TryGetProperty("result", out JsonElement pullResultElement))
        {
            var pullCategory = pullResultElement
                .EnumerateArray()
                .FirstOrDefault()
                .GetProperty("description_category_id")
                .GetInt64();

            if (pullCategory == FIGURINE_CATEGORY.CATEGORY_ID)
                selectedCategory = FIGURINE_CATEGORY;
            else if (pullCategory == TABLE_GAME_ACCESSORY_CATEGORY.CATEGORY_ID)
                selectedCategory = TABLE_GAME_ACCESSORY_CATEGORY;
        }

        var volume = GetPackageVolume(variation);
        string? scaleLabel = variation.scale?.ToString().Replace("oneTo", "1:");

        string nameFormat = figurine.product.name + " - " + figurine.product.series + " ({0}{1})";
        string name = nameFormat;
        if (variation.heightMm.HasValue)
            name = name.Replace("{0}", $"{variation.heightMm}мм ");
        else
            name = name.Replace("{0}", string.Empty);
        if (variation.scale.HasValue)
            name = name.Replace("{1}", $"в масштабе {scaleLabel}");
        else
            name = name.Replace("{1}", string.Empty);
        name = name.Replace("( )", string.Empty);
        name = name.Replace("()", string.Empty);
        name = name.Replace(" )", ")");

        string? seriesTag = figurine.product.series?.Replace(' ', '_');
        string hashtags = $"#3D #Миниатюры #Фигурки #Миниатюра #Фигурка";
        string tags = $"printo;printo.studio;3d;миниатюры;фигурки;фигурка;миниатюра;печать;игры;аниме";
        if (!string.IsNullOrEmpty(seriesTag))
        {
            hashtags = hashtags.Insert(0, $"#{seriesTag} ");
            tags = tags.Insert(0, $"{seriesTag};");
        }

        string description = figurine.product.description;
        description += "\n\n🔍 Характеристики товара:\n";

        string AddLine(string label, string value)
        {
            int totalLength = 40;
            string dots = new string('.', Math.Max(1, totalLength - label.Length));
            return $"{label}{dots}{value}\n";
        }

        description += AddLine("🎨 Цвет", (variation.color == Enums.Color.Gray ? "серый" : "белый"));

        if (variation.scale.HasValue)
            description += AddLine("📐 Масштаб", scaleLabel!);

        {
            string integrityText = variation.integrity switch
            {
                Enums.Integrity.Solid => "Миниатюра доставляется цельной",
                Enums.Integrity.Dismountable => "Миниатюра доставляется в разборе",
                Enums.Integrity.DismountableBase => "Подставка отсоединяется от миниатюры",
                _ => "Неизвестно"
            };
            description += AddLine("🧱 Целостность", integrityText);
        }

        description += AddLine("🧍 Количество фигурок", $"{variation.quantity}");
        description += AddLine("⚖️ Вес фигурки", $"{variation.weightGr} гр.");

        if (variation.heightMm.HasValue)
            description += AddLine("📏 Высота фигурки", $"{variation.heightMm} мм.");
        if (variation.widthMm.HasValue)
            description += AddLine("📏 Ширина фигурки", $"{variation.widthMm} мм.");
        if (variation.depthMm.HasValue)
            description += AddLine("📏 Длина фигурки", $"{variation.depthMm} мм.");
        if (variation.minHeightMm.HasValue)
            description += AddLine("↘️ Минимальная высота", $"{variation.minHeightMm} мм.");
        if (variation.averageHeightMm.HasValue)
            description += AddLine("📊 Средняя высота", $"{variation.averageHeightMm} мм.");
        if (variation.maxHeightMm.HasValue)
            description += AddLine("⬆️ Максимальная высота", $"{variation.maxHeightMm} мм.");

        List<object?> attributes = new()
            {
                // Бренд
                new {
                    id = 85,
                    complex_id = 0,
                    values = new[] {
                        new
                        {
                            value = "Нет бренда"
                        }
                    }
                },
                // Название модели (для объединения в одну карточку)
                new
                {
                    id = 9048,
                    complex_id = 0,
                    values = new[] {
                        new
                        {
                            value = string.IsNullOrEmpty(figurine.product.series) ? name : figurine.product.series
                        }
                    }
                },
                // Признак 18+
                new
                {
                    id = 9070,
                    complex_id = 0,
                    values = new[] {
                        new
                        {
                            value = figurine.product.explicitContent.ToString()
                        }
                    }
                },
                // #Хештеги
                /*
                new
                {
                    id = 23171,
                    complex_id = 0,
                    values = new[] {
                        new
                        {
                            value = hashtags
                        }
                    }
                },
                */
                // Аннотация
                new
                {
                    id = 4191,
                    complex_id = 0,
                    values = new[] {
                        new
                        {
                            value = description
                        }
                    }
                },
                // Ключевые слова
                new
                {
                    id = 22336,
                    complex_id = 0,
                    values = new[] {
                        new
                        {
                            value = tags
                        }
                    }
                },
                // Название группы
                !string.IsNullOrEmpty(figurine.product.series) ? new
                {
                    id = 22390,
                    complex_id = 0,
                    values = new[] {
                        new
                        {
                            value = figurine.product.series
                        }
                    }
                } : null,
                // Вид детской фигурки
                new
                {
                    id = 9447,
                    complex_id = 0,
                    values = new[] {
                        new
                        {
                            dictionary_value_id = 290771564,
                            value = "Статичная"
                        }
                    }
                },
                // Тематика фигурки
                new
                {
                    id = 7110,
                    complex_id = 0,
                    values = new[] {
                        new
                        {
                            dictionary_value_id = 31289,
                            value = "Герои и персонажи"
                        },
                        new
                        {
                            dictionary_value_id = 478434444,
                            value = "Аниме"
                        },
                    }
                    },
                // Цвет товара
                new
                {
                    id = 10096,
                    complex_id = 0,
                    values = new[] {
                        new
                        {
                            dictionary_value_id = 61576,
                            value = variation.color == Enums.Color.Gray ? "серый" : "белый"
                        }
                    }
                },
                // Название цвета
                new
                {
                    id = 10097,
                    complex_id = 0,
                    values = new[] {
                        new
                        {
                            value = variation.name ?? figurine.product.name
                        }
                    }
                },
                // Высота игрушки, см
                new
                {
                    id = 7147,
                    complex_id = 0,
                    values = new[] {
                        new
                        {
                            value = (variation.heightMm / 10f).ToString()
                        }
                    }
                },
                // Материал
                new
                {
                    id = 4975,
                    complex_id = 0,
                    values = new[] {
                        new
                        {
                            dictionary_value_id = 62090,
                            value = "Смола"
                        }
                    }
                },
                // Пол ребенка
                new
                {
                    id = 13216,
                    complex_id = 0,
                    values = new[] {
                        new
                        {
                            dictionary_value_id = 971006037,
                            value = "Унисекс"
                        }
                    }
                },
                // Минимальный возраст ребенка
                new
                {
                    id = 13214,
                    complex_id = 0,
                    values = new[] {
                        new
                        {
                            dictionary_value_id = 971006009,
                            value = "7 лет"
                        }
                    }
                },
                // Максимальный возраст ребенка
                new
                {
                    id = 13215,
                    complex_id = 0,
                    values = new[] {
                        new
                        {
                            dictionary_value_id = 971005969,
                            value = "18 лет"
                        }
                    }
                },
                // Количество фигурок
                new
                {
                    id = 8782,
                    complex_id = 0,
                    values = new[] {
                        new
                        {
                            value = variation.quantity.ToString()
                        }
                    }
                },
            // Количество элементов, шт
                variation.integrity == Enums.Integrity.Solid ?
                new
                {
                    id = 7188,
                    complex_id = 0,
                    values = new[] {
                        new
                        {
                            value = 1.ToString()
                        }
                    }
                } : null,
                // Страна-изготовитель
                new
                {
                    id = 4389,
                    complex_id = 0,
                    values = new[] {
                        new
                        {
                            dictionary_value_id = 90295,
                            value = "Россия"
                        }
                    }
                },
                // Название??
                new
                {
                    id = 4180,
                    complex_id = 0,
                    values = new[] {
                        new
                        {
                            value = name
                        }
                    }
                },
                // Вес с упаковкой, г
                new
                {
                    id = 4497,
                    complex_id = 0,
                    values = new[] {
                        new
                        {
                            value = MathF.Round((volume.width * volume.height * volume.depth) / 20000f + variation.weightGr).ToString()
                        }
                    }
                },
                // Код продавца
                new
                {
                    id = 9024,
                    complex_id = 0,
                    values = new[] {
                        new
                        {
                            value = variationSKU
                        }
                    }
                },
                // Целевая аудитория
                figurine.product.explicitContent ? new
                {
                    id = 9390,
                    complex_id = 0,
                    values = new[] {
                        new
                        {
                            dictionary_value_id = 43241,
                            value = "Взрослая"
                        }
                    }
                } : new
                {
                    id = 9390,
                    complex_id = 0,
                    values = new[] {
                        new
                        {
                            dictionary_value_id = 43241,
                            value = "Взрослая"
                        },
                        new
                        {
                            dictionary_value_id = 43242,
                            value = "Детская"
                        },
                    }
                },
            };

        attributes.RemoveAll(a => a is null);

        object item = new
        {
            barcode = isNewVariation ? variationSKU : null,
            description_category_id = selectedCategory.CATEGORY_ID,
            color_image = primaryImage,
            currency_code = "RUB",
            volume.depth,
            dimension_unit = "mm",
            volume.height,
            images = otherImages,
            name,
            offer_id = variationSKU,
            old_price = variation.priceBeforeSaleRub.ToString(),
            price = variation.priceRub.ToString(),
            primary_image = primaryImage,
            type_id = selectedCategory.TYPE_ID,
            //vat = "0.1",
            weight = variation.weightGr,
            weight_unit = "g",
            volume.width,
            attributes
        };

        return item;
        //items.Add(item);

        (uint width, uint height, uint depth) GetPackageVolume(FigurineVariation variation)
        {
            uint? anyBiggestComponent = new[]
            {
                variation.widthMm,
                variation.heightMm,
                variation.depthMm,
                variation.maxHeightMm,
                variation.averageHeightMm,
                variation.minHeightMm
            }.Max();
            uint biggestComponent = anyBiggestComponent ?? 50;

            uint width = (uint)((variation.widthMm ?? biggestComponent) * 1.15f);
            uint height = (uint)((variation.heightMm ?? biggestComponent) * 1.15f);
            uint depth = (uint)((variation.depthMm ?? biggestComponent) * 1.15f);

            return (width, height, depth);
        }
    }
}