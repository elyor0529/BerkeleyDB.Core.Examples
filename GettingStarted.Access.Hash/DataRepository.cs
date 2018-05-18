﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BerkeleyDB.Core;

namespace GettingStarted.Access.Hash
{
    public abstract class Repository : IDisposable
    {
        protected string path;
        protected HashDatabase db;
        protected DatabaseEnvironment env;
        protected Repository(string dataPath, string databaseName, uint tableSize)
        {
            path = dataPath;
            var cfg = new HashDatabaseConfig
            {
                Creation = CreatePolicy.IF_NEEDED,
                CacheSize = new CacheInfo(1, 0, 1),
                ErrorFeedback = (prefix, message) =>
                {
                    var fg = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"{prefix}: {message}");
                    Console.ForegroundColor = fg;
                },
                ErrorPrefix = databaseName,
                Duplicates = DuplicatesPolicy.SORTED,
                TableSize = tableSize
            };

            db = HashDatabase.Open(Path.Combine(dataPath,databaseName +".db"),cfg);
            
        }


        ~Repository()
        {
            Dispose(false);
        }

        protected void AddToDb(string keyval, byte[] dataval)
        {
            var key = new DatabaseEntry(Encoding.UTF8.GetBytes(keyval));
            var data = new DatabaseEntry(dataval);
            db.Put(key, data);
        }

        public void Sync()
        {
            Console.WriteLine("I'm syncing!");
            db.Sync();
        }

        private void Dispose(bool disposing)
        {
            if (!disposing) return;
            db?.Close(true);
            db?.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
    /// <summary>
    /// The data repository for our Vendor database.
    /// </summary>
    public class VendorRepository : Repository
    {
        private List<DatabaseEntry> keyList;
        private List<DatabaseEntry> valueList;

        public VendorRepository(string dataPath) : base(dataPath, "vendor",8)
        {
            keyList = new List<DatabaseEntry>();
            valueList = new List<DatabaseEntry>();
        }

        public void AddVendor(Vendor v)
        {
            keyList.Add(new DatabaseEntry(v.VendorName.ToByteArray()));
            valueList.Add(new DatabaseEntry(v.ToByteArray()));
        }

        public void Save()
        {
            db.Put(new MultipleDatabaseEntry(keyList, false), new MultipleDatabaseEntry(valueList, false));
        }
    }

    /// <summary>
    /// The repository for our inventory database.
    /// </summary>
    public class InventoryRepository : Repository
    {
        public InventoryRepository(string dataPath) : base(dataPath, "inventory",5) { }
        public void AddInventory(string sku, Inventory inv) { AddToDb(sku, inv.ToByteArray()); }
    }
}