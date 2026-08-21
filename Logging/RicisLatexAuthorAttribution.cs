using Ricis.Core.Metadata;

namespace Ricis.Core.Logging;

/// <summary>Origin and privacy status of a LaTeX author attribution projection.</summary>
public enum RicisLatexAuthorAttributionMode
{
    /// <summary>No author information is included.</summary>
    None,
    /// <summary>Public project author metadata was selected by the trusted local identity selector.</summary>
    TrustedRicisAuthor,
    /// <summary>A paid-user request was received but no ephemeral client callback was supplied.</summary>
    CallbackRequired,
    /// <summary>Public author metadata was supplied transiently by the caller's callback.</summary>
    CallbackProvidedPaidUser,
}

/// <summary>Public work metadata safe for a document attribution block.</summary>
public sealed record RicisLatexAuthorWorkViewModel(string Name, string Url, string DatePublished);

/// <summary>
/// Callback payload for a paid user's public authorship data. It intentionally excludes email, payment data,
/// customer identifiers and any server-persistence contract.
/// </summary>
public sealed record RicisLatexPaidUserAuthorInput(
    string DisplayName,
    string AlternateName,
    string Orcid,
    string Description,
    IReadOnlyList<string> Keywords,
    IReadOnlyList<RicisLatexAuthorWorkViewModel> Works);

/// <summary>
/// Immutable document-only author attribution. The requester email and paid status are never fields of this model.
/// </summary>
public sealed record RicisLatexAuthorAttributionViewModel
{
    internal RicisLatexAuthorAttributionViewModel(
        RicisLatexAuthorAttributionMode mode,
        string displayName,
        string alternateName,
        string orcid,
        string description,
        IReadOnlyList<string> keywords,
        IReadOnlyList<RicisLatexAuthorWorkViewModel> works)
    {
        Mode = mode;
        DisplayName = displayName ?? string.Empty;
        AlternateName = alternateName ?? string.Empty;
        Orcid = orcid ?? string.Empty;
        Description = description ?? string.Empty;
        Keywords = Array.AsReadOnly((keywords ?? Array.Empty<string>()).ToArray());
        Works = Array.AsReadOnly((works ?? Array.Empty<RicisLatexAuthorWorkViewModel>()).ToArray());
    }

    /// <summary>Attribution source and privacy state.</summary>
    public RicisLatexAuthorAttributionMode Mode { get; }

    /// <summary>Whether the external template may render a public author block.</summary>
    public bool IsIncluded => Mode is RicisLatexAuthorAttributionMode.TrustedRicisAuthor or RicisLatexAuthorAttributionMode.CallbackProvidedPaidUser;

    /// <summary>Public display name.</summary>
    public string DisplayName { get; }

    /// <summary>Optional public alternate name.</summary>
    public string AlternateName { get; }

    /// <summary>Optional public ORCID URL.</summary>
    public string Orcid { get; }

    /// <summary>Public author description.</summary>
    public string Description { get; }

    /// <summary>Public SEO keyword list.</summary>
    public IReadOnlyList<string> Keywords { get; }

    /// <summary>Public publication links.</summary>
    public IReadOnlyList<RicisLatexAuthorWorkViewModel> Works { get; }
}

/// <summary>
/// Resolves document-only author attribution. Paid-user information is requested only from a caller-provided callback,
/// held only in the returned in-memory ViewModel and never persisted by this library.
/// </summary>
public sealed class RicisLatexAuthorAttributionResolver
{
    private const string TrustedRicisAuthorEmail = "dima.aley@gmail.com";

    /// <summary>
    /// Resolves a public author block for one document request. The caller owns requester identity and paid-status
    /// validation; neither is stored, rendered, logged or returned. A paid-user callback is invoked at most once.
    /// </summary>
    public RicisLatexAuthorAttributionViewModel Resolve(
        string requesterEmail,
        bool isPaidUser,
        Func<RicisLatexPaidUserAuthorInput> paidUserAuthorCallback = null)
    {
        if (string.Equals(requesterEmail, TrustedRicisAuthorEmail, StringComparison.OrdinalIgnoreCase))
        {
            return FromRicisAuthor(AuthorSeoProfile.RicisAuthor);
        }

        if (!isPaidUser)
        {
            return new RicisLatexAuthorAttributionViewModel(
                RicisLatexAuthorAttributionMode.None,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                Array.Empty<string>(),
                Array.Empty<RicisLatexAuthorWorkViewModel>());
        }

        var paidUserInput = paidUserAuthorCallback?.Invoke();
        if (paidUserInput is null)
        {
            return new RicisLatexAuthorAttributionViewModel(
                RicisLatexAuthorAttributionMode.CallbackRequired,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                Array.Empty<string>(),
                Array.Empty<RicisLatexAuthorWorkViewModel>());
        }

        ValidatePaidUserInput(paidUserInput);
        return new RicisLatexAuthorAttributionViewModel(
            RicisLatexAuthorAttributionMode.CallbackProvidedPaidUser,
            paidUserInput.DisplayName,
            paidUserInput.AlternateName,
            paidUserInput.Orcid,
            paidUserInput.Description,
            paidUserInput.Keywords,
            paidUserInput.Works);
    }

    private static RicisLatexAuthorAttributionViewModel FromRicisAuthor(AuthorSeoProfile author) =>
        new(
            RicisLatexAuthorAttributionMode.TrustedRicisAuthor,
            author.Name,
            author.AlternateName,
            author.Orcid,
            author.Description,
            author.Keywords,
            author.Works.Select(work => new RicisLatexAuthorWorkViewModel(
                work.Name,
                work.Url,
                work.DatePublished.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture))).ToArray());

    private static void ValidatePaidUserInput(RicisLatexPaidUserAuthorInput input)
    {
        if (string.IsNullOrWhiteSpace(input.DisplayName))
        {
            throw new ArgumentException("Paid-user author callback must supply a public display name.", nameof(input));
        }

        if (!string.IsNullOrWhiteSpace(input.Orcid) && !Uri.TryCreate(input.Orcid, UriKind.Absolute, out _))
        {
            throw new ArgumentException("Paid-user ORCID must be an absolute public URI when supplied.", nameof(input));
        }

        foreach (var work in input.Works ?? Array.Empty<RicisLatexAuthorWorkViewModel>())
        {
            if (work is null || string.IsNullOrWhiteSpace(work.Name) || !Uri.TryCreate(work.Url, UriKind.Absolute, out _))
            {
                throw new ArgumentException("Every paid-user work must have a name and absolute public URL.", nameof(input));
            }
        }
    }
}
