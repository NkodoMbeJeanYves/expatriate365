using System.Security.Claims;
using MediatR;
using server.Application.Common;
using server.Application.Documents.Commands;
using server.Application.Documents.DTOs;
using server.Application.Documents.Queries;

namespace server.API.Documents;

public static class DocumentEndpoints
{
    public static void MapDocumentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/documents").WithTags("Documents").RequireAuthorization();

        group.MapGet("/stats", async (ClaimsPrincipal principal, IMediator mediator) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            return Results.Ok(await mediator.Send(new GetDocumentStatsQuery(tenantId.Value)));
        }).RequireAuthorization(Permissions.DocumentsRead);

        group.MapGet("/", async (ClaimsPrincipal principal, IMediator mediator,
            int page = 1, int limit = 20, string? type = null, string? category = null, string? search = null) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            return Results.Ok(await mediator.Send(new ListDocumentsQuery(tenantId.Value, page, limit, type, category, search)));
        }).RequireAuthorization(Permissions.DocumentsRead);

        group.MapPost("/", async (CreateDocumentRequest request, ClaimsPrincipal principal, IMediator mediator) =>
        {
            var tenantId = GetTenantId(principal);
            var userId = GetUserId(principal);
            if (tenantId is null || userId is null) return Results.Unauthorized();
            var result = await mediator.Send(new CreateDocumentCommand(tenantId.Value, userId.Value, request));
            return result.IsSuccess
                ? Results.Created($"/api/v1/documents/{result.Data!.Id}", result.Data)
                : Results.BadRequest(new { error = result.ErrorMessage });
        }).RequireAuthorization(Permissions.DocumentsUpload);

        group.MapPut("/{id:guid}", async (Guid id, UpdateDocumentRequest request,
            ClaimsPrincipal principal, IMediator mediator) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            var result = await mediator.Send(new UpdateDocumentCommand(tenantId.Value, id, request));
            return result.IsSuccess
                ? Results.Ok(result.Data)
                : Results.BadRequest(new { error = result.ErrorMessage });
        }).RequireAuthorization(Permissions.DocumentsManage);

        group.MapDelete("/{id:guid}", async (Guid id, ClaimsPrincipal principal, IMediator mediator) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            var result = await mediator.Send(new DeleteDocumentCommand(tenantId.Value, id));
            return result.IsSuccess
                ? Results.NoContent()
                : Results.NotFound(new { error = result.ErrorMessage });
        }).RequireAuthorization(Permissions.DocumentsManage);
    }

    private static Guid? GetTenantId(ClaimsPrincipal principal)
    {
        var claim = principal.FindFirst("tenant_id")?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }

    private static Guid? GetUserId(ClaimsPrincipal principal)
    {
        var claim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? principal.FindFirst("sub")?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}
