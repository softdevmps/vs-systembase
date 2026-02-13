using Backend.Data;
using Microsoft.Data.SqlClient;

namespace Backend.Negocio.Pipeline
{
    public sealed class AudioPurgeItem
    {
        public int Id { get; init; }
        public string? FilePath { get; init; }
    }

    public sealed class AudioRetentionRepository
    {
        public int SoftDeleteOlderThan(int days)
        {
            using var conn = Db.Open();
            const string sql = @"
UPDATE [sys_mapeo].[IncidenteAudio]
SET [IsDeleted] = 1,
    [DeletedAt] = GETUTCDATE()
WHERE ISNULL([IsDeleted], 0) = 0
  AND [CreatedAt] < DATEADD(DAY, -@Days, GETUTCDATE());";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Days", days);
            return cmd.ExecuteNonQuery();
        }

        public List<AudioPurgeItem> GetPurgeCandidates(int days, int take)
        {
            using var conn = Db.Open();
            const string sql = @"
SELECT TOP (@Take) [Id], [FilePath]
FROM [sys_mapeo].[IncidenteAudio]
WHERE ISNULL([IsDeleted], 0) = 1
  AND [DeletedAt] < DATEADD(DAY, -@Days, GETUTCDATE())
ORDER BY [Id] ASC;";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Days", days);
            cmd.Parameters.AddWithValue("@Take", take);
            using var reader = cmd.ExecuteReader();
            var list = new List<AudioPurgeItem>();
            while (reader.Read())
            {
                list.Add(new AudioPurgeItem
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    FilePath = reader["FilePath"]?.ToString()
                });
            }
            return list;
        }

        public int HardDelete(int id)
        {
            using var conn = Db.Open();
            const string sql = "DELETE FROM [sys_mapeo].[IncidenteAudio] WHERE [Id] = @Id;";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", id);
            return cmd.ExecuteNonQuery();
        }
    }
}
