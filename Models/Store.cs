using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Zorro.Data.Attributes;
using Zorro.Data.Interfaces;
using static PrintO.Models.Store;

namespace PrintO.Models;

public class Store : IEntity, IDataTransferObject<StoreDTO>, IAddable<AddForm>, IUpdateable<UserJoinForm>
{
    [Key]
    public int Id { get; set; }

    [StringLength(STORE_NAME_MAX_LENGTH)]
    public string name { get; set; } = null!;

    [IncludeWhen("INCLUDE_MEMBERS"), InclusionDepth(1)]
    [InverseProperty(nameof(User.memberships))]
    public virtual ICollection<User> members { get; set; } = new List<User>();

    public virtual ICollection<Products.Product> products { get; set; } = new List<Products.Product>();

    public bool AddFill(AddForm form)
    {
        name = form.name;

        return true;
    }

    public bool UpdateFill(UserJoinForm form)
    {
        members.Add(form.user);

        return true;
    }

    public StoreDTO MapToDTO(object? argsObject = null)
    {
        var dto = new StoreDTO()
        {
            id = Id,
            name = name,
        };

        return dto;
    }

    public const int STORE_NAME_MAX_LENGTH = 40;

    public struct AddForm
    {
        public string name { get; set; }
    }

    public struct UserJoinForm
    {
        public User user { get; set; }

        public UserJoinForm(User user)
        {
            this.user = user;
        }
    }

    public struct StoreDTO
    {
        public int id { get; set; }
        public string name { get; set; }
    }
}