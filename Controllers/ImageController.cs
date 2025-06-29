using Microsoft.AspNetCore.Mvc;
using PrintO.Models;
using PrintO.Models.Products;
using PrintO.Query;
using Zorro.Middlewares;
using Zorro.Modules.JwtBearer.Attributes;
using Zorro.Query;
using Zorro.Query.Essentials;
using Zorro.Query.Essentials.ModelRepository;

namespace PrintO.Controllers;

[ApiController]
[JwtAuthorize]
public class ImageController : Controller
{
    [HttpPost]
    [Route("products/{productId}/images")]
    public IActionResult PostProductImages(int productId, int[] files)
    {
        int orderCounter = 0;

        return this.StartQuery()

        .CheckStoreMembership(out int selectedStoreId)

        .Eject(_ => _.FindById<Product>(productId), out var product)

        .If(product.storeId != selectedStoreId, _ => _
            .Throw(new QueryException(statusCode: StatusCodes.Status403Forbidden))
        )

        .Eject(_ => _.GetAllWhere<ImageReference>(i => i.productId == productId), out var initialImagesEnumerable)

        .Eject(initialImagesEnumerable.ToArray(), out var initialImages)

        .Eject(initialImages.Length, out var initialImagesCount)

        .If(initialImagesCount > files.Length, _ => _
            .Loop(initialImagesCount - files.Length, (i, _) => _
                //.Execute(() => $"REMOVING IMAGE WITH FILE ID {initialImages[initialImages.Length - i - 1].fileId}".Dump())

                .Remove<ImageReference>(initialImages[initialImages.Length - i - 1].Id)
            )
        )

        .If(initialImagesCount < files.Length, _ => _
            .Loop(files.Length - initialImagesCount, (i, _) => _
                .Eject(files.Length - (++orderCounter), out var imageIndex)

                //.Execute(() => $"ADDING IMAGE WITH FILE ID {files[files.Length - i - 1]} AT {imageIndex}".Dump())

                .Eject(new ImageReference.AddForm(files[files.Length - i - 1], productId, imageIndex), out var addForm)

                .Add<ImageReference, ImageReference.AddForm>(addForm)
            )
        )

        .Loop(Math.Min(initialImagesCount, files.Length), (i, _) => _
            .Eject(orderCounter++, out var imageIndex)

            //.Execute(() => $"UPDATING IMAGE WITH FILE ID {files[i]} AND INDEX {i}".Dump())

            .Eject(new ImageReference.UpdateForm(i, files[i]), out var updateForm)

            .Update<ImageReference, ImageReference.UpdateForm>(initialImages[i].Id, updateForm)
        )

        .EndAndReturn();
    }
}