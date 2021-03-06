﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Management;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Microsoft.VisualBasic.Devices;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Globalization;
using System.Text.RegularExpressions;

namespace KeppyMIDIConverter
{
    public partial class InfoDialog : Form
    {
        // Funcs

        private RegistryKey CurrentVerKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion", false);
        public static FileVersionInfo Converter = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
        private static FileVersionInfo BASS = FileVersionInfo.GetVersionInfo(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\bass.dll");
        private static FileVersionInfo BASSMIDI = FileVersionInfo.GetVersionInfo(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\bassmidi.dll");

        private void InitializeLanguage()
        {
            Text = Languages.Parse("IATP");
            GitHubLink.Text = Languages.Parse("SourceCodeText");
            DonateBtn.Text = Languages.Parse("Donate");

            ConverterInfo.Text = Languages.Parse("ConverterInfo");
            WindowsInstallInfo.Text = Languages.Parse("WindowsInstallInfo");

            ConverterVerLabel.Text = String.Format(Languages.Parse("Version"), Languages.Parse("VersionConverter"));
            BASSVerLabel.Text = String.Format(Languages.Parse("Version"), "BASS");
            BASSMIDIVerLabel.Text = String.Format(Languages.Parse("Version"), "BASSMIDI");
            CompilerDateLabel.Text = Languages.Parse("CompilerDate");

            WindowsNameLabel.Text = Languages.Parse("WindowsName");
            WindowsBuildLabel.Text = Languages.Parse("WindowsBuild");

            CTC.Text = Languages.Parse("CTC");
            CFU.Text = Languages.Parse("CFU");
            OKClose.Text = Languages.Parse("OKBtn");
        }

        // Dialog

        public InfoDialog(Int32 mode)
        {
            InitializeComponent();
            InitializeLanguage();

            if (mode == 0)
                StartPosition = FormStartPosition.CenterParent;
            else
                StartPosition = FormStartPosition.CenterScreen;
        }

        private string ReturnBASSAssemblyVersion(String FileVersion, Int32 FilePrivatePart)
        {
            if (FilePrivatePart < 1)
                return String.Format("{0}", FileVersion, FilePrivatePart);
            else
                return String.Format(Languages.Parse("RevisionLabel"), FileVersion, FilePrivatePart);
        }

        private void InfoDialog_Load(object sender, EventArgs e)
        {
            ComputerInfo CI = new ComputerInfo();

            String Version = String.Format("{0}.{1}.{2}", Converter.FileMajorPart, Converter.FileMinorPart, Converter.FileBuildPart);
            TaCI.Text = String.Format(Languages.Parse("TaCI"), Converter.FileMajorPart, DateTime.Now.Year, Languages.Parse("0Translators0"));
            ConverterVer.Text = String.Format("{0} ({1})", Version, (Environment.Is64BitProcess ? "x64, SSE2" : "x86, SSE"));
            BASSVer.Text = ReturnBASSAssemblyVersion(BASS.FileVersion, BASS.FilePrivatePart);
            BASSMIDIVer.Text = ReturnBASSAssemblyVersion(BASSMIDI.FileVersion, BASSMIDI.FilePrivatePart);
            CompilerDate.Text = BasicFunctions.GetLinkerTime(Assembly.GetExecutingAssembly(), TimeZoneInfo.Utc).ToString(Languages.ReturnCulture(false, null));

            OSInfo.OSVERSIONINFOEX osVersionInfo = new OSInfo.OSVERSIONINFOEX
            {
                dwOSVersionInfoSize = Marshal.SizeOf(typeof(OSInfo.OSVERSIONINFOEX))
            };

            if (Properties.Settings.Default.IsItPreRelease) ConverterVer.Text += " (PRERELEASE)";

            WindowsName.Text = String.Format("{0} ({1})", OSInfo.Name, Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit");

            if (Environment.OSVersion.Version.Major == 10) // If OS is Windows 10, get UBR too
            {
                WindowsBuild.Text = String.Format(Languages.Parse("W10VerRev"),
                   CurrentVerKey.GetValue("ReleaseId", 0).ToString(), CurrentVerKey.GetValue("UBR", 0).ToString());
            }
            else // Else, give normal version number
            {
                if (Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor > 1)
                {
                    WindowsBuild.Text = String.Format("{0}.{1}.{2}",
                        Environment.OSVersion.Version.Major, Environment.OSVersion.Version.Minor,
                        Environment.OSVersion.Version.Build);
                }
                else
                {
                    int SP = Int32.Parse(Regex.Match(Environment.OSVersion.ServicePack, @"\d+").Value, NumberFormatInfo.InvariantInfo);

                    if (SP > 0)
                    {
                        WindowsBuild.Text = String.Format("{0}.{1}.{2} ({3})",
                            Environment.OSVersion.Version.Major, Environment.OSVersion.Version.Minor,
                            Environment.OSVersion.Version.Build, Environment.OSVersion.ServicePack);
                    }
                    else
                    {
                        WindowsBuild.Text = String.Format("{0}.{1}.{2}",
                            Environment.OSVersion.Version.Major, Environment.OSVersion.Version.Minor,
                            Environment.OSVersion.Version.Build);
                    }
                }
            }
        }

        private void OKClose_Click(object sender, EventArgs e)
        {
            CurrentVerKey.Close();
            Close();
        }

        private void GitHubLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://github.com/KaleidonKep99/Keppys-MIDI-Converter");
        }

        private string CPUArch(int Value)
        {
            if (Value == 0)
                return "x86";
            else if (Value == 6)
                return "IA64";
            else if (Value == 9)
                return "x64";
            else
                return "N/A";
        }

        private void CTC_Click(object sender, EventArgs e)
        {
            try
            {
                ManagementObjectSearcher mosProcessor = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM CIM_Processor");
                ManagementObjectSearcher mosGPU = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM CIM_VideoController");

                String cpubit = "??";
                Int32 cpuclock = 0;
                String cpumanufacturer = "Unknown";
                String cpuname = "Unknown";
                String gpuname = "Unknown";
                String gpuver = "N/A";
                UInt32 gpuvram = 0;
                String Frequency = "0";
                String coreCount = "1";

                // Get CPU info
                foreach (ManagementObject moProcessor in mosProcessor.Get())
                {
                    cpuclock = int.Parse(moProcessor["maxclockspeed"].ToString());
                    cpubit = CPUArch(int.Parse(moProcessor["Architecture"].ToString()));
                    cpuname = moProcessor["name"].ToString();
                    cpumanufacturer = moProcessor["manufacturer"].ToString();
                    coreCount = moProcessor["NumberOfCores"].ToString();
                }

                // Get GPU info
                foreach (ManagementObject moGPU in mosGPU.Get())
                {
                    gpuname = moGPU["Name"].ToString();
                    gpuvram = Convert.ToUInt32(moGPU["AdapterRAM"]);
                    gpuver = moGPU["DriverVersion"].ToString();
                }

                if (cpuclock < 1000)
                    Frequency = String.Format("{0}MHz", cpuclock);
                else
                    Frequency = String.Format("{0}GHz", ((float)cpuclock / 1000).ToString("0.00"));

                // Ok, print everything to the string builder
                StringBuilder sb = new StringBuilder();
                sb.Append(String.Format("Keppy's MIDI Converter Information Dialog\n\n", ConverterVer.Text));
                sb.Append("== Driver info =================================================\n");
                sb.Append(String.Format("Driver version: {0}\n", ConverterVer.Text));
                sb.Append(String.Format("BASS version: {0}\n", BASSVer.Text));
                sb.Append(String.Format("BASSMIDI version: {0}\n", BASSMIDIVer.Text));
                sb.Append(String.Format("Compiled on: {0}\n\n", CompilerDate.Text));
                sb.Append("== Windows installation info ===================================\n");
                sb.Append(String.Format("Name: {0}\n", WindowsName.Text));
                sb.Append(String.Format("Version: {0}\n\n", WindowsBuild.Text));
                sb.Append("== Computer info ===============================================\n");
                sb.Append(String.Format("Processor: {0} ({1})\n", cpuname, cpubit));
                sb.Append(String.Format("Processor info: {1} cores and {2} threads, running at {3}\n", cpumanufacturer, coreCount, Environment.ProcessorCount, Frequency));
                sb.Append(String.Format("Graphics card: {0}\n", gpuname));
                sb.Append(String.Format("Graphics card info: {0}MB VRAM, driver version {1}\n\n", (gpuvram / 1048576), gpuver));
                sb.Append("================================================================\n");
                sb.Append(String.Format("End of info. Got them on {0}.", DateTime.Now.ToString()));

                // Copy to clipboard
                Clipboard.SetText(sb.ToString());
                sb = null;

                MessageBox.Show("Copied to clipboard.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                string message = String.Format("Exception type: {0}\nException message: {1}\nStack trace: {2}", ex.GetType(), ex.Message, ex.StackTrace);
                String Error = String.Format("An error has occured while copying the info to the clipboard.\n\nError:\n{0}\n\nDo you want to try again?", message);
                DialogResult dialogResult = MessageBox.Show(Error, "Error", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (dialogResult == DialogResult.Yes)
                {
                    CTC.PerformClick();
                }
            }
        }

        private void CFU_Click(object sender, EventArgs e)
        {
            BasicFunctions.CheckForUpdates(false);
        }

        private void DonateBtn_Click(object sender, EventArgs e)
        {
            BasicFunctions.Donate();
        }

        private void PatreonBtn_Click(object sender, EventArgs e)
        {
            new BecomeAPatron().ShowDialog();
        }
    }
}
