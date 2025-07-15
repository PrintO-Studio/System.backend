using PrintO.Models;
using PrintO.Models.Integrations;
using PrintO.Models.Products;
using System.Text.Json;
using Zorro.Data;
using Zorro.Middlewares;
using static Zorro.Secrets.GetSecretValueUtility;

namespace PrintO.Intergrations;

public class OzonTasksInspector : BackgroundService
{
    public static HttpClient client { get; private set; } = null!;

    public const string BASE_ADDRESS = "https://api-seller.ozon.ru/";

    private readonly IServiceScopeFactory _scopeFactory;

    static readonly TimeSpan INSPECTION_SPAN = TimeSpan.FromSeconds(10);

    public OzonTasksInspector(IServiceScopeFactory scopeFactory)
    {
        string API_KEY = GetSecretValue("/OZON", "API_KEY");
        string CLIENT_ID = GetSecretValue("/OZON", "CLIENT_ID");

        client = new HttpClient();
        client.DefaultRequestHeaders.Add("Api-Key", API_KEY);
        client.DefaultRequestHeaders.Add("Client-Id", CLIENT_ID);
        client.BaseAddress = new Uri(BASE_ADDRESS);

        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();
            var taskRepo = scope.ServiceProvider.GetRequiredService<ModelRepository<OzonIntegrationTask>>();
            var productRepo = scope.ServiceProvider.GetRequiredService<ModelRepository<Product>>();
            List<int> inspectTasks = taskRepo.GetAll(t => t.inProgress == true).Select(t => t.Id).ToList();

            for (int i = 0; i < inspectTasks.Count; i++)
            {
                int taskId = inspectTasks[i];
                OzonIntegrationTask? task = taskRepo.FindById(taskId);
                if (task is null || task.inProgress is false)
                {
                    RemoveSelf();
                    continue;
                }

                object statusRequest = new
                {
                    task_id = task.taskId
                };
                string statusRequestJson = JsonSerializer.Serialize(statusRequest);

                string responseJson = "{}";
                try
                {
                    var statusRequestTask = client.POST("/v1/product/import/info", statusRequestJson);
                    responseJson = statusRequestTask.GetAwaiter().GetResult();
                }
                catch (QueryException ex)
                {
                    UpdateSelfToError($"[ERROR]\tEncountered response {ex.statusCode} code error with message:\n{ex.Message}\n");
                }

                JsonDocument statusDoc = JsonDocument.Parse(responseJson);

                if (!statusDoc.RootElement.TryGetProperty("result", out JsonElement postResultElement))
                    throw new Exception("No 'result' property in response.");

                if (!postResultElement.TryGetProperty("items", out JsonElement itemsElement))
                    throw new Exception("No 'items' property in 'result'.");

                var firstItem = itemsElement.EnumerateArray().FirstOrDefault();
                string? statusText = firstItem.GetProperty("status").GetString();
                string errorsRawText = firstItem.GetProperty("errors").GetRawText();
                if (string.IsNullOrEmpty(statusText))
                {
                    UpdateSelfToError("[ERROR]\tStatus text is empty.\n");
                }

                switch (statusText)
                {
                    case "pending":
                        {
                            AppendLogs(taskRepo, ref task, "[INFO]\tInspection cycle completed. Integration status is pending...\n");
                            continue;
                        }
                    case "imported":
                        {
                            UpdateSelfToSuccess();
                            break;
                        }
                    case "failed":
                        {
                            UpdateSelfToError($"[ERROR]\t:\n```{errorsRawText}```\n[ERROR]\tInspection cycle completed. Integration failed.\n");
                            break;
                        }
                    case "skipped":
                        {
                            UpdateSelfToError($"[ERROR]\t:\n```{errorsRawText}```\n[ERROR]\tInspection cycle completed. Integration was skipped.\n");
                            break;
                        }
                }

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

    private static void AppendLogs(ModelRepository<OzonIntegrationTask> repo, ref OzonIntegrationTask task, string logs, IDictionary<string, bool?>? context = null)
    {
        var updateForm = new IntegrationTask.AppendLogsForm(logs);
        task.UpdateFill(updateForm);
        //repo.Update(ref task, context);
    }
}