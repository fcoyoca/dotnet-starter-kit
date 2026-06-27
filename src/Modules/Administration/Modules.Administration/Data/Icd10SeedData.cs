using System.Reflection;

namespace FSH.Modules.Administration.Data;

/// <summary>
/// Reads the embedded ICD-10-CM seed (tab-separated: code, isBillable, shortDescription, longDescription),
/// migrated from the legacy <c>ICD10CMCodes</c> table. Drop a larger file at the same path to seed the full set.
/// </summary>
internal static class Icd10SeedData
{
    private const string ResourceName = "FSH.Modules.Administration.Data.Seed.icd10cm.tsv";

    public sealed record SeedRow(string Code, string? ShortDescription, string? LongDescription, bool IsBillable);

    public static IEnumerable<SeedRow> Read()
    {
        Assembly assembly = typeof(Icd10SeedData).Assembly;
        using Stream? stream = assembly.GetManifestResourceStream(ResourceName);
        if (stream is null)
        {
            yield break;
        }

        using var reader = new StreamReader(stream);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            string[] parts = line.Split('\t');
            if (parts.Length < 1 || string.IsNullOrWhiteSpace(parts[0]))
            {
                continue;
            }

            string code = parts[0].Trim();
            bool isBillable = parts.Length > 1 && parts[1].Trim() == "1";
            string? shortDesc = parts.Length > 2 ? Nullify(parts[2]) : null;
            string? longDesc = parts.Length > 3 ? Nullify(parts[3]) : null;
            yield return new SeedRow(code, shortDesc, longDesc, isBillable);
        }
    }

    private static string? Nullify(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
