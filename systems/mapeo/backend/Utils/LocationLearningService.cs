using System.Text.RegularExpressions;
using Backend.Data;
using Microsoft.Data.SqlClient;

namespace Backend.Utils
{
    internal sealed class PendingLocationFeedback
    {
        public int Id { get; init; }
        public string? PredLugarTexto { get; init; }
    }

    public static class LocationLearningService
    {
        private static readonly HashSet<string> StopTokens =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "avenida", "av", "boulevard", "bulevar", "blvd", "calle", "pasaje", "ruta", "diagonal",
                "barrio", "cordoba", "argentina", "capital", "municipio", "de", "del", "la", "el", "los", "las",
                "y", "e", "en", "al", "a", "con", "esquina",
                "plaza", "parque", "hospital", "frente", "centro", "zona", "ciudad",
                "interseccion", "aproximada"
            };

        public static void RecordPredictionFailure(
            int incidenteId,
            string? rawText,
            string? whisperLocation,
            string? predLugarTexto,
            string? predLugarNormalizado,
            decimal? predLat,
            decimal? predLng,
            string? notes = null)
        {
            if (incidenteId <= 0)
                return;

            try
            {
                var safeNotes = string.IsNullOrWhiteSpace(notes)
                    ? "geocode_failed"
                    : notes!.Trim();
                if (safeNotes.Length > 500)
                    safeNotes = safeNotes.Substring(0, 500);

                using var conn = Db.Open();
                using var cmd = new SqlCommand(@"
IF NOT EXISTS
(
    SELECT 1
    FROM [sys_mapeo].[LocationNormalizationFeedback]
    WHERE [IncidenteId] = @IncidenteId
      AND [Verdict] = 'pending'
      AND ISNULL([PredLugarTexto], '') = ISNULL(@PredLugarTexto, '')
)
BEGIN
    INSERT INTO [sys_mapeo].[LocationNormalizationFeedback]
    (
        [IncidenteId], [RawText], [WhisperLocation],
        [PredLugarTexto], [PredLugarNormalizado], [PredLat], [PredLng],
        [Verdict], [Notes], [CreatedAt]
    )
    VALUES
    (
        @IncidenteId, @RawText, @WhisperLocation,
        @PredLugarTexto, @PredLugarNormalizado, @PredLat, @PredLng,
        'pending', @Notes, SYSUTCDATETIME()
    );
END;", conn);

                cmd.Parameters.AddWithValue("@IncidenteId", incidenteId);
                cmd.Parameters.AddWithValue("@RawText", (object?)rawText ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@WhisperLocation", (object?)whisperLocation ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@PredLugarTexto", (object?)predLugarTexto ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@PredLugarNormalizado", (object?)predLugarNormalizado ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@PredLat", predLat ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@PredLng", predLng ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Notes", safeNotes);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LocationLearning] Error registrando feedback fallido: {ex.Message}");
            }
        }

        public static void ResolvePendingFeedback(
            int incidenteId,
            string? correctLugarTexto,
            string? correctLugarNormalizado,
            decimal? correctLat,
            decimal? correctLng,
            string reviewer = "system-auto")
        {
            if (incidenteId <= 0)
                return;

            try
            {
                using var conn = Db.Open();
                using var tx = conn.BeginTransaction();

                var pending = GetLastPending(conn, tx, incidenteId);
                if (pending == null)
                {
                    tx.Commit();
                    return;
                }

                using (var update = new SqlCommand(@"
UPDATE [sys_mapeo].[LocationNormalizationFeedback]
SET [CorrectLugarTexto] = @CorrectLugarTexto,
    [CorrectLugarNormalizado] = @CorrectLugarNormalizado,
    [CorrectLat] = @CorrectLat,
    [CorrectLng] = @CorrectLng,
    [Verdict] = 'corrected',
    [Reviewer] = @Reviewer,
    [UpdatedAt] = SYSUTCDATETIME()
WHERE [Id] = @Id;", conn, tx))
                {
                    update.Parameters.AddWithValue("@Id", pending.Id);
                    update.Parameters.AddWithValue("@CorrectLugarTexto", (object?)correctLugarTexto ?? DBNull.Value);
                    update.Parameters.AddWithValue("@CorrectLugarNormalizado", (object?)correctLugarNormalizado ?? DBNull.Value);
                    update.Parameters.AddWithValue("@CorrectLat", correctLat ?? (object)DBNull.Value);
                    update.Parameters.AddWithValue("@CorrectLng", correctLng ?? (object)DBNull.Value);
                    update.Parameters.AddWithValue("@Reviewer", reviewer);
                    update.ExecuteNonQuery();
                }

                var targetText = !string.IsNullOrWhiteSpace(correctLugarTexto)
                    ? correctLugarTexto
                    : correctLugarNormalizado;

                // Regla directa frase->frase para que una correccion manual aplique
                // exactamente en el siguiente retry del mismo patron.
                var predictedPhrase = NormalizeForRule(pending.PredLugarTexto);
                var correctedPhrase = NormalizeForRule(targetText);
                if (!string.IsNullOrWhiteSpace(predictedPhrase) &&
                    !string.IsNullOrWhiteSpace(correctedPhrase) &&
                    !string.Equals(predictedPhrase, correctedPhrase, StringComparison.OrdinalIgnoreCase))
                {
                    UpsertAutoRule(conn, tx, predictedPhrase, correctedPhrase, priority: 40);
                }

                foreach (var candidate in BuildRuleCandidates(pending.PredLugarTexto, targetText))
                {
                    UpsertAutoRule(conn, tx, candidate.FindText, candidate.ReplaceText);
                }

                tx.Commit();
                LocationNormalizationRuleStore.InvalidateCache();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LocationLearning] Error resolviendo feedback: {ex.Message}");
            }
        }

        public static void LearnFromSuccessfulGeocode(string? predictedLugarTexto, string? normalizedDisplayName)
        {
            var predicted = AppConfig.NormalizeKey(predictedLugarTexto ?? "");
            var corrected = AppConfig.NormalizeKey(normalizedDisplayName ?? "");
            if (string.IsNullOrWhiteSpace(predicted) || string.IsNullOrWhiteSpace(corrected))
                return;

            if (!LooksComparable(predicted, corrected))
            {
                Console.WriteLine("[LocationLearning] Skip auto-learn: textos no comparables.");
                return;
            }

            try
            {
                using var conn = Db.Open();
                using var tx = conn.BeginTransaction();
                var added = 0;

                var predictedPhrase = NormalizeForRule(predictedLugarTexto);
                var correctedPhrase = NormalizeForRule(normalizedDisplayName);
                if (!string.IsNullOrWhiteSpace(predictedPhrase) &&
                    !string.IsNullOrWhiteSpace(correctedPhrase) &&
                    !string.Equals(predictedPhrase, correctedPhrase, StringComparison.OrdinalIgnoreCase))
                {
                    UpsertAutoRule(conn, tx, predictedPhrase, correctedPhrase, priority: 45);
                    added++;
                }

                foreach (var candidate in BuildRuleCandidates(predicted, corrected))
                {
                    UpsertAutoRule(conn, tx, candidate.FindText, candidate.ReplaceText);
                    added++;
                }
                tx.Commit();
                LocationNormalizationRuleStore.InvalidateCache();
                if (added > 0)
                    Console.WriteLine($"[LocationLearning] Auto-learn generado: {added} regla(s).");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LocationLearning] Error aprendiendo desde geocode exitoso: {ex.Message}");
            }
        }

        private static PendingLocationFeedback? GetLastPending(SqlConnection conn, SqlTransaction tx, int incidenteId)
        {
            using var cmd = new SqlCommand(@"
SELECT TOP 1 [Id], [PredLugarTexto]
FROM [sys_mapeo].[LocationNormalizationFeedback]
WHERE [IncidenteId] = @IncidenteId
  AND [Verdict] = 'pending'
ORDER BY [Id] DESC;", conn, tx);
            cmd.Parameters.AddWithValue("@IncidenteId", incidenteId);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            return new PendingLocationFeedback
            {
                Id = Convert.ToInt32(reader["Id"]),
                PredLugarTexto = reader["PredLugarTexto"] == DBNull.Value ? null : reader["PredLugarTexto"].ToString()
            };
        }

        private static void UpsertAutoRule(SqlConnection conn, SqlTransaction tx, string findText, string replaceText, int priority = 60)
        {
            findText = TruncateRuleText(findText, 200);
            replaceText = TruncateRuleText(replaceText, 200);
            if (string.IsNullOrWhiteSpace(findText) || string.IsNullOrWhiteSpace(replaceText))
                return;

            using var select = new SqlCommand(@"
SELECT TOP 1 [Id], [ReplaceText], [Source]
FROM [sys_mapeo].[LocationNormalizationRules]
WHERE [FindText] = @FindText
  AND [Scope] = 'location'
  AND [IsActive] = 1
ORDER BY CASE WHEN [Source] = 'manual' THEN 0 ELSE 1 END, [Id] DESC;", conn, tx);
            select.Parameters.AddWithValue("@FindText", findText);

            int? existingId = null;
            string? existingReplace = null;
            string? source = null;

            using (var reader = select.ExecuteReader())
            {
                if (reader.Read())
                {
                    existingId = Convert.ToInt32(reader["Id"]);
                    existingReplace = reader["ReplaceText"] == DBNull.Value ? null : reader["ReplaceText"].ToString();
                    source = reader["Source"] == DBNull.Value ? null : reader["Source"].ToString();
                }
            }

            if (existingId == null)
            {
                using var insert = new SqlCommand(@"
INSERT INTO [sys_mapeo].[LocationNormalizationRules]
([FindText], [ReplaceText], [Scope], [Priority], [IsRegex], [IsActive], [Source], [CreatedAt])
VALUES
(@FindText, @ReplaceText, 'location', @Priority, 0, 1, 'auto-feedback', SYSUTCDATETIME());", conn, tx);
                insert.Parameters.AddWithValue("@FindText", findText);
                insert.Parameters.AddWithValue("@ReplaceText", replaceText);
                insert.Parameters.AddWithValue("@Priority", priority);
                insert.ExecuteNonQuery();
                return;
            }

            if (string.Equals(existingReplace, replaceText, StringComparison.OrdinalIgnoreCase))
                return;

            if (string.Equals(source, "manual", StringComparison.OrdinalIgnoreCase))
                return;

            using var update = new SqlCommand(@"
UPDATE [sys_mapeo].[LocationNormalizationRules]
SET [ReplaceText] = @ReplaceText,
    [Priority] = @Priority,
    [UpdatedAt] = SYSUTCDATETIME(),
    [Source] = 'auto-feedback'
WHERE [Id] = @Id;", conn, tx);
            update.Parameters.AddWithValue("@Id", existingId.Value);
            update.Parameters.AddWithValue("@ReplaceText", replaceText);
            update.Parameters.AddWithValue("@Priority", priority);
            update.ExecuteNonQuery();
        }

        private static IEnumerable<(string FindText, string ReplaceText)> BuildRuleCandidates(string? predicted, string? corrected)
        {
            var predTokens = Tokenize(predicted);
            var corrTokens = Tokenize(corrected);
            if (predTokens.Count == 0 || corrTokens.Count == 0)
                yield break;

            var corrSet = new HashSet<string>(corrTokens, StringComparer.OrdinalIgnoreCase);
            var emitted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var source in predTokens)
            {
                if (corrSet.Contains(source))
                    continue;

                var best = corrTokens
                    .Where(target => !string.Equals(target, source, StringComparison.OrdinalIgnoreCase))
                    .Select(target => new
                    {
                        Target = target,
                        Distance = Levenshtein(source, target),
                        Ratio = SimilarityRatio(source, target)
                    })
                    .OrderBy(x => x.Ratio)
                    .ThenBy(x => x.Distance)
                    .FirstOrDefault();

                if (best == null)
                    continue;

                if (best.Distance > 2 || best.Ratio > 0.34m)
                    continue;

                var key = $"{source}=>{best.Target}";
                if (!emitted.Add(key))
                    continue;

                yield return (source, best.Target);
            }

            var predPhrases = BuildPhrases(predTokens, 2);
            var corrPhrases = BuildPhrases(corrTokens, 2);
            var corrPhraseSet = new HashSet<string>(corrPhrases, StringComparer.OrdinalIgnoreCase);

            foreach (var sourcePhrase in predPhrases)
            {
                if (corrPhraseSet.Contains(sourcePhrase))
                    continue;

                var best = corrPhrases
                    .Where(target => !string.Equals(target, sourcePhrase, StringComparison.OrdinalIgnoreCase))
                    .Select(target => new
                    {
                        Target = target,
                        Distance = Levenshtein(sourcePhrase, target),
                        Ratio = SimilarityRatio(sourcePhrase, target)
                    })
                    .OrderBy(x => x.Ratio)
                    .ThenBy(x => x.Distance)
                    .FirstOrDefault();

                if (best == null)
                    continue;

                if (best.Distance > 3 || best.Ratio > 0.26m)
                    continue;

                if (sourcePhrase.Length > 80 || best.Target.Length > 80)
                    continue;

                var key = $"{sourcePhrase}=>{best.Target}";
                if (!emitted.Add(key))
                    continue;

                yield return (sourcePhrase, best.Target);
            }
        }

        private static string NormalizeForRule(string? text)
        {
            return AppConfig.NormalizeKey(text ?? "");
        }

        private static string TruncateRuleText(string value, int maxLen)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "";
            var normalized = value.Trim();
            return normalized.Length <= maxLen ? normalized : normalized[..maxLen];
        }

        private static bool LooksComparable(string predicted, string corrected)
        {
            var predTokens = Tokenize(predicted);
            var corrTokens = Tokenize(corrected);
            if (predTokens.Count == 0 || corrTokens.Count == 0)
                return false;

            var corrSet = new HashSet<string>(corrTokens, StringComparer.OrdinalIgnoreCase);
            var common = predTokens.Count(token => corrSet.Contains(token));
            if (common == 0)
                return false;

            if (common < 2 && predTokens.Count >= 5 && corrTokens.Count >= 5)
                return false;

            var predNum = ParseFirstNumber(predicted);
            var corrNum = ParseFirstNumber(corrected);
            if (predNum.HasValue && corrNum.HasValue && Math.Abs(predNum.Value - corrNum.Value) > 500)
                return false;

            return true;
        }

        private static List<string> Tokenize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return new List<string>();

            return AppConfig.NormalizeKey(value)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(token => token.Length >= 4)
                .Where(token => !Regex.IsMatch(token, @"\d", RegexOptions.CultureInvariant))
                .Where(token => !StopTokens.Contains(token))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static List<string> BuildPhrases(IReadOnlyList<string> tokens, int size)
        {
            var list = new List<string>();
            if (tokens == null || tokens.Count < size || size < 2)
                return list;

            for (var i = 0; i <= tokens.Count - size; i++)
            {
                var phraseTokens = tokens.Skip(i).Take(size).ToArray();
                if (phraseTokens.Length != size)
                    continue;
                list.Add(string.Join(" ", phraseTokens));
            }

            return list.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static int Levenshtein(string a, string b)
        {
            if (string.IsNullOrEmpty(a)) return b.Length;
            if (string.IsNullOrEmpty(b)) return a.Length;

            var d = new int[a.Length + 1, b.Length + 1];
            for (var i = 0; i <= a.Length; i++) d[i, 0] = i;
            for (var j = 0; j <= b.Length; j++) d[0, j] = j;

            for (var i = 1; i <= a.Length; i++)
            {
                for (var j = 1; j <= b.Length; j++)
                {
                    var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost
                    );
                }
            }

            return d[a.Length, b.Length];
        }

        private static decimal SimilarityRatio(string a, string b)
        {
            var max = Math.Max(a.Length, b.Length);
            if (max == 0) return 0m;
            return (decimal)Levenshtein(a, b) / max;
        }

        private static int? ParseFirstNumber(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var match = Regex.Match(value, @"\d{1,5}");
            if (!match.Success)
                return null;

            return int.TryParse(match.Value, out var parsed) ? parsed : null;
        }
    }
}
