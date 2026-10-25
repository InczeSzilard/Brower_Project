    using System;
    using System.Windows.Forms;

    namespace Lab3Window
    {
        // Window for typing a new keyword: a label, a textbox, OK and Cancel
        public partial class AddKeywordForm : Form
        {
            public AddKeywordForm()
            {
                InitializeComponent();

                // OK closes the window and ShowDialog() returns DialogResult.OK,
                // Cancel closes it and returns DialogResult.Cancel
                btnOK.DialogResult = DialogResult.OK;
                btnCancel.DialogResult = DialogResult.Cancel;
            }

            // Returns the text typed in the textbox
            public string getInputText()
            {
                return txtKeyword.Text;
            }
        }
    }