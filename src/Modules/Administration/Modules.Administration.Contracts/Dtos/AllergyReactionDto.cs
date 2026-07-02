namespace FSH.Modules.Administration.Contracts.Dtos;

public sealed record AllergyReactionDto(int Id, string Term, string? SnomedCode, bool IsActive);
