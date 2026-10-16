using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Application.UseCases.License;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;
using BitWrite.OcelotConsole.Domain.ValueObjects.Status;
using DomainLicense = BitWrite.OcelotConsole.Domain.Aggregates.License.License;

namespace BitWrite.OcelotConsole.Application.UseCases.License;

public class ListLicensesQueryHandler
{
    private readonly ILicenseRepository _licenseRepository;

    public ListLicensesQueryHandler(ILicenseRepository licenseRepository)
    {
        _licenseRepository = licenseRepository;
    }

    public async Task<LicenseListResponse> HandleAsync(ListLicensesQuery query, CancellationToken cancellationToken = default)
    {
        if (query.Status != null)
        {
            if (query.Status.IsActive)
            {
                var activeLicenses = await _licenseRepository.GetActiveAsync(cancellationToken);
                var filtered = activeLicenses
                    .OrderByDescending(l => l.CreatedAt)
                    .Skip((query.Page - 1) * query.PageSize)
                    .Take(query.PageSize)
                    .ToList();

                var totalCount = (await _licenseRepository.GetActiveAsync(cancellationToken)).Count;

                return new LicenseListResponse(
                    filtered.Select(MapToResponse).ToList(),
                    totalCount,
                    query.Page,
                    query.PageSize
                );
            }
            else if (query.Status.IsExpired)
            {
                var expiredLicenses = await _licenseRepository.GetExpiredAsync(cancellationToken);
                var filtered = expiredLicenses
                    .OrderByDescending(l => l.CreatedAt)
                    .Skip((query.Page - 1) * query.PageSize)
                    .Take(query.PageSize)
                    .ToList();

                var totalCountExpired = (await _licenseRepository.GetExpiredAsync(cancellationToken)).Count;

                return new LicenseListResponse(
                    filtered.Select(MapToResponse).ToList(),
                    totalCountExpired,
                    query.Page,
                    query.PageSize
                );
            }
        }

        var allLicenses = await _licenseRepository.GetAllAsync(cancellationToken);
        var allTotalCount = allLicenses.Count;

        var pagedLicenses = allLicenses
            .OrderByDescending(l => l.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        return new LicenseListResponse(
            pagedLicenses.Select(MapToResponse).ToList(),
            allTotalCount,
            query.Page,
            query.PageSize
        );
    }

    private static LicenseResponse MapToResponse(DomainLicense license)
    {
        return new LicenseResponse(
            license.Id,
            license.Name,
            license.ProductCode,
            license.Status,
            license.ExpirationDate,
            license.CreatedAt,
            license.UpdatedAt,
            license.ActivatedAt,
            license.RevokedAt,
            license.RevocationReason,
            license.Features.Select(f => f.Key).ToList()
        );
    }
}
