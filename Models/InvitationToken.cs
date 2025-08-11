using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Zorro.Data.Interfaces;

namespace PrintO.Models;

public class InvitationToken : IEntity, IDTO<object>, IAddable<InvitationToken.AddForm>, IUpdateable<InvitationToken.UseForm>
{
    [Key]
    public int Id { get; set; }

    [StringLength(64)]
    public string token { get; set; } = null!;

    public bool used { get; set; } = false;

    [ForeignKey(nameof(usedByUser))]
    public int? usedByUserId { get; set; }
    public User? usedByUser { get; set; }

    public DateTime createdAt { get; set; }
    public DateTime? usedAt { get; set; }

    public bool AddFill(AddForm form)
    {
        createdAt = DateTime.UtcNow;

        token = Guid.NewGuid().ToString();

        return true;
    }

    public bool UpdateFill(UseForm form)
    {
        if (used)
            return false;

        usedByUserId = form.usedByUserId;
        usedAt = DateTime.UtcNow;
        used = true;

        return true;
    }

    public object MapToDTO(Zorro.Query.HttpQueryContext context)
    {
        return new
        {
            token,
        };
    }

    public struct AddForm
    {
        
    }

    public struct UseForm
    {
        public int usedByUserId { get; set; }

        public UseForm(int usedByUserId)
        {
            this.usedByUserId = usedByUserId;
        }
    }
}