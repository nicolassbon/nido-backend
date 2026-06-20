using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Nido.Api.Contracts.Notificaciones;
using Nido.Application.Common.Notifications;
using Nido.Application.Common.Security;
using Nido.Application.Notificaciones;

namespace Nido.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/notificaciones")]
public sealed class NotificacionesController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetNotifications(
        [FromServices] GetNotificationsHandler handler,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        var list = await handler.Handle(new GetNotificationsQuery(currentUser.UsuarioId), ct);
        return Ok(list.Select(ToResponse));
    }

    [HttpPost("{id:guid}/leer")]
    public async Task<IActionResult> MarkAsRead(
        Guid id,
        [FromServices] MarkNotificationAsReadHandler handler,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        var updated = await handler.Handle(new MarkNotificationAsReadCommand(id, currentUser.UsuarioId), ct);
        if (!updated) return NotFound();

        return NoContent();
    }

    [HttpPost("leer-todas")]
    public async Task<IActionResult> MarkAllAsRead(
        [FromServices] MarkAllNotificationsAsReadHandler handler,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        await handler.Handle(new MarkAllNotificationsAsReadCommand(currentUser.UsuarioId), ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromServices] DeleteNotificationHandler handler,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken ct)
    {
        var deleted = await handler.Handle(new DeleteNotificationCommand(id, currentUser.UsuarioId), ct);
        if (!deleted) return NotFound();

        return NoContent();
    }

    [HttpPost("suscripciones")]
    public async Task<IActionResult> SubscribePush(
        [FromBody] SubscribePushRequest request,
        [FromServices] SubscribePushHandler handler,
        [FromServices] ICurrentUserContext currentUser,
        [FromServices] IServiceScopeFactory scopeFactory,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Endpoint) || string.IsNullOrWhiteSpace(request.P256dh) || string.IsNullOrWhiteSpace(request.Auth))
        {
            return BadRequest("Endpoint, p256dh y auth son requeridos.");
        }

        var usuarioId = currentUser.UsuarioId;

        await handler.Handle(new SubscribePushCommand(
            usuarioId,
            request.Endpoint,
            request.P256dh,
            request.Auth
        ), ct);

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(500);
                using (var scope = scopeFactory.CreateScope())
                {
                    var pushService = scope.ServiceProvider.GetRequiredService<IPushNotificationService>();
                    await pushService.SendNotificationAsync(
                        usuarioId,
                        "Notificaciones Activas",
                        "¡Felicidades! Las notificaciones en segundo plano de Nido están configuradas correctamente.",
                        "/tareas",
                        CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al enviar notificación de prueba: {ex.Message}");
            }
        });

        return NoContent();
    }

    private static NotificacionResponse ToResponse(NotificacionResult n)
    {
        return new NotificacionResponse(
            n.Id,
            n.UsuarioId,
            n.Tipo,
            n.Mensaje,
            n.Leida,
            n.ReferenciaId,
            n.ReferenciaTipo,
            n.CreatedAt);
    }
}
