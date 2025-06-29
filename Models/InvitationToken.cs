using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Zorro.Data.Interfaces;

namespace PrintO.Models;

public class InvitationToken : IEntity, IDataTransferObject<object>, IAddable<InvitationToken.AddForm>, IUpdateable<InvitationToken.UseForm>
{
    [Key]
    public int Id { get; set; }

    [StringLength(64)]
    public string token { get; set; } = null!;

    public bool used { get; set; } = false;

    [ForeignKey(nameof(usedByUser))]
    public int? usedByUserId { get; set; }
    public User? usedByUser { get; set; }

    [ForeignKey(nameof(createdByUser))]
    public int createdByUserId { get; set; }
    public User createdByUser { get; set; } = null!;

    public DateTime createdAt { get; set; }
    public DateTime? usedAt { get; set; }

    public bool AddFill(AddForm form)
    {
        createdByUserId = form.createdByUserId;
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

    public object MapToDTO(object? argsObject = null)
    {
        return new
        {
            token,
        };
    }

    public struct AddForm
    {
        public int createdByUserId { get; set; }

        public AddForm(int createdByUserId)
        {
            this.createdByUserId = createdByUserId;
        }
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