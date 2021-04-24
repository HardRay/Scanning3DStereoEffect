using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Program
{
    public partial class CalculationForm : Form
    {
        public CalculationForm()
        {
            InitializeComponent();
        }

        public ProgressBar ProgressBar()
        {
            return progressBar1;
        }

        public void ChangeDel(int value)
        {
            RTChangeValue rt = new RTChangeValue(ChangeValue);
            //Таким обра3ом его исполь3уешь
            progressBar1.BeginInvoke(rt, new object[] { value });
        }

        public void ChangeValue(int value)
        {
            this.progressBar1.Value = value;
        }

        public delegate void RTChangeValue(int value);

        private void EquaButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
