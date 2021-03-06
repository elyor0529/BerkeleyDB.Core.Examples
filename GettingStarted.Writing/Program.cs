﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CsvHelper;

namespace GettingStarted.Writing
{
    class Program
    {
        static void Main()
        {
            List<long> commitTimes = new List<long>();


            if (String.IsNullOrEmpty(Environment.GetEnvironmentVariable("DATA_DIR")))
                throw new ApplicationException("DATA_DIR environment must be set to directory containing example CSV files.");
            //this value was set in the project settings. We'll read CSVs and write DB files there.
            var dataPath = Environment.GetEnvironmentVariable("DATA_DIR");
            var vendorRepo = new VendorRepository(dataPath);
            var invRepo = new InventoryRepository(dataPath);

            var readTimer = new Stopwatch();
            readTimer.Start();
            var vr = new CsvReader(File.OpenText(Path.Combine(dataPath, "vendor-master.csv")));
            vr.Configuration.HasHeaderRecord = false;
            vr.Configuration.RegisterClassMap<VendorMap>();
            vr.Configuration.DetectColumnCountChanges = true;
            var ir = new CsvReader(File.OpenText(Path.Combine(dataPath, "inv-master.csv")));
            ir.Configuration.HasHeaderRecord = false;
            ir.Configuration.DetectColumnCountChanges = true;
            
            int id = 1;

            int z = 0;
            var writeTimer = new Stopwatch();
            Vendor vendor;
            Inventory inv;
            while (vr.Read())
            {
                writeTimer.Start();
                int y = z + 5;
                vendor = vr.GetRecord<Vendor>();
                vendorRepo.AddVendor(id,vendor);

                for (int x = z; x < y; x++)
                {
                    if (ir.Read())
                    {
                        inv = new Inventory(id)
                        {
                            Vendor = id,
                            Category = ir.GetField(2),
                            Name = ir.GetField(3),
                            Price = ir.GetField<double>(0),
                            Quantity = ir.GetField<int>(1)
                        };
                        invRepo.AddInventory(ir.GetField(5), inv);
                    }
                    else
                    {
                        Console.WriteLine($"{DateTime.Now}\tno more inventory");
                    }
                }
                z = y;
                writeTimer.Stop();
                commitTimes.Add(writeTimer.ElapsedTicks);
                writeTimer.Reset();
            }

            vendorRepo.Save();
            readTimer.Stop();
            Console.WriteLine("-----------------------------");
            Console.WriteLine();
            Console.WriteLine($"Read of CSV files took {readTimer.Elapsed.Minutes}:{readTimer.Elapsed.Seconds}.{readTimer.Elapsed.Milliseconds}.");
            readTimer.Reset();
            readTimer.Start();
            vendorRepo.Sync();
            invRepo.Sync();
            readTimer.Stop();
            Console.WriteLine($"syncing databases took {readTimer.ElapsedMilliseconds} milliseconds.");
            Console.WriteLine();
            Console.WriteLine($" Average write for each iteration (1 vendor, 5 inventory) averaged {commitTimes.Average()} ticks. Min: {commitTimes.Min()} Max: {commitTimes.Max()}");
            Console.WriteLine("Press any key to exit.");

            vendorRepo.Dispose();
            invRepo.Dispose();

            Console.ReadLine();
        }
    }
}
