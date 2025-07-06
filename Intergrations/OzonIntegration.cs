using PrintO.Intergrations.Interfaces;
using PrintO.Models.Products;
using PrintO.Models.Products.Figurine;
using System.Text;
using System.Text.Json;
using Zorro.Data;
using static Zorro.Secrets.GetSecretValueUtility;

namespace PrintO.Intergrations;

public class OzonIntegration : IIntegradable<FigurineReference, FigurineVariation>
{
    protected HttpClient client { get; init; }

    public const string BASE_ADDRESS = "https://api-seller.ozon.ru/";

    private readonly ModelRepository<Product> _productRepo;
    private readonly ModelRepository<FigurineReference> _fRefRepo;
    private readonly ModelRepository<FigurineVariation> _fVarRepo;

    public OzonIntegration(ModelRepository<Product> productRepo,
        ModelRepository<FigurineReference> fRefRepo,
        ModelRepository<FigurineVariation> fVarRepo)
    {
        string API_KEY = GetSecretValue("/OZON", "API_KEY");
        string CLIENT_ID = GetSecretValue("/OZON", "CLIENT_ID");

        client = new HttpClient();
        client.DefaultRequestHeaders.Add("Api-Key", API_KEY);
        client.DefaultRequestHeaders.Add("Client-Id", CLIENT_ID);
        client.BaseAddress = new Uri(BASE_ADDRESS);

        SKUs = GetProductSKUListing();

        _productRepo = productRepo;
        _fRefRepo = fRefRepo;
        _fVarRepo = fVarRepo;
    }

    private string[] SKUs;
    public string[] GetProductSKUListing()
    {
        string jsonRequest = "{" +
            "\"filter\": {" +
            "   \"visibility\": " +
            "   \"ALL\"}," +
            "   \"last_id\": \"\"," +
            "   \"limit\": 1000" +
            "}";

        var postTask = POSTAsync("/v3/product/list", jsonRequest);
        string resultRaw = postTask.GetAwaiter().GetResult();

        using JsonDocument doc = JsonDocument.Parse(resultRaw);

        if (!doc.RootElement.TryGetProperty("result", out JsonElement resultElement))
            throw new Exception("No 'result' property in response.");

        if (!resultElement.TryGetProperty("items", out JsonElement itemsElement))
            throw new Exception("No 'items' property in 'result'.");

        List<string> offerIds = new List<string>();

        foreach (JsonElement item in itemsElement.EnumerateArray())
        {
            if (item.TryGetProperty("offer_id", out JsonElement offerIdElement))
            {
                offerIds.Add(offerIdElement.GetString()!);
            }
        }

        return offerIds.ToArray();
    }

    public bool UploadProduct(FigurineReference product)
    {
        throw new NotImplementedException();
    }

    private async Task<string> POSTAsync(string url, string jsonData)
    {
        var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
        var response = await client.PostAsync(url, content);
        return await response.Content.ReadAsStringAsync();
    }

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
}