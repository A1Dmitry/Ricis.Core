using Ricis.Finance.Domain;

namespace Ricis.Finance.Application;

/// <summary>Coordinates an authorised, idempotent payout request without embedding provider SDK code.</summary>
public sealed class RequestPayoutService
{
    private readonly ISettlementRepository _settlements;
    private readonly IPayoutRepository _payouts;
    private readonly IPayoutReleasePolicy _releasePolicy;
    private readonly IPaymentProviderPort _provider;
    private readonly IClock _clock;

    /// <summary>Creates the payout workflow from explicit application ports.</summary>
    public RequestPayoutService(
        ISettlementRepository settlements,
        IPayoutRepository payouts,
        IPayoutReleasePolicy releasePolicy,
        IPaymentProviderPort provider,
        IClock clock)
    {
        _settlements = settlements ?? throw new ArgumentNullException(nameof(settlements));
        _payouts = payouts ?? throw new ArgumentNullException(nameof(payouts));
        _releasePolicy = releasePolicy ?? throw new ArgumentNullException(nameof(releasePolicy));
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    /// <summary>
    /// Requests a provider payout once. This does not create, delay, delete or
    /// alter a tax receipt candidate; payout and tax recognition are separate.
    /// </summary>
    public async ValueTask<PayoutRequest> HandleAsync(RequestPayout command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var existing = await _payouts.FindByIdempotencyKeyAsync(command.IdempotencyKey, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            return existing;
        }

        var settlement = await _settlements.FindByIdAsync(command.SettlementId, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Settlement {command.SettlementId} не найден.");
        var decision = _releasePolicy.Decide(settlement, command.Amount);
        if (!decision.IsAllowed)
        {
            throw new InvalidOperationException($"Payout отклонён policy: {decision.Reason}");
        }

        var payout = settlement.ReservePayout(Guid.NewGuid(), command.IdempotencyKey, command.Amount, _clock.UtcNow);
        var submission = await _provider.SubmitPayoutAsync(payout, cancellationToken).ConfigureAwait(false);
        payout.MarkSubmitted(submission.ProviderPayoutId);
        await _settlements.StoreAsync(settlement, cancellationToken).ConfigureAwait(false);
        await _payouts.StoreAsync(payout, cancellationToken).ConfigureAwait(false);
        return payout;
    }
}

/// <summary>Input to the idempotent provider payout request workflow.</summary>
public sealed record RequestPayout(Guid SettlementId, string IdempotencyKey, Money Amount);
