using DogWalker.Core.Abstractions;
using DogWalker.Core.DTOs;
using DogWalker.Core.Enums;
using DogWalker.Core.Models;
using DogWalker.Core.Requests;
using DogWalkerApi.Data;
using DogWalkerApi.Security;
using DogWalkerApi.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace DogWalkerApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BookingsController : ControllerBase
{
    private readonly AccountAccess _access;
    private readonly IBookingService _bookingService;
    private readonly IPaymentService _paymentService;
    private readonly DogWalkerDbContext _db;
    private readonly BookingBroadcaster _broadcaster;

    public BookingsController(
        IBookingService bookingService,
        IPaymentService paymentService,
        DogWalkerDbContext db,
        BookingBroadcaster broadcaster, AccountAccess access)
    {
        _access = access;
        _bookingService = bookingService;
        _paymentService = paymentService;
        _db = db;
        _broadcaster = broadcaster;
    }

    [HttpPost]
    public async Task<ActionResult<BookingDto>> CreateBooking([FromBody] CreateBookingRequest request, CancellationToken cancellationToken)
    {
        var booking = await _bookingService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetBooking), new { id = booking.Id }, booking);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BookingDto>> GetBooking(Guid id, CancellationToken cancellationToken)
    {
        var booking = await _bookingService.GetByIdAsync(id, cancellationToken);
        return booking is null ? NotFound() : Ok(booking);
    }

    [HttpGet("walker/{walkerId:guid}")]
    public async Task<ActionResult<IEnumerable<BookingDto>>> GetWalkerBookings(Guid walkerId, CancellationToken cancellationToken)
    {
        var bookings = await _bookingService.GetUpcomingAsync(walkerId, cancellationToken);
        return Ok(bookings);
    }

    [HttpGet("client/{clientId:guid}")]
    public async Task<ActionResult<IEnumerable<BookingDto>>> GetClientBookings(Guid clientId, CancellationToken cancellationToken)
        => Ok(await _bookingService.GetForClientAsync(clientId, cancellationToken));

    [HttpPatch("{bookingId:guid}/status")]
    public async Task<ActionResult<BookingDto>> UpdateStatus(Guid bookingId, [FromBody] UpdateBookingStatusRequest request, CancellationToken cancellationToken)
    {
        var booking = await _bookingService.UpdateStatusAsync(bookingId, request.Status, cancellationToken);
        await _broadcaster.SendAsync(bookingId, "StatusChanged", request.Status.ToString(), cancellationToken);

        if (request.Status == BookingStatus.Completed)
        {
            await _paymentService.CaptureAsync(bookingId, cancellationToken);
        }

        return Ok(booking);
    }

    [HttpPost("{bookingId:guid}/route")]
    public async Task<ActionResult> AppendRoutePoint(Guid bookingId, [FromBody] RoutePointRequest request, CancellationToken cancellationToken)
    {
        await _access.RequireBookingAsync(bookingId, caregiverOnly: true, ct: cancellationToken);
        var booking = await _db.Bookings.Include(b => b.RoutePoints).FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);
        if (booking is null)
        {
            return NotFound();
        }

        var point = new WalkRoutePoint
        {
            BookingId = bookingId,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            TotalDistanceMeters = request.TotalDistanceMeters,
            SpeedMph = request.SpeedMph
        };

        booking.RoutePoints.Add(point);
        _db.RoutePoints.Add(point);
        await _db.SaveChangesAsync(cancellationToken);

        await _broadcaster.SendAsync(bookingId, "RoutePointUpdated", point, cancellationToken);
        return Ok();
    }

    [HttpPost("{bookingId:guid}/media")]
    public async Task<ActionResult<MediaAsset>> UploadMedia(Guid bookingId, [FromBody] MediaUploadRequest request, CancellationToken cancellationToken)
    {
        await _access.RequireBookingAsync(bookingId, caregiverOnly: true, ct: cancellationToken);
        var booking = await _db.Bookings.Include(b => b.Media).FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);
        if (booking is null)
        {
            return NotFound();
        }

        var media = new MediaAsset
        {
            BookingId = bookingId,
            Url = request.Url,
            ThumbnailUrl = request.ThumbnailUrl ?? string.Empty,
            MediaType = request.MediaType,
            Caption = request.Caption ?? string.Empty
        };

        booking.Media.Add(media);
        _db.MediaAssets.Add(media);
        await _db.SaveChangesAsync(cancellationToken);

        await _broadcaster.SendAsync(bookingId, "MediaShared", media, cancellationToken);
        return Ok(media);
    }

    [HttpPost("{bookingId:guid}/payments")]
    public async Task<ActionResult<PaymentDto>> CreatePaymentIntent(Guid bookingId, [FromBody] PaymentRequest request, CancellationToken cancellationToken)
    {
        await _access.RequireBookingAsync(bookingId, ownerOnly: true, ct: cancellationToken);
        var payment = await _paymentService.CreateOrUpdateIntentAsync(bookingId, request.Amount, request.TipAmount, cancellationToken);
        return Ok(payment);
    }
}

public record PaymentRequest(double Amount, double TipAmount);
