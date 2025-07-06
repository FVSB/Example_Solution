using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Axpo;
using static Axpo.PowerService; // Cambia esto al namespace real de PowerService.dll

namespace PowerPositionCalculator
{
    class Program
    {
        static void Main(string[] args)
        {
            DateTime date = new DateTime(2015,04,01);
            Calculate.Worker(date);
        }
    }
}

