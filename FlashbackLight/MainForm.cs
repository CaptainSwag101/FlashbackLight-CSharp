using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using FlashbackLight.Formats;

namespace FlashbackLight
{
    public partial class MainForm : Form
    {
        public static string RegionString = "US";
        private static string currentSPCFilename;
        private static SPC currentSPC;
        private static string currentWRDFilename;
        private static WRD currentWRD;


        public MainForm()
        {
            InitializeComponent();
        }

        private void openScriptEntry(string entryName)
        {
            foreach (SPCEntry entry in currentSPC.Entries)
            {
                if (entry.Filename != entryName)
                    continue;

                // Convert the entry data into the appropriate format, based on file extension
                // TODO: This is REALLY SLOW, see if we can speed it up
                string ext = entry.Filename.Split('.').Last().ToUpper();
                /*
                switch (ext)
                {
                    case "DAT":

                        break;

                    case "SPC":
                        entry.Contents = new SPC(entry.Contents, entry.Filename);
                        break;

                    case "SRD":

                        break;

                    case "STX":
                        entry.Contents = new STX(entry.Contents);
                        break;

                    case "WRD":
                        entry.Contents = new WRD(entry.Contents, spcName, entry.Filename);
                        break;
                }
                */

                if (ext == "WRD")
                {
                    currentWRD = new WRD(entry.Contents, currentSPCFilename, entryName);
                    currentWRDFilename = entryName;
                }

                break;
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog fd = new OpenFileDialog();
            fd.ShowDialog();
            string filepath = fd.FileName;
            
            if (File.Exists(filepath))
            {
                byte[] filedata;
                filedata = File.ReadAllBytes(filepath);
                currentSPC = new SPC(filedata, filepath);
                currentSPCFilename = filepath;
            }

            currentSPCEntryList.Items.Clear();
            foreach (SPCEntry entry in currentSPC.Entries)
            {
                currentSPCEntryList.Items.Add(entry.Filename);
            }
        }

        private void currentSPCEntryList_DoubleClick(object sender, EventArgs e)
        {
            if (currentWRDFilename != currentSPC.Entries[currentSPCEntryList.SelectedIndex].Filename)
                openScriptEntry(currentSPC.Entries[currentSPCEntryList.SelectedIndex].Filename);
        }
    }
}
