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
    }
}
