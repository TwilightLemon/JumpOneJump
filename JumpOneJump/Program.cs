using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JumpOneJump
{
    class Program
    {
        static void Main(string[] args)
        {
            string adbPath = "adb"; // adb.exe路径

            PlayJumpJump testPlay = new PlayJumpJump(adbPath);
            if (testPlay.GetDevices())
            {
                while (true)
                {
                    testPlay.Jump2Happy();
                }
            }

            testPlay.DisposeProcess();

            Console.ReadKey();
        }
    }
}
