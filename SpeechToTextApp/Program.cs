using System;
using System.Windows.Forms;

namespace SpeechToTextApp
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // .NET 6/7/8 WinForms startup standardý
            ApplicationConfiguration.Initialize();

            // bizim formumuzu aç
            Application.Run(new Form1());
        }
    }
}
