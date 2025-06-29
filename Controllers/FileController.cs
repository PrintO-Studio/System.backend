using Microsoft.AspNetCore.Mvc;
using Minio;
using Minio.DataModel;
using PrintO.Models;
using PrintO.Models.Products;
using PrintO.Query;
using Zorro.Data;
using Zorro.Middlewares;
using Zorro.Modules.JwtBearer.Attributes;
using Zorro.Query;
using Zorro.Query.Auxiliaries;
using Zorro.Query.Essentials;
using Zorro.Query.Essentials.Auth;
using Zorro.Query.Essentials.BucketRepository;
using Zorro.Query.Essentials.ModelRepository;

namespace PrintO.Controllers;

[ApiController]
[JwtAuthorize]
public class FileController : Controller
{
    [HttpPut]
    [Route("/products/{productId}/files")]
    [RequestSizeLimit((long)2e+9)] // 2Gb
    public IActionResult PutProductFiles(int productId, IFormFile[] files)
    {
        var minIORepo = HttpContext.RequestServices.GetService<MinIORepository>()!;

        return this.StartQuery()

        .CheckStoreMembership(out int selectedStoreId)

        .Eject(_ => _.FindById<Product>(productId), out var product)

        .If(product.storeId != selectedStoreId, _ => _
            .Throw(new QueryException(statusCode: StatusCodes.Status403Forbidden))
        )

        .Eject(_ => _.GetUser<User, int>(), out var me)

        .ForEach(files, (file, _) => _
            .GenerateId<Models.File>(out int fileId)

            .Eject($"{selectedStoreId}/{productId}/{fileId}/{file.FileName}", out var filePath)

            .Eject(new Models.File.AddForm(filePath, me.Id, productId, file.ContentType, file.Length), out var fileAdd)

            .Upload<MinIORepository, IMinioClient, Bucket, Item>(file, filePath)

            .Add<Models.File, Models.File.AddForm>(fileAdd, fileId)

            .MapToDTO<Models.File, object>(new { fullPath = minIORepo.GetFullPath(filePath) })
        )

        .EndAndReturn();
    }

    [HttpDelete]
    [Route("/products/{productId}/files/{id}")]
    public IActionResult DeleteProductFile(int productId, int id)
    {
        var minIORepo = HttpContext.RequestServices.GetService<MinIORepository>()!;

        return this.StartQuery()

        .CheckStoreMembership(out int selectedStoreId)

        .Eject(_ => _.FindById<Product>(productId), out var product)

        .If(product.storeId != selectedStoreId, _ => _
            .Throw(new QueryException(statusCode: StatusCodes.Status403Forbidden))
        )

        .Eject(_ => _.FindById<Models.File>(id), out var file)

        .Delete<MinIORepository, IMinioClient, Bucket, Item>(file.filePath)

        .Remove<Models.File>(file.Id)

        .End();
    }
}