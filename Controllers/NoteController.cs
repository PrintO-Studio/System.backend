using Microsoft.AspNetCore.Mvc;
using PrintO.Models;
using PrintO.Models.Products;
using PrintO.Query;
using Zorro.Modules.JwtBearer.Attributes;
using Zorro.Query;
using Zorro.Query.Essentials;
using Zorro.Query.Essentials.ModelRepository;
using static PrintO.Models.Note;

namespace PrintO.Controllers;

[ApiController]
[JwtAuthorize]
public class NoteController : Controller
{
    [HttpPost]
    [Route("/products/{productId}/notes")]
    public IActionResult PostNote(int productId, [FromBody] UserAddForm input)
    {
        return this.StartQuery()

        .CheckStoreMembership(out int selectedStoreId)

        .Find<Product>(f => f.storeId == selectedStoreId && f.Id == productId)

        .Eject(new AddForm(input, productId), out var addForm)

        .Add<Note, AddForm>(addForm)

        .MapToDTO<Note, object>()

        .EndAndReturn();
    }

    [HttpPatch]
    [Route("/products/{productId}/notes/{id}")]
    public IActionResult PatchNote(int productId, int id, [FromBody] UpdateForm input)
    {
        return this.StartQuery()

        .CheckStoreMembership(out int selectedStoreId)

        .Find<Product>(f => f.storeId == selectedStoreId && f.Id == productId)

        .Find<Note>(n => n.productId == productId && n.Id == id)

        .Update(input)

        .End();
    }

    [HttpDelete]
    [Route("/products/{productId}/notes/{id}")]
    public IActionResult DeleteNote(int productId, int id)
    {
        return this.StartQuery()

        .CheckStoreMembership(out int selectedStoreId)

        .Find<Product>(f => f.storeId == selectedStoreId && f.Id == productId)

        .Find<Note>(n => n.productId == productId && n.Id == id)

        .Remove()

        .End();
    }
}