using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Zorro.Data.Attributes;
using Zorro.Data.Interfaces;
using static PrintO.Models.User;

namespace PrintO.Models;

public class User : IdentityUser<int>, IEntity, IDTO<UserDTO>, IUpdateable<SelectStoreForm>
{
    int IEntity.Id
    {
        get => Id;
        set => Id = value;
    }

    //[ForeignKey(nameof(selectedStore))]
    public int? selectedStoreId { get; set; }
    public Store? selectedStore { get; set; }

    [IncludeWhen("INCLUDE_MEMBERSHIPS"), InclusionDepth(1)]
    [InverseProperty(nameof(Store.members))]
    public virtual ICollection<Store> memberships { get; set; } = new List<Store>();

    [StringLength(USER_NAME_MAX_LENGTH)]
    public override string? UserName { get => base.UserName; set => base.UserName = value; }

    public bool isAdmin { get; set; }

    public bool UpdateFill(SelectStoreForm form)
    {
        selectedStoreId = form.storeId;

        return true;
    }

    public UserDTO MapToDTO(Zorro.Query.HttpQueryContext context)
    {
        return new UserDTO()
        {
            id = Id,
            userName = UserName,
            selectedStoreId = selectedStoreId
        };
    }

    public const int USER_NAME_MAX_LENGTH = 100;

    public struct UserDTO
    {
        public int id { get; set; }
        public string? userName { get; set; }
        public int? selectedStoreId { get; set; }
    }

    public struct SelectStoreForm
    {
        public int storeId { get; set; }

        public SelectStoreForm(int storeId)
        {
            this.storeId = storeId;
        }
    }
}