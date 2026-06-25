# Schema Migration Plan

> **🗄️ Archived proposal — not implemented.** This proposed a `view_timers` junction table, `CustomColors` JSON column, and a `schema_version` table. v0.6.0 took a different path: per-view `ForeColor` / `BackColor` columns directly on `miniviews`, no junction table, idempotent `isTableExist` / `isFieldExist` checks in `Database.cs` instead of a versions table. Retained for reference only.

> **Last Updated:** 2026-03-27  
> **Related:** [active-views-design.md](../../ThorneTimer/Docs/active-views/active-views-design.md)

---

## Overview

This document details the database schema changes required for the Active Views feature and provides a migration strategy that preserves existing user data.

---

## Current Schema State

### Existing `miniviews` Table (Incomplete)

```sql
CREATE TABLE miniviews (
    ID INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT
);
```

**Status:** Table exists but is unused. Only `ID` and `Name` columns present.

---

## Target Schema

### Expanded `miniviews` Table

```sql
CREATE TABLE miniviews (
    ID INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    CharacterID INTEGER,              -- NULL = global view, set = per-character
    FilterType TEXT DEFAULT 'Type',   -- 'Category', 'Type', 'Manual', 'All'
    FilterValue TEXT,                 -- CategoryID, type name, or timer IDs
    PositionX INTEGER DEFAULT 100,
    PositionY INTEGER DEFAULT 100,
    Width INTEGER DEFAULT 0,          -- 0 = auto-size
    Height INTEGER DEFAULT 0,
    SortOrder INTEGER DEFAULT 0,
    IsVisible INTEGER DEFAULT 1,
    ColorScheme TEXT DEFAULT 'Normal', -- 'Normal', 'Pet', 'Buff', 'Ping', 'Custom'
    CustomColors TEXT,                -- JSON: {"fore": "#FFFFFF", "back": "#000000"}
    FOREIGN KEY (CharacterID) REFERENCES characters(ID) ON DELETE SET NULL
);
```

### New `view_timers` Junction Table

```sql
CREATE TABLE view_timers (
    ViewID INTEGER NOT NULL,
    TimerID INTEGER NOT NULL,
    PRIMARY KEY (ViewID, TimerID),
    FOREIGN KEY (ViewID) REFERENCES miniviews(ID) ON DELETE CASCADE,
    FOREIGN KEY (TimerID) REFERENCES timers(ID) ON DELETE CASCADE
);
```

### New `schema_version` Table

```sql
CREATE TABLE schema_version (
    Version INTEGER NOT NULL,
    AppliedAt TEXT NOT NULL
);
```

---

## Migration Steps

### Step 1: Add Schema Versioning

```csharp
// Add to Database.cs
static int GetSchemaVersion(SQLiteConnection con)
{
    if (!isTableExist(con, "schema_version"))
    {
        SQLiteCommand cmd = new SQLiteCommand(con);
        cmd.CommandText = "CREATE TABLE schema_version (Version INTEGER NOT NULL, AppliedAt TEXT NOT NULL)";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO schema_version (Version, AppliedAt) VALUES (0, @now)";
        cmd.Parameters.AddWithValue("@now", DateTime.UtcNow.ToString("o"));
        cmd.ExecuteNonQuery();
        return 0;
    }
    
    SQLiteCommand query = new SQLiteCommand(con);
    query.CommandText = "SELECT MAX(Version) FROM schema_version";
    object result = query.ExecuteScalar();
    return result == DBNull.Value ? 0 : Convert.ToInt32(result);
}

static void SetSchemaVersion(SQLiteConnection con, int version)
{
    SQLiteCommand cmd = new SQLiteCommand(con);
    cmd.CommandText = "INSERT INTO schema_version (Version, AppliedAt) VALUES (@ver, @now)";
    cmd.Parameters.AddWithValue("@ver", version);
    cmd.Parameters.AddWithValue("@now", DateTime.UtcNow.ToString("o"));
    cmd.ExecuteNonQuery();
}
```

### Step 2: Migration Version 1 — Expand miniviews

```csharp
static void RunMigration1(SQLiteConnection con)
{
    // Check if migration already applied
    if (isFieldExist(con, "miniviews", "FilterType"))
        return;
    
    SQLiteCommand cmd = new SQLiteCommand(con);
    
    // Add new columns to miniviews
    string[] newColumns = {
        "ALTER TABLE miniviews ADD COLUMN CharacterID INTEGER",
        "ALTER TABLE miniviews ADD COLUMN FilterType TEXT DEFAULT 'Type'",
        "ALTER TABLE miniviews ADD COLUMN FilterValue TEXT",
        "ALTER TABLE miniviews ADD COLUMN PositionX INTEGER DEFAULT 100",
        "ALTER TABLE miniviews ADD COLUMN PositionY INTEGER DEFAULT 100",
        "ALTER TABLE miniviews ADD COLUMN Width INTEGER DEFAULT 0",
        "ALTER TABLE miniviews ADD COLUMN Height INTEGER DEFAULT 0",
        "ALTER TABLE miniviews ADD COLUMN SortOrder INTEGER DEFAULT 0",
        "ALTER TABLE miniviews ADD COLUMN IsVisible INTEGER DEFAULT 1",
        "ALTER TABLE miniviews ADD COLUMN ColorScheme TEXT DEFAULT 'Normal'",
        "ALTER TABLE miniviews ADD COLUMN CustomColors TEXT"
    };
    
    foreach (string sql in newColumns)
    {
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }
    
    // Create view_timers junction table
    cmd.CommandText = @"
        CREATE TABLE IF NOT EXISTS view_timers (
            ViewID INTEGER NOT NULL,
            TimerID INTEGER NOT NULL,
            PRIMARY KEY (ViewID, TimerID),
            FOREIGN KEY (ViewID) REFERENCES miniviews(ID) ON DELETE CASCADE,
            FOREIGN KEY (TimerID) REFERENCES timers(ID) ON DELETE CASCADE
        )";
    cmd.ExecuteNonQuery();
    
    SetSchemaVersion(con, 1);
}
```

### Step 3: Migration Version 2 — Create Default Views

```csharp
static void RunMigration2(SQLiteConnection con)
{
    // Only create default views if none exist
    SQLiteCommand countCmd = new SQLiteCommand(con);
    countCmd.CommandText = "SELECT COUNT(*) FROM miniviews";
    int viewCount = Convert.ToInt32(countCmd.ExecuteScalar());
    
    if (viewCount > 0)
    {
        SetSchemaVersion(con, 2);
        return;
    }
    
    // Create 4 legacy views
    string insertSql = @"
        INSERT INTO miniviews (Name, FilterType, FilterValue, PositionX, PositionY, ColorScheme, SortOrder)
        VALUES (@name, 'Type', @filter, @x, @y, @color, @order)";
    
    var defaultViews = new[] {
        ("Normal Timers", "Normal", 100, 100, "Normal", 1),
        ("Pet Timers", "Pet", 300, 100, "Pet", 2),
        ("Buff Timers", "Buff", 500, 100, "Buff", 3),
        ("Ping Timers", "Ping", 700, 100, "Ping", 4)
    };
    
    SQLiteCommand cmd = new SQLiteCommand(con);
    cmd.CommandText = insertSql;
    
    foreach (var (name, filter, x, y, color, order) in defaultViews)
    {
        cmd.Parameters.Clear();
        cmd.Parameters.AddWithValue("@name", name);
        cmd.Parameters.AddWithValue("@filter", filter);
        cmd.Parameters.AddWithValue("@x", x);
        cmd.Parameters.AddWithValue("@y", y);
        cmd.Parameters.AddWithValue("@color", color);
        cmd.Parameters.AddWithValue("@order", order);
        cmd.ExecuteNonQuery();
    }
    
    SetSchemaVersion(con, 2);
}
```

### Step 4: Migration Runner

```csharp
// Add to Database.Connection() after initial table creation
static void RunAllMigrations(SQLiteConnection con)
{
    int currentVersion = GetSchemaVersion(con);
    
    if (currentVersion < 1)
    {
        RunMigration1(con);
    }
    
    if (currentVersion < 2)
    {
        RunMigration2(con);
    }
    
    // Future migrations go here
    // if (currentVersion < 3) RunMigration3(con);
}
```

---

## Integration with Database.Connection()

```csharp
static public SQLiteConnection Connection()
{
    // ... existing connection setup ...
    
    SQLiteConnection con = new SQLiteConnection("URI=file:" + newDbName);
    con.Open();
    
    if (newDatabase)
    {
        // Create initial tables (existing code)
        CreateInitialTables(con);
    }
    
    // Run migrations for both new and existing databases
    RunAllMigrations(con);
    
    return con;
}
```

---

## Rollback Strategy

SQLite doesn't support `ALTER TABLE DROP COLUMN` easily, so rollbacks are limited. For safety:

1. **Backup before upgrade** — Copy `ThorneTimer.db` before running migrations
2. **Version tracking** — `schema_version` table shows migration history
3. **Additive changes only** — New columns don't break old code

### Manual Rollback SQL (if needed)

```sql
-- To revert to pre-migration state:
-- 1. Export data you want to keep
-- 2. Drop and recreate table

-- Save existing view names (if any)
CREATE TEMP TABLE miniviews_backup AS SELECT ID, Name FROM miniviews;

-- Recreate original table
DROP TABLE miniviews;
CREATE TABLE miniviews (ID INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT);

-- Restore data
INSERT INTO miniviews (ID, Name) SELECT ID, Name FROM miniviews_backup;
DROP TABLE miniviews_backup;

-- Remove view_timers
DROP TABLE IF EXISTS view_timers;

-- Reset version
DELETE FROM schema_version WHERE Version >= 1;
```

---

## Testing Checklist

### Fresh Install
- [ ] New database created with all tables
- [ ] Schema version table created at version 2
- [ ] 4 default views created with correct positions/colors
- [ ] view_timers table exists (empty)

### Upgrade from Existing Database
- [ ] Existing miniviews rows preserved
- [ ] New columns added with defaults
- [ ] No duplicate views created if already populated
- [ ] Schema version increments correctly

### Edge Cases
- [ ] Empty miniviews table → default views created
- [ ] Database with only some columns → remaining added
- [ ] Concurrent access during migration (file lock handling)

---

## Timeline

| Phase | Migration | Status |
|-------|-----------|--------|
| Pre-work | Add schema_version table | Planned |
| Phase 1 | Migration 1: Expand miniviews | Planned |
| Phase 1 | Migration 2: Create default views | Planned |
| Phase 3 | Migration 3: Per-character views data | Future |

---

*This document should be updated as migrations are implemented and tested.*
