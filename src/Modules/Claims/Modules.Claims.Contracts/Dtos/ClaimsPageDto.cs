using FSH.Framework.Shared.Persistence;

namespace FSH.Modules.Claims.Contracts.Dtos;

public sealed record ClaimsPageDto(
    PagedResponse<ClaimListItemDto> Page,
    ClaimsSummaryDto Summary);
