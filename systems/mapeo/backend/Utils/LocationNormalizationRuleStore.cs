using System.Text.RegularExpressions;
using Backend.Data;
using Microsoft.Data.SqlClient;

namespace Backend.Utils
{
    public sealed class LocationNormalizationRule
    {
        public int Id { get; init; }
        public string FindText { get; init; } = "";
        public string ReplaceText { get; init; } = "";
        public string Scope { get; init; } = "location";
        public int Priority { get; init; } = 100;
        public bool IsRegex { get; init; }
    }

    public static class LocationNormalizationRuleStore
    {
        private static readonly object Sync = new();
        private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(45);
        private static DateTime _lastLoadUtc = DateTime.MinValue;
        private static IReadOnlyList<LocationNormalizationRule> _cache = Array.Empty<LocationNormalizationRule>();

        public static string ApplyRules(string input, string scope)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            var rules = GetRules(scope);
            if (rules.Count == 0)
                return input;

            var result = input;
            foreach (var rule in rules)
            {
                if (string.IsNullOrWhiteSpace(rule.FindText))
                    continue;

                if (rule.IsRegex)
                {
                    try
                    {
                        result = Regex.Replace(
                            result,
                            rule.FindText,
                            rule.ReplaceText ?? "",
                            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
                        );
                    }
                    catch
                    {
                        // Regla regex inválida: se ignora para no romper el pipeline.
                    }
                }
                else
                {
                    var key = rule.FindText.Trim();
                    var value = (rule.ReplaceText ?? "").Trim();
                    if (key.Contains(' '))
                    {
                        result = result.Replace(key, value, StringComparison.OrdinalIgnoreCase);
                    }
                    else
                    {
                        result = Regex.Replace(
                            result,
                            $@"\b{Regex.Escape(key)}\b",
                            value,
                            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
                        );
                    }
                }
            }

            return result;
        }

        private static IReadOnlyList<LocationNormalizationRule> GetRules(string scope)
        {
            EnsureCacheLoaded();
            return _cache
                .Where(r => string.Equals(r.Scope, scope, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(r.Scope, "all", StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        private static void EnsureCacheLoaded()
        {
            var now = DateTime.UtcNow;
            if ((now - _lastLoadUtc) < CacheTtl && _cache.Count > 0)
                return;

            lock (Sync)
            {
                now = DateTime.UtcNow;
                if ((now - _lastLoadUtc) < CacheTtl && _cache.Count > 0)
                    return;

                try
                {
                    using var conn = Db.Open();
                    using var cmd = new SqlCommand(@"
SELECT [Id], [FindText], [ReplaceText], [Scope], [Priority], [IsRegex]
FROM [sys_mapeo].[LocationNormalizationRules]
WHERE [IsActive] = 1
ORDER BY [Priority] ASC, [Id] ASC;", conn);
                    using var reader = cmd.ExecuteReader();

                    var list = new List<LocationNormalizationRule>();
                    while (reader.Read())
                    {
                        list.Add(new LocationNormalizationRule
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            FindText = reader["FindText"]?.ToString() ?? "",
                            ReplaceText = reader["ReplaceText"]?.ToString() ?? "",
                            Scope = reader["Scope"]?.ToString() ?? "location",
                            Priority = reader["Priority"] == DBNull.Value ? 100 : Convert.ToInt32(reader["Priority"]),
                            IsRegex = reader["IsRegex"] != DBNull.Value && Convert.ToBoolean(reader["IsRegex"])
                        });
                    }

                    _cache = list;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[LocationRules] Error cargando reglas: {ex.Message}");
                }
                finally
                {
                    _lastLoadUtc = DateTime.UtcNow;
                }
            }
        }
    }
}
