using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MyLoggers;

namespace BuildLoger
{
    class Program
    {
        static void Main(string[] args)
        {
            var x = Utility.GetSettingValue<string>(Constant.K_SMTP_SERVER);
        }
    }
}
