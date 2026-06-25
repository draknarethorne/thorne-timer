using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace ThorneTimer
{
    /// <summary>
    /// Repository for the reference <c>classes</c> table (EQ class list).
    ///
    /// Classes are a shared reference set used by both <c>characters</c>
    /// (a character has one ClassID) and <c>timers</c> (a timer can be
    /// scoped to a single class via ClassID).  This repository owns the
    /// classes lookup so neither Characters nor Timers has to take a
    /// dependency on the other.
    /// </summary>
    class ClassesRepository
    {
        private readonly SQLiteConnection con;

        public ClassesRepository(SQLiteConnection con)
        {
            this.con = con;
        }

        // ---------------------------------------------------------------
        // Static API â€” matches the original Database.GetGridClasses
        // signature so existing call sites compile unchanged.
        // ---------------------------------------------------------------

        /// <summary>
        /// Returns combo-box items for EQ class selection, with a leading
        /// "All" row representing "no class restriction" (ID = 0).
        /// Used by the Characters grid (assigned class) and the Timers grid
        /// (class filter / restriction).
        /// </summary>
        static internal List<ComboBoxItem> GetGridClasses(SQLiteConnection con)
        {
            var cboData = new List<ComboBoxItem>
            {
                new ComboBoxItem { Value = 0, Text = "All" }
            };

            if (!Database.isTableExist(con, "classes")) return cboData;

            using (var cmd = new SQLiteCommand("SELECT * from classes ORDER BY Name", con))
            using (var rdr = cmd.ExecuteReader())
            {
                while (rdr.Read())
                {
                    cboData.Add(new ComboBoxItem
                    {
                        Value = rdr.GetInt64(rdr.GetOrdinal("ID")),
                        Text = rdr.GetString(rdr.GetOrdinal("Name"))
                    });
                }
            }

            return cboData;
        }
    }
}
