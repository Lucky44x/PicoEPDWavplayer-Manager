using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MP3Manager
{
    public partial class DatabaseFlasher : Form
    {
        private string current_step = "Databases";

        public DatabaseFlasher()
        {
            InitializeComponent();
        }

        public void change_flash_step(string new_step)
        {
            current_step = new_step;
            progressBar1.Value = 0;
            label1.Text = "Flashing " + current_step + " : " + progressBar1.Value + "%";
        }

        public void update_progress(int progress)
        {
            progressBar1.Value = progress;
            label1.Text = "Flashing " + current_step + " : " + progress.ToString() + "%";
        }

        public void flashing_completed()
        {
            Close();
        }
    }
}
