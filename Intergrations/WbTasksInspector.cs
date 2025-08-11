using Dumpify;
using PrintO.Models;
using PrintO.Models.Integrations;
using PrintO.Models.Products;
using PrintO.Models.Products.Figurine;
using System.Collections.Concurrent;
using System.Text.Json;
using Zorro.Data;
using Zorro.Middlewares;

namespace PrintO.Intergrations;

public class WbTasksInspector : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    static readonly TimeSpan INSPECTION_SPAN = TimeSpan.FromSeconds(10);

    public WbTasksInspector(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();
            var taskRepo = scope.ServiceProvider.GetRequiredService<ModelRepository<WbIntegrationTask>>();
            var productRepo = scope.ServiceProvider.GetRequiredService<ModelRepository<Product>>();
            var figurineRepo = scope.ServiceProvider.GetRequiredService<ModelRepository<FigurineReference>>();
            var wbIntegration = scope.ServiceProvider.GetRequiredService<WbIntegration>();
            var minIORepo = scope.ServiceProvider.GetRequiredService<MinIORepository>();
            List<int> inspectTasks = taskRepo.GetAll(t => t.inProgress == true).Select(t => t.Id).ToList();

            ConcurrentDictionary<string, bool?> inclusion = new();
            inclusion.TryAdd("INCLUDE_FILES", true);
            inclusion.TryAdd("INCLUDE_IMAGES", true);
            inclusion.TryAdd("INCLUDE_VARIATIONS", true);

            for (int i = 0; i < inspectTasks.Count; i++)
            {
                int taskId = inspectTasks[i];
                WbIntegrationTask? task = taskRepo.FindById(taskId);
                if (task is null || task.inProgress is false)
                {
                    RemoveSelf();
                    continue;
                }
                FigurineReference figurine = figurineRepo.Find(f => f.productId == task.productId, inclusion)!;

                var images = figurine.product.images
                    .OrderBy(i => i.index)
                    .Select(i =>
                    {
                        var file = figurine.product.files.FirstOrDefault(f => f.Id == i.fileId);
                        if (file is null)
                            throw new Exception("File not found.");
                        return minIORepo.GetFullPath(file.filePath);
                    })
                    .ToList();

                List<(int nmID, int price, int discount)> priceMapping = new();

                foreach (var variation in figurine.variations) 
                {
                    if (!wbIntegration.HasFigurine(variation.separateSKU, out var rootEl))
                    {
                        AppendLogs(taskRepo, ref task, $"[INFO]\t{variation.separateSKU}: Inspection cycle completed. Product hasn't been added yet.");
                        continue;
                    }

                    if (!rootEl.TryGetProperty("cards", out var cardsEl))
                        throw new Exception("No 'cards' property in response.");

                    var pulledProductEl = cardsEl.EnumerateArray().FirstOrDefault();

                    if (!pulledProductEl.TryGetProperty("nmID", out var nmIDEl))
                        throw new Exception("No 'nmID' property in response.");
                    int nmID = nmIDEl.GetInt32();

                    object mediaUpdateRequest = new
                    {
                        nmId = nmID,
                        data = images
                    };

                    string mediaUpdateRequestJson = JsonSerializer.Serialize(mediaUpdateRequest, wbIntegration.jsonOptions);
                    var mediaUpdateTask = wbIntegration.contentClient.POST("/content/v3/media/save", mediaUpdateRequestJson);
                    try
                    {
                        string mediaUpdateResponseJson = mediaUpdateTask.GetAwaiter().GetResult();
                    }
                    catch (QueryException e)
                    {
                        UpdateSelfToError(e.Message);
                    }

                    int oldPrice = (int)variation.priceBeforeSaleRub;
                    int discount = (int)((oldPrice - (double)variation.priceRub) / oldPrice * 100);
                    priceMapping.Add((nmID, oldPrice, discount));
                }

                object priceUpdateRequest = new
                {
                    data = priceMapping.Select(priceData => new
                    {
                        priceData.nmID,
                        priceData.price,
                        priceData.discount
                    }).ToArray()
                };
                string priceUpdateRequestJson = JsonSerializer.Serialize(priceUpdateRequest, wbIntegration.jsonOptions);
                var priceUpdateTask = wbIntegration.priceClient.POST("/api/v2/upload/task", priceUpdateRequestJson);
                try
                {
                    string priceUpdateResponseJson = priceUpdateTask.GetAwaiter().GetResult();
                }
                catch (QueryException e)
                {
                    UpdateSelfToError(e.Message);
                }

                UpdateSelfToSuccess();

                void RemoveSelf() => inspectTasks.RemoveAt(i--);
                void UpdateSelfToSuccess()
                {
                    var productVersion = GetProductVersion(task.productId);
                    var updateForm = new IntegrationTask.UpdateForm(true, productVersion);
                    task.UpdateFill(updateForm);
                    AppendLogs(taskRepo, ref task, $"[INFO]\tBumped version to v{productVersion}\n.[SUCCESS]\tInspection cycle completed. Integration successful.\n");
                    taskRepo.Update(ref task);
                }
                void UpdateSelfToError(string errorMessage)
                {
                    var productVersion = GetProductVersion(task.productId);
                    var updateForm = new IntegrationTask.UpdateForm(false, productVersion);
                    task.UpdateFill(updateForm);
                    AppendLogs(taskRepo, ref task, errorMessage);
                    taskRepo.Update(ref task);
                }
                uint GetProductVersion(int productId)
                {
                    return productRepo.FindById(productId)!.version;
                }
            }

            await Task.Delay(INSPECTION_SPAN, stoppingToken);
        }
    }

    private static void AppendLogs(ModelRepository<WbIntegrationTask> repo, ref WbIntegrationTask task, string logs, IDictionary<string, bool?>? context = null)
    {
        var updateForm = new IntegrationTask.AppendLogsForm(logs);
        task.UpdateFill(updateForm);
    }
}