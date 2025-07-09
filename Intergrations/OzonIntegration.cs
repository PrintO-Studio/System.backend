using Dumpify;
using PrintO.Intergrations.Interfaces;
using PrintO.Models;
using PrintO.Models.Products;
using PrintO.Models.Products.Figurine;
using System.Text;
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
    protected HttpClient client { get; init; }

    public const string BASE_ADDRESS = "https://api-seller.ozon.ru/";

    static readonly CATEGORY FIGURINE_CATEGORY = (17028973, 115944384);
    static readonly CATEGORY TABLE_GAME_ACCESSORY_CATEGORY = (88076545, 92885);

    private readonly ModelRepository<Product> _productRepo;
    private readonly ModelRepository<FigurineReference> _fRefRepo;
    private readonly ModelRepository<FigurineVariation> _fVarRepo;
    private readonly ModelRepository<Models.File> _fFileRepo;
    private readonly ModelRepository<ImageReference> _fImageRepo;
    private readonly MinIORepository _minIORepo;

    public OzonIntegration(ModelRepository<Product> productRepo,
        ModelRepository<FigurineReference> fRefRepo,
        ModelRepository<FigurineVariation> fVarRepo,
        ModelRepository<Models.File> fFileRepo,
        ModelRepository<ImageReference> fImageRepo,
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
        _minIORepo = minIORepo;
    }

    public object UploadFigurine(FigurineReference figurine)
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

        var primaryImage = images.FirstOrDefault();
        var otherImages = images[1..];

        List<object> items = new List<object>();

        foreach (FigurineVariation variation in figurine.variations)
        {
            if (variation.isActive is false)
                continue;

            string variationSKU = variation.separateSKU;
            CATEGORY selectedCategory = FIGURINE_CATEGORY;

            object pullRequest = new
            {
                filter = new
                {
                    offer_id = new[] { variationSKU },
                    visibility = "ALL"
                },
                limit = 1,
                sort_dir = "ASC"
            };

            bool isNewVariation = false;
            try
            {
                var pullTask = POSTAsync("/v4/product/info/attributes", JsonSerializer.Serialize(pullRequest));
                string pullResponseJson = pullTask.GetAwaiter().GetResult();
                using JsonDocument pullDoc = JsonDocument.Parse(pullResponseJson);

                if (isNewVariation is false)
                {
                    if (!pullDoc.RootElement.TryGetProperty("result", out JsonElement pullResultElement))
                        throw new Exception("No 'result' property in response.");

                    var pullCategory = pullResultElement.EnumerateArray().FirstOrDefault().GetProperty("description_category_id").GetInt64();
                    if (pullCategory == FIGURINE_CATEGORY.CATEGORY_ID)
                        selectedCategory = FIGURINE_CATEGORY;
                    else if (pullCategory == TABLE_GAME_ACCESSORY_CATEGORY.CATEGORY_ID)
                        selectedCategory = TABLE_GAME_ACCESSORY_CATEGORY;
                }
            }
            catch (QueryException ex)
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
            description += $"\t🎨 Цвет:\t{(variation.color == Enums.Color.Gray ? "серый" : "белый")}\n";
            if (variation.scale.HasValue)
            {
                description += $"\t📐 Масштаб:\t{scaleLabel}\n";
            }
            {
                description += $"\t🧱 Целостность:\t";
                if (variation.integrity == Enums.Integrity.Solid)
                    description += "Миниатюра доставляется цельной";
                else if (variation.integrity == Enums.Integrity.Dismountable)
                    description += "Миниатюра доставляется в разборе";
                else if (variation.integrity == Enums.Integrity.DismountableBase)
                    description += "Подставка отсоединяется от миниатюры";
                description += $"\n";
            }
            description += $"\t🧍 Количество фигурок:\t{variation.quantity}\n";
            description += $"\t⚖️ Вес фигурки:\t{variation.weightGr} гр.\n";
            if (variation.heightMm.HasValue)
                description += $"\t📏 Высота фигурки:\t\t\t{variation.heightMm} мм.\n";
            if (variation.widthMm.HasValue)
                description += $"\t📏 Ширина фигурки:\t\t\t{variation.widthMm} мм.\n";
            if (variation.depthMm.HasValue)
                description += $"\t📏 Длина фигурки:\t\t\t{variation.depthMm} мм.\n";
            if (variation.minHeightMm.HasValue)
                description += $"\t↘️ Минимальная высота:\t\t\t{variation.minHeightMm} мм.\n";
            if (variation.averageHeightMm.HasValue)
                description += $"\t📊 Средняя высота:\t\t\t{variation.averageHeightMm} мм.\n";
            if (variation.maxHeightMm.HasValue)
                description += $"\t⬆️ Максимальная высота:\t\t\t{variation.maxHeightMm} мм.\n";

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

            items.Add(item);
        }

        return UploadFigurine();

        return true;

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

        long UploadFigurine()
        {
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic),
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            };

            string json = JsonSerializer.Serialize(new { items }, options);

            //System.IO.File.WriteAllText("./test_upload.json", json, Encoding.UTF8);
            string response = POSTAsync("/v3/product/import", json).GetAwaiter().GetResult();
            JsonDocument postDoc = JsonDocument.Parse(response);

            if (!postDoc.RootElement.TryGetProperty("result", out JsonElement postResultElement))
                throw new Exception("No 'result' property in response.");

            if (!postResultElement.TryGetProperty("task_id", out JsonElement taskIdElement))
                throw new Exception("No 'items' property in 'result'.");

            return taskIdElement.GetInt64();
        }
    }

    private async Task<string> POSTAsync(string url, string jsonData)
    {
        var requestContent = new StringContent(jsonData, Encoding.UTF8, "application/json");
        var response = await client.PostAsync(url, requestContent);
        string responseContent = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode is false)
        {
            throw new QueryException(responseContent);
        }
        return responseContent;
    }

    /*
    public async Task UploadProductsAsync()
    {
        List<SheetProduct> products = JsonSerializer.Deserialize<SheetProduct[]>(System.IO.File.ReadAllText("./products.json", Encoding.UTF8))!.ToList();

        if (products is null)
            throw new Exception();

        string description = System.IO.File.ReadAllText("./description.txt", Encoding.UTF8);

        List<string> SKUsToSkip = new();
        List<string> SKUtoShip = new() { "91.2", "127.2", "127.3", "474", "343", "218.65", "218.83", "267.5", "274.1", "274.2", "274.4", "274.5", "274.7", "274.8", "274.9", "274.10", "386", "478.6" };

        int fCounter = 10000000;

        foreach (SheetProduct sheetProduct in products)
        {
            if (SKUsToSkip.Contains(sheetProduct.SKU))
                continue;
            string productImagesDir = $"./images/{sheetProduct.SKU}";

            //
            int imageCounter = 0;
            Directory.CreateDirectory(productImagesDir);
            foreach (string image in product.images)
            {
                string path = $"{productImagesDir}/{++imageCounter}.jpg";
                if (System.IO.File.Exists(path))
                {
                    //Console.WriteLine($"[SKIP]: {path}");
                    continue;
                }

                try
                {
                    using var client = new HttpClient();
                    client.Timeout = TimeSpan.FromSeconds(10);
                    using var stream = await client.GetStreamAsync(image);
                    using var filestream = new FileStream(path, FileMode.OpenOrCreate);
                    await stream.CopyToAsync(filestream);
                    Console.WriteLine($"[SUCCESS]: {path}");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"[ERROR]: {path} ({e.Message})");
                }
            }
            //

            Product product = new Product();
            Product.UserAddForm pUserAdd = new();
            pUserAdd.SKU = sheetProduct.SKU;
            pUserAdd.name = GetName(sheetProduct);
            pUserAdd.series = sheetProduct.series;
            pUserAdd.description = description;
            Product.AddForm pAdd = new(pUserAdd, 1);
            product.AddFill(pAdd);
            int pId = _productRepo.Add(product)!.Id;

            FigurineReference figurine = new FigurineReference();
            FigurineReference.AddForm fAdd = new(pId);
            figurine.AddFill(fAdd);
            int fId = _fRefRepo.Add(figurine)!.Id;

            List<SheetProduct> same = products.FindAll(p => GetName(p) == pUserAdd.name && p.series == pUserAdd.series);
            if (same.RemoveAll(s => SKUtoShip.Contains(s.SKU) && product.SKU != s.SKU) > 0)
            {
                SKUtoShip.Add(product.SKU);
            }
            SKUsToSkip.AddRange(same.Select(s => s.SKU));

            //AddVariattion(sheetProduct);
            same.ForEach(s => AddVariattion(s));

            var images = Directory.GetFiles(productImagesDir, "*.jpg").ToList();
            int imageCounter = 0;
            images.ForEach(i => AddImage(i));

            void AddImage(string path)
            {
                string fileName = Path.GetFileName(path);
                string filePath = $"1/{pId}/{fCounter++}/{fileName}";

                using var fileStream = new FileStream(path, FileMode.Open);
                _minIORepo.UploadAsync(fileStream, filePath, "image/jpeg");
                Models.File file = new Models.File();
                Models.File.AddForm fAdd = new Models.File.AddForm(filePath, 1, pId, "image/jpeg", fileStream.Length);
                file.AddFill(fAdd);
                int fId = _fFileRepo.Add(file)!.Id;

                ImageReference image = new();
                ImageReference.AddForm iAdd = new ImageReference.AddForm(fId, pId, imageCounter++);
                image.AddFill(iAdd);
                int iId = _fImageRepo.Add(image)!.Id;
            }

            void AddVariattion(SheetProduct p)
            {
                FigurineVariation variation = new();
                FigurineVariation.UserAddForm vUserAdd = new();
                vUserAdd.isActive = true;
                vUserAdd.name = p.colorName ?? GetName(p);
                vUserAdd.color = p.color ?? Color.Gray;
                vUserAdd.weightGr = (uint)p.weight.GetValueOrDefault(50);
                vUserAdd.heightMm = (uint?)p.height;
                vUserAdd.widthMm = (uint?)p.width;
                vUserAdd.depthMm = (uint?)p.depth;
                ulong priceRounded = (ulong)MathF.Round(p.price / 50f) * 50,
                    oldPriceRounded = (ulong)MathF.Round((priceRounded * 1.4f) / 100f) * 100 - 1;
                vUserAdd.priceRub = priceRounded;
                vUserAdd.priceBeforeSaleRub = oldPriceRounded;
                vUserAdd.minimalPriceRub = priceRounded - 150;
                vUserAdd.integrity = p.integrity;
                vUserAdd.quantity = (uint)p.quantity;
                FigurineVariation.AddForm vAdd = new(vUserAdd, fId, p.SKU);
                variation.AddFill(vAdd);
                int vId = _fVarRepo.Add(variation)!.Id;
                Console.WriteLine($"Added prod SKU:{p.SKU} at variation {vId}");
            }

            string GetName(SheetProduct product)
            {
                if (string.IsNullOrEmpty(product.suffix) is false)
                    return $"{product.name} / {product.suffix}";
                else
                    return product.name;
            }
        }
    }
    */

    /*
    public void PullSheetData()
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
        };
        using (var reader = new StreamReader("products.csv"))
        using (var csv = new CsvReader(reader, config))
        {
            var records = csv.GetRecords<SheetProductRaw>();

            var parsedProducts = records.Select(pr =>
            {
                string? prefix = null, name = null, suffix = null;
                string?[] nameComponents = [null, null, null];
                var nComponents = pr.name.Split(';');
                if (nComponents.Length == 1)
                {
                    name = nComponents[0].Trim();
                }
                else if (nComponents.Length == 2)
                {
                    prefix = nComponents[0].Trim();
                    name = nComponents[1].Trim();
                }
                else if (nComponents.Length == 3)
                {
                    if (!string.IsNullOrEmpty(nComponents[0].Trim()))
                        prefix = nComponents[0].Trim();
                    name = nComponents[1].Trim();
                    suffix = nComponents[2].Trim();
                }

                Color? c = null;
                if (pr.color == "серый")
                    c = Color.Gray;
                else if (pr.color == "белый")
                    c = Color.White;

                int? width = null, height = null, depth = null, baseDiameter = null;
                if (string.IsNullOrEmpty(pr.dimensions) is false)
                {
                    int?[] dimensionsComponents = [null, null, null, null];
                    var dComponents = pr.dimensions.Split('/');
                    for (int i = 0; i < dComponents.Length; i++)
                    {
                        if (int.TryParse(dComponents[i].Trim(), out int parsedComponent))
                        {
                            dimensionsComponents[i] = parsedComponent;
                        }
                    }
                    width = dimensionsComponents[0];
                    height = dimensionsComponents[1];
                    depth = dimensionsComponents[2];
                    baseDiameter = dimensionsComponents[3];
                }

                Integrity integrity;
                if (pr.integrity == "1")
                    integrity = Integrity.Dismountable;
                else if (pr.integrity == "2")
                    integrity = Integrity.DismountableBase;
                else
                    integrity = Integrity.Solid;

                bool enabled;
                if (pr.enabled == "Active")
                    enabled = true;
                else
                    enabled = false;

                return new SheetProduct()
                {
                    SKU = pr.SKU,
                    prefix = prefix,
                    name = name!,
                    suffix = suffix,
                    weight = pr.weight,
                    price = pr.price,
                    quantity = pr.quantity,
                    color = c,
                    width = width,
                    height = height,
                    depth = depth,
                    baseDiameter = baseDiameter,
                    series = pr.series,
                    integrity = integrity,
                    joinName = pr.joinName,
                    colorName = pr.colorName,
                    enabled = enabled
                };
            });

            var enabledProducts = parsedProducts.ToList();
            enabledProducts.RemoveAll(p => p.enabled is false);

            enabledProducts.RemoveAll(p => SKUs.Contains(p.SKU) is false);

            for(int i = 0; i < enabledProducts.Count; i++)
            {
                Console.WriteLine(enabledProducts[i].SKU);

                string priceJsonRequest = "{" +
                    "\"cursor\": \"\"," +
                    "\"filter\": {" +
                    $"   \"offer_id\": [\"{enabledProducts[i].SKU}\"]," +
                    "   \"visibility\": \"ALL\"" +
                    "}," +
                    "\"limit\": 100" +
                "}";

                var priceResultRaw = POSTAsync("/v5/product/info/prices", priceJsonRequest).GetAwaiter().GetResult();

                using JsonDocument priceDoc = JsonDocument.Parse(priceResultRaw);

                if (!priceDoc.RootElement.TryGetProperty("items", out JsonElement itemsElement))
                    throw new Exception("No 'items' property in 'result'.");

                enabledProducts[i].price = itemsElement.EnumerateArray().ElementAt(0).GetProperty("price").GetProperty("price").GetInt32();

                string imagesJsonRequest = "{" +
                    "\"filter\": {" +
                    $"   \"offer_id\": [\"{enabledProducts[i].SKU}\"]," +
                    "   \"visibility\": \"ALL\"" +
                    "}," +
                    "\"limit\": 100," +
                    "\"sort_dir\": \"ASC\"" +
                "}";

                var imagesResultRaw = POSTAsync("/v4/product/info/attributes", imagesJsonRequest).GetAwaiter().GetResult();

                using JsonDocument imagesDoc = JsonDocument.Parse(imagesResultRaw);

                if (!imagesDoc.RootElement.TryGetProperty("result", out JsonElement imagesElement))
                    throw new Exception("No 'items' property in 'result'.");

                var primaryImage = imagesElement.EnumerateArray().ElementAt(0).GetProperty("primary_image").GetString()!;
                var otherImages = imagesElement.EnumerateArray().ElementAt(0).GetProperty("images").EnumerateArray().Select(i => i.GetString());
                var imagesList = otherImages.ToList();
                imagesList.Insert(0, primaryImage);
                enabledProducts[i].images = imagesList.ToArray();
            }

            JsonSerializerOptions options = new JsonSerializerOptions()
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = true,
            };
            File.WriteAllText("products.json", JsonSerializer.Serialize(enabledProducts, options));
        }
    }

    public class SheetProductRaw
    {
        public string SKU { get; set; }
        public string name { get; set; }
        public int? weight { get; set; }
        public int price { get; set; }
        public int min_price { get; set; }
        public int quantity { get; set; }
        public string color { get; set; }
        public string? dimensions { get; set; }
        public string? series { get; set; }
        public string? tags { get; set; }
        public string printer { get; set; }
        public string integrity { get; set; }
        public string? joinName { get; set; }
        public string? colorName { get; set; }
        public string enabled { get; set; }
    }
    */

    /*
    public class SheetProduct
    {
        public string SKU { get; set; }
        public bool enabled { get; set; }
        public string? prefix { get; set; }
        public string name { get; set; }
        public string? suffix { get; set; }
        public int? weight { get; set; }
        public int price { get; set; }
        public int quantity { get; set; }
        public Color? color { get; set; }
        public int? width { get; set; }
        public int? height { get; set; }
        public int? depth { get; set; }
        public int? baseDiameter { get; set; }
        public string? series { get; set; }
        public Integrity integrity { get; set; }
        public string? joinName { get; set; }
        public string? colorName { get; set; }
        public string[] images { get; set; }
    }
    */
}