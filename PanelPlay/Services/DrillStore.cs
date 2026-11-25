using System.Text.Json;
using Android.Database.Sqlite;
using PanelPlay.Models;

namespace PanelPlay.Services;

internal sealed class DrillStore : IDisposable
{
    private readonly SQLiteDatabase database = SQLiteDatabase.OpenOrCreateDatabase(
        Path.Combine(FileSystem.AppDataDirectory, "drills.db3"), null)!;

    public DrillStore() => database.ExecSQL(
        "CREATE TABLE IF NOT EXISTS drills (id TEXT PRIMARY KEY, title TEXT NOT NULL, payload TEXT NOT NULL)");

    public IReadOnlyList<Drill> List()
    {
        // A Base64 photo can exceed the Android CursorWindow row limit.
        using var cursor = database.RawQuery("""
            WITH RECURSIVE parts(id, title, offset, total) AS (
                SELECT id, title, 1, length(payload) FROM drills
                UNION ALL
                SELECT id, title, offset + 131072, total FROM parts WHERE offset + 131072 <= total
            )
            SELECT substr(drills.payload, parts.offset, 131072),
                   parts.offset + 131072 > parts.total
            FROM parts JOIN drills ON drills.id = parts.id
            ORDER BY parts.title COLLATE NOCASE, parts.id, parts.offset
            """, null)!;
        var drills = new List<Drill>();
        var payload = new System.Text.StringBuilder();
        while (cursor.MoveToNext())
        {
            payload.Append(cursor.GetString(0));
            if (cursor.GetInt(1) == 0)
                continue;
            var drill = JsonSerializer.Deserialize<Drill>(payload.ToString());
            payload.Clear();
            if (drill is not null)
                drills.Add(drill);
        }
        return drills;
    }

    public void Save(Drill drill)
    {
        using var id = new Java.Lang.String(drill.Id.ToString());
        using var title = new Java.Lang.String(drill.Title);
        using var payload = new Java.Lang.String(JsonSerializer.Serialize(drill));
        database.ExecSQL("INSERT OR REPLACE INTO drills (id, title, payload) VALUES (?, ?, ?)",
            [id, title, payload]);
    }

    public void Delete(Guid id) => database.Delete("drills", "id = ?", [id.ToString()]);

    public void Dispose() => database.Dispose();
}
