using Microsoft.AspNetCore.Mvc;
using PrintO.Models;
using PrintO.Models.Products;
using PrintO.Query;
using Zorro.Modules.JwtBearer.Attributes;
using Zorro.Query;
using Zorro.Query.Essentials;
using Zorro.Query.Essentials.ModelRepository;
using static PrintO.Models.FileTag;

namespace PrintO.Controllers;

[ApiController]
[JwtAuthorize]
public class FileTagController : Controller
{
    [HttpPost]
    [Route("/products/{productId}/files/{fileId}/tags")]
    public IActionResult PostFile(int productId, int fileId, [FromBody] UserAddForm input)
    {
        return this.StartQuery()

        .CheckStoreMembership(out int selectedStoreId)

        .Find<Models.File>(f => f.productId == productId && f.Id == fileId)

        .Eject(new AddForm(input, fileId), out var addForm)

        .Add<FileTag, AddForm>(addForm)

        .End();
    }

    [HttpPatch]
    [Route("/products/{productId}/files/{fileId}/tags/{id}")]
    public IActionResult PatchNote(int productId, int fileId, int id, [FromBody] UpdateForm input)
    {
        return this.StartQuery()

        .CheckStoreMembership(out int selectedStoreId)

        .Find<Product>(f => f.storeId == selectedStoreId && f.Id == productId)

        .Find<FileTag>(t => t.fileId == fileId && t.Id == id)

        .Update(input)

        .End();
    }

    [HttpDelete]
    [Route("/products/{productId}/files/{fileId}/tags/{id}")]
    public IActionResult DeleteNote(int productId, int fileId, int id)
    {
        return this.StartQuery()

        .CheckStoreMembership(out int selectedStoreId)

        .Find<Product>(f => f.storeId == selectedStoreId && f.Id == productId)

        .Find<FileTag>(t => t.fileId == fileId && t.Id == id)

        .Remove()

        .End();
    }
}