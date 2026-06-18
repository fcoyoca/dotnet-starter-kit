namespace FSH.Modules.Administration.Contracts.Dtos;

public sealed record LookupItemDto(int Id, string Name, bool IsActive, string? SnomedCode = null);
