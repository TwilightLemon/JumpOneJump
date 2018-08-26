using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JumpOneJump
{

    class Cmd
    {
        private System.Diagnostics.Process process;
        private bool isExecuted;    // 是否执行过命令
        private string command;     // 上次执行命令
        private int result;         // 上次执行命令结果
        private string resultContent;   // 上次执行命令返回结果
        public Cmd()
        {
            process = new System.Diagnostics.Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.UseShellExecute = false;    //是否使用操作系统shell启动
            process.StartInfo.RedirectStandardInput = true;//接受来自调用程序的输入信息
            process.StartInfo.RedirectStandardOutput = true;//由调用程序获取输出信息
            process.StartInfo.RedirectStandardError = true;//重定向标准错误输出
            process.StartInfo.CreateNoWindow = true;//不显示程序窗口

            isExecuted = false;
        }
        public int ExecuteCmd(string cmd)
        {
            command = cmd;
            try
            {
                process.Start();
                process.StandardInput.WriteLine(cmd + "&exit");
                process.StandardInput.AutoFlush = true;
                string content = process.StandardOutput.ReadToEnd();
                process.WaitForExit();//等待程序执行完退出进程
                process.Close();

                result = 0;
                resultContent = content.Split(new string[] { "&exit" }, StringSplitOptions.None)[1].Replace("\n", "");
            }
            catch (Exception ex)
            {
                result = -1;
                resultContent = ex.Message;
            }

            if (!isExecuted) isExecuted = true;

            return result;
        }
        private int ExecuteCmd(string adbPath, string cmd)
        {
            command = $"\"{adbPath}\" {cmd}";
            try
            {
                process.Start();
                process.StandardInput.WriteLine(command + "&exit");
                process.StandardInput.AutoFlush = true;
                string content = process.StandardOutput.ReadToEnd();
                process.WaitForExit();//等待程序执行完退出进程
                process.Close();

                result = 0;
                resultContent = content.Split(new string[] { "&exit" }, StringSplitOptions.None)[1].Replace("\n", "");
            }
            catch (Exception ex)
            {
                result = -1;
                resultContent = ex.Message;
            }

            if (!isExecuted) isExecuted = true;

            return result;
        }
        public string GetExcResult()
        {
            if (isExecuted)
            {
                if (result == 0)
                {
                    return resultContent;
                }
                else
                {
                    return $"Execute Failed! Command:{command}\n{resultContent}";
                }
            }
            else
            {
                return "从未执行过命令";
            }
        }
        public void DisposeProcess()
        {
            process.Dispose();
        }
    }

    class Pixel
    {
        public byte[] pixel = new byte[3];
        public Pixel()
        {

        }
    }
}
