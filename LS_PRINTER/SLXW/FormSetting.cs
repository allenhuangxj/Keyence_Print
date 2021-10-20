using ConfigurationTool;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SLXW
{
    public partial class FormSetting : Form
    {
        public FormSetting()
        {
            InitializeComponent();
        }

        private void FormSetting_Load(object sender, EventArgs e)
        {
            numericUpDown_count.Value = Convert.ToDecimal(Configure.ReadConfig("SET", "COUNT", 1));
            textBox_model_grf.Text = Configure.ReadConfig("SET", "MODEL_GRF", "");
            checkBox_MarkNow.Checked = Convert.ToBoolean(Configure.ReadConfig("SET", "MarkNow", "True"));
        }

        private void button_oK_Click(object sender, EventArgs e)
        {
            Configure.WriteConfig("SET", "COUNT", numericUpDown_count.Value.ToString());
            Configure.WriteConfig("SET", "MODEL_GRF", textBox_model_grf.Text);
            Configure.WriteConfig("SET", "MarkNow", checkBox_MarkNow.Checked == true ? "True" : "False");
            this.DialogResult = DialogResult.OK;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.Description = "请选择文件路径";

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                textBox_model_grf.Text = dialog.SelectedPath;
            }
        }
    }
}
