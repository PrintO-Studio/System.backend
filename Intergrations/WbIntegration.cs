using Dumpify;
using PrintO.Intergrations.Interfaces;
using PrintO.Models;
using PrintO.Models.Integrations;
using PrintO.Models.Products;
using PrintO.Models.Products.Figurine;
using System.Collections.Concurrent;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Text.Unicode;
using Zorro.Data;
using static Zorro.Secrets.GetSecretValueUtility;

namespace PrintO.Intergrations;

public class WbIntegration : IIntegradable<FigurineReference, FigurineVariation>
{
    public readonly HttpClient commonClient;
    public readonly HttpClient contentClient;
    public readonly HttpClient priceClient;
    public readonly JsonSerializerOptions jsonOptions;

    private readonly ModelRepository<Product> _productRepo;
    private readonly ModelRepository<FigurineReference> _fRefRepo;
    private readonly ModelRepository<FigurineVariation> _fVarRepo;
    private readonly ModelRepository<Models.File> _fFileRepo;
    private readonly ModelRepository<ImageReference> _fImageRepo;
    private readonly ModelRepository<WbIntegrationTask> _taskRepo; // -> WB
    private readonly MinIORepository _minIORepo;

    public WbIntegration(ModelRepository<Product> productRepo,
        ModelRepository<FigurineReference> fRefRepo,
        ModelRepository<FigurineVariation> fVarRepo,
        ModelRepository<Models.File> fFileRepo,
        ModelRepository<ImageReference> fImageRepo,
        ModelRepository<WbIntegrationTask> taskRepo, // -> WB
        MinIORepository minIORepo)
    {
        string WB_API_KEY = GetSecretValue("/WILDBERRIES", "API_KEY");

        commonClient = new();
        commonClient.DefaultRequestHeaders.Add("Authorization", WB_API_KEY);
        commonClient.BaseAddress = new Uri("https://common-api.wildberries.ru");

        contentClient = new HttpClient();
        contentClient.DefaultRequestHeaders.Add("Authorization", WB_API_KEY);
        contentClient.BaseAddress = new Uri("https://content-api.wildberries.ru");

        priceClient = new HttpClient();
        priceClient.DefaultRequestHeaders.Add("Authorization", WB_API_KEY);
        priceClient.BaseAddress = new Uri("https://discounts-prices-api.wildberries.ru");

        jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic),
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        _productRepo = productRepo;
        _fRefRepo = fRefRepo;
        _fVarRepo = fVarRepo;
        _fFileRepo = fFileRepo;
        _fImageRepo = fImageRepo;
        _taskRepo = taskRepo;
        _minIORepo = minIORepo;
    }

    public void AddTask(int executorId, int productId)
    {
        WbIntegrationTask task = new();
        task.AddFill(new WbIntegrationTask.AddForm(executorId, productId));
        _taskRepo.Add(task);
    }

    public bool HasFigurine(string SKU, out JsonElement root)
    {
        var prodDoc = PullProductAsync(SKU).GetAwaiter().GetResult();
        root = prodDoc.RootElement;

        if (!root.TryGetProperty("cursor", out var cursorEl))
            throw new Exception("No 'cursor' property in response.");

        if (!cursorEl.TryGetProperty("total", out var totalEl))
            throw new Exception("No 'total' property in response.");

        int total = totalEl.GetInt32();

        if (total == 0)
            return false;
        else if (total == 1)
            return true;
        else
            throw new Exception($"More that one product were found using search query: {SKU}");
    }

    public bool UpdateAllFigurines(User executor, int storeId)
    {
        throw new NotImplementedException();
    }

    public bool UpdateFigurine(User executor, FigurineReference figurine)
    {
        List<(object request, string SKU, (ulong price, ulong oldPrice) priceData)> uploadRequests = new();
        List<(object request, int nmID, int imtID)> updateRequests = new();
        ConcurrentDictionary<int, (ulong price, ulong oldPrice)> priceMapping = new();
        foreach (FigurineVariation variation in figurine.variations)
        {
            if (variation.isActive is false)
                continue;

            bool hasVariation = HasFigurine(variation.separateSKU, out JsonElement root);
            var priceData = (variation.priceRub, variation.priceBeforeSaleRub);

            if (hasVariation)
            {
                var ur = BuildUpdateRequest(executor, figurine, variation, root);
                updateRequests.Add(ur);
                priceMapping.TryAdd(ur.nmID, priceData);
            }
            else
            {
                uploadRequests.Add((BuildUploadRequest(executor, figurine, variation), variation.separateSKU, priceData));
            }
        }

        // update request
        int? generalImtID = null;
        if (updateRequests.Count > 0)
        {
            string updateRequestJson = JsonSerializer.Serialize(updateRequests.Select(r => r.request), jsonOptions);
            var updateTask = contentClient.POST("/content/v2/cards/update", updateRequestJson);
            string updateResponseJson = updateTask.GetAwaiter().GetResult();

            generalImtID = updateRequests.FirstOrDefault().imtID;
        }

        // join existing
        if (generalImtID.HasValue && updateRequests.Any(ur => ur.imtID != generalImtID.Value))
        {
            var nmIDs = updateRequests.Select(ur => ur.nmID);
            object joinRequest = new
            {
                targetIMT = generalImtID.Value,
                nmIDs
            };

            string joinRequestJson = JsonSerializer.Serialize(joinRequest, jsonOptions);
            var joinTask = contentClient.POST("/content/v2/cards/moveNm", joinRequestJson);
            string joinResponseJson = joinTask.GetAwaiter().GetResult();
        }

        // add request (with join)
        var newNmIds = Array.Empty<int>().ToList();
        if (uploadRequests.Count > 0)
        {
            var justRequests = uploadRequests.Select(ur => ur.request);

            if (generalImtID.HasValue)
            {
                object uploadRequest = new
                {
                    imtID = generalImtID.Value,
                    cardsToAdd = justRequests
                };

                string uploadRequestJson = JsonSerializer.Serialize(uploadRequest, jsonOptions);
                var uploadTask = contentClient.POST("/content/v2/cards/upload/add", uploadRequestJson);
                string uploadResponseJson = uploadTask.GetAwaiter().GetResult();
            }
            else
            {
                object uploadRequest = new
                {
                    subjectID = 236,
                    variants = justRequests
                };

                string uploadRequestJson = JsonSerializer.Serialize(new[] { uploadRequest }, jsonOptions);
                var uploadTask = contentClient.POST("/content/v2/cards/upload", uploadRequestJson);
                string uploadResponseJson = uploadTask.GetAwaiter().GetResult();
            }
        }

        AddTask(executor.Id, figurine.productId);

        return true;
    }

    private (object request, int nmID, int imtID) BuildUpdateRequest(User executor, FigurineReference figurine, FigurineVariation variation, JsonElement root)
    {
        if (!root.TryGetProperty("cards", out var cardsEl))
            throw new Exception("No 'cards' property in response.");

        var pulledProductEl = cardsEl.EnumerateArray().FirstOrDefault();

        if (!pulledProductEl.TryGetProperty("nmID", out var nmIDEl))
            throw new Exception("No 'nmID' property in response.");
        int nmID = nmIDEl.GetInt32();

        if (!pulledProductEl.TryGetProperty("imtID", out var imtIDEl))
            throw new Exception("No 'imtID' property in response.");
        int imtID = imtIDEl.GetInt32();

        if (!pulledProductEl.TryGetProperty("sizes", out var sizesEl))
            throw new Exception("No 'sizes' property in response.");

        var pulledSizeEl = sizesEl.EnumerateArray().FirstOrDefault();

        if (!pulledSizeEl.TryGetProperty("chrtID", out var chrtIDEl))
            throw new Exception("No 'chrtID' property in response.");

        int chrtID = chrtIDEl.GetInt32();

        if (!pulledSizeEl.TryGetProperty("skus", out var skusEl))
            throw new Exception("No 'skus' property in response.");

        string? sizeSku = skusEl.EnumerateArray().FirstOrDefault().GetString();

        if (string.IsNullOrEmpty(sizeSku))
            throw new Exception("size sku is empty.");

        string variationSKU = variation.separateSKU;
        PushProductData pushData = BuildVariationPushData(figurine, variation);
        var volume = GetPackageVolume(variation);

        List<object?> characteristics = new()
        {
            // SKU
            new {
                id = 14177453,
                value = new[]
            {
                    variationSKU
                }
            },
            // Высота предмета
            variation.heightMm.HasValue ? new {
                id = 90630,
                value = variation.heightMm / 10.0
            } : null,
            // Комплектация
            new {
                id = 378533,
                value = new[] { "Фигурка" }
            },
            // Материал изделия
            new {
                id = 17596,
                value = new[] { "смола" }
            },
            // Назначение подарка
            new {
                id = 59611,
                value = new[] { "брату", "для ребенка", "другу" }
            },
            // Повод
            new {
                id = 59615,
                value = new[] { "просто так", "23 февраля", "новый год" }
            },
            // Страна производства
            new {
                id = 14177451,
                value = new[] { "Россия" }
            },
            // Цвет
            new {
                id = 14177449,
                value = new[] { variation.color == Enums.Color.Gray ? "серый" : "белый" }
            },
            variation.widthMm.HasValue ? new
            {
                id = 90673,
                value = variation.widthMm.Value / 10.0
            } : null
        };

        characteristics.RemoveAll(a => a is null);

        object updateRequest = new
        {
            nmID,
            vendorCode = variationSKU,
            brand = "",
            title = pushData.name,
            description = RemoveDescriptionRestrictedSymbols(pushData.description),
            dimensions = new
            {
                length = volume.depth / 10,
                width = volume.width / 10,
                height = volume.height / 10,
                weightBrutto = variation.weightGr / 1000.0
            },
            characteristics,
            sizes = new object[]
            {
                new
                {
                    chrtID,
                    skus = new string[] { sizeSku }
                }
            },
        };

        return (updateRequest, nmID, imtID);
    }

    private object BuildUploadRequest(User executor, FigurineReference figurine, FigurineVariation variation)
    {
        string variationSKU = variation.separateSKU;
        PushProductData pushData = BuildVariationPushData(figurine, variation);
        var volume = GetPackageVolume(variation);

        List<object?> characteristics = new()
        {
            // SKU
            new {
                id = 14177453,
                value = new[]
            {
                    variationSKU
                }
            },
            // Высота предмета
            variation.heightMm.HasValue ? new {
                id = 90630,
                value = variation.heightMm / 10.0
            } : null,
            // Комплектация
            new {
                id = 378533,
                value = new[] { "Фигурка" }
            },
            // Материал изделия
            new {
                id = 17596,
                value = new[] { "смола" }
            },
            // Назначение подарка
            new {
                id = 59611,
                value = new[] { "брату", "для ребенка", "другу" }
            },
            // Повод
            new {
                id = 59615,
                value = new[] { "просто так", "23 февраля", "новый год" }
            },
            // Страна производства
            new {
                id = 14177451,
                value = new[] { "Россия" }
            },
            // Цвет
            new {
                id = 14177449,
                value = new[] { variation.color == Enums.Color.Gray ? "серый" : "белый" }
            },
            variation.widthMm.HasValue ? new
            {
                id = 90673,
                value = variation.widthMm.Value / 10.0
            } : null
        };

        characteristics.RemoveAll(a => a is null);

        object uploadRequest = new
        {
            vendorCode = variationSKU,
            title = pushData.name,
            description = RemoveDescriptionRestrictedSymbols(pushData.description),
            brand = "",
            dimensions = new
            {
                length = volume.depth / 10,
                width = volume.width / 10,
                height = volume.height / 10,
                weightBrutto = variation.weightGr / 1000.0
            },
            characteristics
        };

        return uploadRequest;
    }

    private string RemoveDescriptionRestrictedSymbols(string description)
    {
        return Regex.Replace(description, @"\p{So}|\p{Cs}", "-");
    }

    private PushProductData BuildVariationPushData(FigurineReference figurine, FigurineVariation variation)
    {
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
        if (name.Length > 60)
            name = figurine.product.name + " - " + figurine.product.series;

        string description = figurine.product.description;
        description += "\n\n🔍 Характеристики товара:\n";

        string AddLine(string label, string value)
        {
            int totalLength = 45;
            int dotsCount = Math.Max(2, totalLength - label.Length);
            return $"{label}{new string('.', dotsCount)}{value}\n";
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

        return new PushProductData()
        {
            name = name,
            description = description,
        };
    }

    private (uint width, uint height, uint depth) GetPackageVolume(FigurineVariation variation)
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

    private (string? primaryImage, List<string> otherImages) GetProductImages(FigurineReference figurine)
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

        return (primaryImage, otherImages);
    }

    private async Task<JsonDocument> PullProductAsync(string SKU)
    {
        object productSearchRequest = new
        {
            settings = new
            {
                sort = new
                {
                    ascending = false
                },
                filter = new
                {
                    textSearch = SKU,
                    withPhoto = -1
                },
                cursor = new
                {
                    limit = 1
                }
            }
        };

        string searchRequestJson = JsonSerializer.Serialize(productSearchRequest, jsonOptions);
        var searchResponseJson = await contentClient.POST("/content/v2/get/cards/list", searchRequestJson);

        return JsonDocument.Parse(searchResponseJson);
    }

    private class PushProductData
    {
        public required string name;
        public required string description;
    }
}