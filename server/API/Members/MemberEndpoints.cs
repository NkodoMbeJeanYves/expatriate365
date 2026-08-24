using System.Security.Claims;
using MediatR;
using server.Application.Common;
using server.Application.Members.Commands;
using server.Application.Members.DTOs;
using server.Application.Members.Queries;

namespace server.Api.Members;

public static class MemberEndpoints
{
    public static void MapMemberEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/members").WithTags("Members").RequireAuthorization();

        group.MapGet("/", async (
            ClaimsPrincipal principal, IMediator mediator,
            int page = 1, int limit = 20,
            string? search = null, string? status = null, string? category_id = null) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();

            var result = await mediator.Send(new GetMembersQuery(tenantId.Value, page, limit, search, status, category_id));
            return Results.Ok(result);
        }).RequireAuthorization(Permissions.MembersRead);

        group.MapGet("/categories", async (ClaimsPrincipal principal, IMediator mediator) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            return Results.Ok(await mediator.Send(new GetMemberCategoriesQuery(tenantId.Value)));
        });

        group.MapPost("/categories", async (
            ClaimsPrincipal principal, IMediator mediator, CreateCategoryRequest dto) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            var result = await mediator.Send(new CreateCategoryCommand(tenantId.Value, dto));
            return result.IsSuccess ? Results.Created($"/api/v1/members/categories", result.Data) : Results.BadRequest(new { error = result.ErrorMessage });
        }).RequireAuthorization(Permissions.CategoriesCreate);

        group.MapPut("/categories/{id:guid}", async (
            Guid id, ClaimsPrincipal principal, IMediator mediator, CreateCategoryRequest dto) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            var result = await mediator.Send(new UpdateCategoryCommand(tenantId.Value, id, dto));
            return result.IsSuccess ? Results.Ok(result.Data) : Results.BadRequest(new { error = result.ErrorMessage });
        }).RequireAuthorization(Permissions.CategoriesUpdate);

        group.MapDelete("/categories/{id:guid}", async (
            Guid id, ClaimsPrincipal principal, IMediator mediator) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            var result = await mediator.Send(new DeleteCategoryCommand(tenantId.Value, id));
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.ErrorMessage });
        }).RequireAuthorization(Permissions.CategoriesDelete);

        group.MapGet("/export", async (
            ClaimsPrincipal principal, IMediator mediator, string? status = null) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            var bytes = await mediator.Send(new ExportMembersQuery(tenantId.Value, status));
            return Results.File(bytes, "text/csv", $"membres_{DateTime.UtcNow:yyyyMMdd}.csv");
        }).RequireAuthorization(Permissions.MembersExport);

        group.MapGet("/{id:guid}", async (
            Guid id, ClaimsPrincipal principal, IMediator mediator) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            var result = await mediator.Send(new GetMemberByIdQuery(tenantId.Value, id));
            return result.IsSuccess ? Results.Ok(result.Data) : Results.NotFound(new { error = result.ErrorMessage });
        }).RequireAuthorization(Permissions.MembersReadOwn);

        group.MapPost("/", async (
            ClaimsPrincipal principal, IMediator mediator, CreateMemberRequest dto) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            var result = await mediator.Send(new CreateMemberCommand(tenantId.Value, dto));
            return result.IsSuccess
                ? Results.Created($"/api/v1/members/{result.Data!.Id}", result.Data)
                : Results.BadRequest(new { error = result.ErrorMessage });
        }).RequireAuthorization(Permissions.MembersCreate);

        group.MapPut("/{id:guid}", async (
            Guid id, ClaimsPrincipal principal, IMediator mediator, UpdateMemberRequest dto) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            var result = await mediator.Send(new UpdateMemberCommand(tenantId.Value, id, dto));
            return result.IsSuccess ? Results.Ok(result.Data) : Results.BadRequest(new { error = result.ErrorMessage });
        }).RequireAuthorization(Permissions.MembersUpdate);

        group.MapPatch("/{id:guid}/status", async (
            Guid id, ClaimsPrincipal principal, IMediator mediator, PatchMemberStatusRequest dto) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            var result = await mediator.Send(new PatchMemberStatusCommand(tenantId.Value, id, dto.Status));
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.ErrorMessage });
        }).RequireAuthorization(Permissions.MembersUpdate);

        group.MapPost("/{id:guid}/send-activation", async (
            Guid id, ClaimsPrincipal principal, IMediator mediator) =>
        {
            var tenantId = GetTenantId(principal);
            if (tenantId is null) return Results.Unauthorized();
            var result = await mediator.Send(new SendMemberActivationCommand(tenantId.Value, id));
            return result.IsSuccess ? Results.Ok(new { message = "Activation envoyée." }) : Results.BadRequest(new { error = result.ErrorMessage });
        }).RequireAuthorization(Permissions.MembersSendActivation);
    }

    private static Guid? GetTenantId(ClaimsPrincipal principal)
    {
        var raw = principal.FindFirstValue("tenant_id");
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
