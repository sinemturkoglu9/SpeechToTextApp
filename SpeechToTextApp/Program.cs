using System;
using System.Windows.Forms;

namespace SpeechToTextApp
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // .NET 6/7/8 WinForms startup standard�
            ApplicationConfiguration.Initialize();

            // bizim formumuzu a�
            Application.Run(new Form1());
        }
    }
}
