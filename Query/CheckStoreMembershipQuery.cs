using PrintO.Models;
using Zorro.Middlewares;
using Zorro.Query;
using Zorro.Query.Essentials;
using Zorro.Query.Essentials.Auth;
using Zorro.Query.Essentials.ModelRepository;

namespace PrintO.Query;

public static class CheckStoreMembershipQuery
{
    public static QueryContext CheckStoreMembership(
        this QueryContext context,
        out int outSelectedStoreId
    )
    {
        context
            .Eject(GetUserQuery.GetUser<User, int>, out var me)

            .If(me.selectedStoreId.HasValue is false, _ => _
                .Throw(new (statusCode: StatusCodes.Status400BadRequest))
            )

            .Eject(me.selectedStoreId!.Value, out int selectedStoreId)

            .SetInclusion("INCLUDE_MEMBERS")

            .Eject(_ => _.FindById<Store>(selectedStoreId), out var store)

            .If(store.members.Any(m => m.Id == me.Id) is false, _ => _
                .Throw(new (statusCode: StatusCodes.Status403Forbidden))
            );

        outSelectedStoreId = selectedStoreId;

        return context;
    }
}