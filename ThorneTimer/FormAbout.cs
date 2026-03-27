using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ThorneTimer
{
    public partial class FormAbout : Form
    {
        public FormAbout()
        {
            InitializeComponent();
        }

        private void FormAbout_Load(object sender, EventArgs e)
        {
            System.Reflection.Assembly executingAssembly = System.Reflection.Assembly.GetExecutingAssembly();
            Version appVersion = executingAssembly.GetName().Version;
            labelVersion.Text = "Version " + appVersion.ToString();
        }

        private void ButtonOk_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
