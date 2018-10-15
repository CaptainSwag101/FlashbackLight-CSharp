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
        private static (string filename, V3Format data) currentFile;


        public MainForm()
        {
            InitializeComponent();
        }

        private void openDataEntry(string entryName)
        {
            currentFile = ("", null);

            if (currentSPC.Entries.TryGetValue(entryName, out var entry))
            {
                // Convert the entry data into the appropriate format, based on file extension
                // TODO: This is REALLY SLOW, see if we can speed it up

                // Change: We don't need to decode every file within an SPC whenever we open one of them

                string ext = Path.GetExtension(entry.Filename).ToUpper();

                switch (ext)
                {
                    //case ".DAT":

                    //    break;

                    //case ".SPC":
                        
                    //    break;

                    //case ".SRD":

                    //    break;

                    case ".STX":
                        currentFile = (entryName, new STX(entry.Contents));
                        break;

                    case ".WRD":
                        currentFile = (entryName, new WRD(entry.Contents, currentSPCFilename, entryName));
                        break;

                    default:
                        break;
                }
            }
            displayCurrentFile();
        }

        private void displayCurrentFile()
        {
            wrdViewer.Visible = false;
            stxViewer.Visible = false;

            switch (currentFile.data)
            {
                case WRD stx:
                    wrdViewer.Visible = true;
                    refreshWRDCommandList(stx);
                    break;
                case STX stx:
                    stxViewer.Visible = true;
                    refreshSTXStringList(stx);
                    break;
                default:
                    break;
            }
        }

        private void refreshWRDCommandList(WRD wrd)
        {
            currentWRDCommandList.Items.Clear();
            foreach (WRDCmd cmd in wrd.Code)
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
                            argString = arg < wrd.Params.Count ? wrd.Params[arg] : '!' + arg.ToString() + '!';
                            break;

                        case 1: // Raw number
                            argString = arg.ToString();
                            break;

                        case 2: // Dialog string
                            argString = arg < wrd.Strings.Count ? '"' + wrd.Strings[arg] + '"' : '!' + arg.ToString() + '!';
                            break;

                        case 3: // Label name
                            argString = arg < wrd.Labels.Count ? wrd.Labels[arg] : '!' + arg.ToString() + '!';
                            break;
                    }

                    // Check if we have too many arguments
                    if (!cmd.IsVarLength && i >= cmd.ArgTypes.Length)
                    {
                        argString.Insert(0, "!");
                        argString.Insert(argString.Length, "!");    // Should this be (argString.Length - 1)?
                    }

                    if (i + 1 < cmd.ArgData.Length && !cmd.IsVarLength)
                    {
                        argString += ", ";
                    }

                    commandString += argString;
                }
                commandString += ")";

                currentWRDCommandList.Items.Add(commandString);
            }
        }

        private void refreshSTXStringList(STX stx)
        {
            currentSTXStringList.DataSource = stx.Strings;
        }

        private void showOpenErrorBox(Exception error, string filepath)
        {
            if (MessageBox.Show($"Failed to open {filepath}: \n\n{error.Message}\n\n{error.StackTrace}\n\nWould you like to copy this error to your clipboard?",
                                    $"{Path.GetExtension(filepath)} Open Error: {error.Message}",
                                    MessageBoxButtons.YesNo,
                                    MessageBoxIcon.Error,
                                    MessageBoxDefaultButton.Button2) == DialogResult.Yes)
            {
                Clipboard.SetText($"{error.Message}\n\n{error.StackTrace}");
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog fd = new OpenFileDialog();
            fd.ShowDialog();
            string filepath = fd.FileName;
            if (filepath == "")
            {
                return;
            }
            else if (Path.GetExtension(filepath).ToLower() != ".spc")
            {
                if (MessageBox.Show("Selected file does not have the .SPC file extension and may not load properly. Attempt to open anyways?",
                                    "SPC Extension Warning",
                                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                {
                    return;
                }
            }
            if (File.Exists(filepath))
            {
                try
                {
                    byte[] filedata;
                    filedata = File.ReadAllBytes(filepath);
                    currentSPC = new SPC(filedata, filepath);
                    currentSPCFilename = filepath;
                }
                catch (Exception error)
                {
                    showOpenErrorBox(error, filepath);
                    return;
                }
            }

            currentSPCEntryList.Items.Clear();
            foreach (string entryName in currentSPC.Entries.Keys)
            {
                currentSPCEntryList.Items.Add(entryName);
            }
        }

        private void currentSPCEntryList_DoubleClick(object sender, EventArgs e)
        {
            if (currentSPC == null)
                return;

            string entryFilename = currentSPC.Entries.Keys.ToArray()[currentSPCEntryList.SelectedIndex];
            try
            {
                if (currentFile.filename != entryFilename)
                    openDataEntry(entryFilename);
            }
            catch (Exception error)
            {
                showOpenErrorBox(error, entryFilename);
                return;
            }
        }
    }
}
