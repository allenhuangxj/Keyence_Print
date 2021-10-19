using gregn6Lib;
using System.IO;

namespace PrintReport
{
    class Print
    {
        // 保证debug模式，没有安装gridpord++也可以正常打开调试
#if !DEBUG
        private GridppReport Report = new GridppReport();
#endif

        public bool LoadGrfFile(string strTemplate)
        {
            if (!File.Exists(strTemplate))
            {
                return false;
            }
#if DEBUG
        return true;
#else
            return Report.LoadFromFile(strTemplate);
#endif
        }
        public bool PrintDoc(bool bShowPrintDialog)
        {
#if !DEBUG  
            Report.Print(bShowPrintDialog);
#endif
            return true;
        }

        public bool PrintPreview(bool bShowModal)
        {
#if !DEBUG
            Report.PrintPreview(bShowModal);
#endif
            return true;
        }

        public bool ChangeTextByName(string name, string text)
        {
#if !DEBUG
            IGRControl ControlCommon = Report.ControlByName(name);
            if (ControlCommon.ControlType == GRControlType.grctBarcode)
            {
                IGRBarcode control = ControlCommon.AsBarcode;
                control.Text = text;
            }
            else if (ControlCommon.ControlType == GRControlType.grctShapeBox)
            {
                System.Windows.Forms.MessageBox.Show("ShapeBox类型不能替换文本");
                return false;
            }
            else if (ControlCommon.ControlType == GRControlType.grctStaticBox)
            {
                IGRStaticBox control = ControlCommon.AsStaticBox;
                control.Text = text;
            }
            else if (ControlCommon.ControlType == GRControlType.grctSystemVarBox)
            {
                System.Windows.Forms.MessageBox.Show("SystemVarBox类型不能替换文本");
                return false;
            }
            else if (ControlCommon.ControlType == GRControlType.grctFieldBox)
            {
                System.Windows.Forms.MessageBox.Show("FieldBox类型不能替换文本");
                return false;
            }
            else if (ControlCommon.ControlType == GRControlType.grctSummaryBox)
            {
                System.Windows.Forms.MessageBox.Show("SummaryBox类型不能替换文本");
                return false;
            }
            else if (ControlCommon.ControlType == GRControlType.grctRichTextBox)
            {
                System.Windows.Forms.MessageBox.Show("RichTextBox类型不能替换文本");
                return false;
            }
            else if (ControlCommon.ControlType == GRControlType.grctPictureBox)
            {
                System.Windows.Forms.MessageBox.Show("PictureBox类型不能替换文本");
                return false;
            }
            else if (ControlCommon.ControlType == GRControlType.grctMemoBox)
            {
                IGRMemoBox control = ControlCommon.AsMemoBox;
                control.Text = text;
            }
            else if (ControlCommon.ControlType == GRControlType.grctSubReport)
            {
                System.Windows.Forms.MessageBox.Show("SubReport类型不能替换文本");
                return false;
            }
            else if (ControlCommon.ControlType == GRControlType.grctLine)
            {
                System.Windows.Forms.MessageBox.Show("Line类型不能替换文本");
                return false;
            }
            else if (ControlCommon.ControlType == GRControlType.grctChart)
            {
                System.Windows.Forms.MessageBox.Show("Chart类型不能替换文本");
                return false;
            }
            else if (ControlCommon.ControlType == GRControlType.grctSystemVarBox)
            {
                System.Windows.Forms.MessageBox.Show("SystemVarBox类型不能替换文本");
                return false;
            }
            else if (ControlCommon.ControlType == GRControlType.grctFreeGrid)
            {
                System.Windows.Forms.MessageBox.Show("FreeGrid类型不能替换文本");
                return false;
            }
#endif
            return true;
        }
    }
}
