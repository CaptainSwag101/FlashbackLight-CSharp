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
                    refreshWRDCommandList();
                }

                break;
            }
        }

        private void refreshWRDCommandList()
        {
            currentWRDCommandList.Items.Clear();
            foreach (WRDCmd cmd in currentWRD.Code)
            {
                string commandString = cmd.Name + "(";
                for (int i = 0; i < cmd.ArgData.Length; i++)
                {
                    ushort arg = cmd.ArgData[i];
                    string argString = "!ERROR!";

                    // Use modulus to prevent out-of-range errors for variable-length opcodes
                    // and for invalid parameter lengths. We'll handle those afterwards.
                    byte argtype = cmd.ArgTypes[i % cmd.ArgTypes.Length];
                    switch (argtype)
                    {
                        case 0: // Plaintext Parameter
                            argString = arg < currentWRD.Params.Count ? currentWRD.Params[arg] : '!' + arg.ToString() + '!';
                            break;

                        case 1: // Raw number
                            argString = arg.ToString();
                            break;

                        case 2: // Dialog string
                            argString = arg < currentWRD.Strings.Count ? '"' + currentWRD.Strings[arg] + '"' : '!' + arg.ToString() + '!';
                            break;

                        case 3: // Label name
                            argString = arg < currentWRD.Labels.Count ? currentWRD.Labels[arg] : '!' + arg.ToString() + '!';
                            break;
                    }

                    // Check if we have too many arguments
                    if (!cmd.IsVarLength && i >= cmd.ArgTypes.Length)
                    {
                        argString.Insert(0, "!");
                        argString.Insert(argString.Length, "!");    // Should this be (argString.Length - 1)?
                    }

                    if (i + 1 < cmd.ArgData.Length)
                    {
                        argString += ", ";
                    }

                    commandString += argString;
                }
                commandString += ")";

                currentWRDCommandList.Items.Add(commandString);
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
