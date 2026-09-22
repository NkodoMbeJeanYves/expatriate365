using MediatR;
using server.Application.Members.Queries;

namespace server.API.DirectoryFeature;

public static class DirectoryEndpoints
{
    public static void MapDirectoryEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/directory/{slug}", async (
            string slug,
            IMediator mediator,
            string? search = null,
            string? profession = null) =>
        {
            var result = await mediator.Send(new GetDirectoryQuery(slug, search, profession));
            return Results.Ok(result);
        })
        .WithTags("Directory")
        .AllowAnonymous();
    }
}
