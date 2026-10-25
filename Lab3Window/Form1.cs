using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;
namespace Lab3Window
{
    public partial class Form1 : Form
    {
        // Files are created next to the .exe
        private static readonly string baseDir = AppDomain.CurrentDomain.BaseDirectory;

        private SQLiteHandler db = new SQLiteHandler(Path.Combine(baseDir, "browser.sqlite"));
        private KeywordFilter filter = new KeywordFilter();
        private AppLogger logger = new AppLogger(Path.Combine(baseDir, "browser_log.txt"));

        // Words saved in the database the very first time the program runs
        private readonly string[] defaultKeywords = { "facebook", "tiktok", "gambling", "instagram" };

        // true = the next Navigating event was already checked
        private bool skipCheck = false;

        public Form1()
        {
            SetBrowserEmulation();
            InitializeComponent();
            webBrowser1.ScriptErrorsSuppressed = true;
            Text = "My Browser";

            // -= first, so a handler is never connected twice
            toolStripTextBox1.KeyDown -= toolStripTextBox1_KeyDown;
            toolStripTextBox1.KeyDown += toolStripTextBox1_KeyDown;

           

            webBrowser1.Navigating -= webBrowser1_Navigating;
            webBrowser1.Navigating += webBrowser1_Navigating;

            addKeywordToolStripMenuItem.Click -= addKeywordToolStripMenuItem_Click;
            addKeywordToolStripMenuItem.Click += addKeywordToolStripMenuItem_Click;

            viewKeywordsToolStripMenuItem.Click -= viewKeywordsToolStripMenuItem_Click;
            viewKeywordsToolStripMenuItem.Click += viewKeywordsToolStripMenuItem_Click;

            deleteKeywordToolStripMenuItem.Click -= deleteKeywordToolStripMenuItem_Click;
            deleteKeywordToolStripMenuItem.Click += deleteKeywordToolStripMenuItem_Click;

            aboutToolStripMenuItem.Click -= aboutToolStripMenuItem_Click;
            aboutToolStripMenuItem.Click += aboutToolStripMenuItem_Click;

            // The program connects by itself, so these are not needed anymore
            connectToolStripMenuItem.Visible = false;
            disconnectToolStripMenuItem.Visible = false;

            FormClosing += (s, e) => db.DisconnectFromDb();

            ConnectAtStartup();
        }

        // Opens the database, loads the keywords into the filter and the combo box
        private void ConnectAtStartup()
        {
            try
            {
                db.ConnectToDb();

                // Default words only the first time (new database file)
                if (db.IsNewDatabase)
                {
                    foreach (string word in defaultKeywords)
                        db.InsertKeyword(word);
                }

                filter.SetAll(db.GetAllKeywords());
                _ = logger.LogAsync("Connected to database");
            }
            catch (Exception ex)
            {
                // Blocking still works with the default words
                filter.SetAll(defaultKeywords);
                MessageBox.Show("Could not open the database: " + ex.Message);
            }

            RefreshCombo();
        }

        private void RefreshCombo()
        {
            toolStripComboBoxKeywords.Items.Clear();
            toolStripComboBoxKeywords.Items.AddRange(filter.GetAll().Cast<object>().ToArray());
        }

        // =============== Navigation ===============

        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            // Back: the page was already checked when it first opened
            if (webBrowser1.CanGoBack)
            {
                skipCheck = true;
                webBrowser1.GoBack();
            }
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            if (webBrowser1.CanGoForward)
            {
                skipCheck = true;
                webBrowser1.GoForward();
            }
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            webBrowser1.GoHome();
        }

        // Go button
        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            OpenAddress();
        }

        // Enter in the address bar
        private void toolStripTextBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                OpenAddress();
                e.SuppressKeyPress = true;
            }
        }

        // Empty handler created by the designer
        private void toolStripTextBox1_Click(object sender, EventArgs e)
        {
        }

        private void OpenAddress()
        {
            string url = toolStripTextBox1.Text.Trim();
            if (url == "") return;

            if (!url.StartsWith("http://") && !url.StartsWith("https://"))
                url = "https://" + url;

            try
            {
                webBrowser1.Navigate(url);
            }
            catch (UriFormatException)
            {
                MessageBox.Show("This address is not valid.");
            }
        }

        // Runs before every page opens: cancel, check in the background,
        // then open again only if the page is allowed
        private async void webBrowser1_Navigating(object sender, WebBrowserNavigatingEventArgs e)
        {
            // Frames inside a page are not checked
            if (!string.IsNullOrEmpty(e.TargetFrameName))
                return;

            // This navigation was already approved below
            if (skipCheck)
            {
                skipCheck = false;
                return;
            }

            e.Cancel = true;
            string url = e.Url.ToString();

            if (await filter.IsBlockedAsync(url))
            {
                _ = logger.LogAsync("Blocked page: " + url);
                MessageBox.Show("This page is blocked!", "Blocked",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                _ = logger.LogAsync("Navigated to: " + url);
                skipCheck = true;
                webBrowser1.Navigate(url);
            }
        }

        // =============== Keywords ===============

        // Enter in the keyword textbox
        

        // Adds the word to the filter, the combo box and the database.
        // Returns false if it was empty or already existed.
        private bool AddKeyword(string keyword)
        {
            keyword = keyword.Trim().ToLower();
            if (keyword == "")
                return false;

            if (!filter.Add(keyword))
            {
                MessageBox.Show("This keyword already exists!");
                return false;
            }

            if (db.IsConnected)
                db.InsertKeyword(keyword);

            toolStripComboBoxKeywords.Items.Add(keyword);
            _ = logger.LogAsync("Keyword added: " + keyword);
            return true;
        }

        // Database > AddKeyword
        private void addKeywordToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddKeywordForm form = new AddKeywordForm();

            if (form.ShowDialog() == DialogResult.OK)
            {
                string kwd = form.getInputText().Trim();

                if (kwd == "")
                {
                    MessageBox.Show("The keyword can't be empty.");
                    return;
                }

                if (AddKeyword(kwd))
                    MessageBox.Show("Keyword added: " + kwd);
            }
        }

        // Database > ViewKeywords
        private void viewKeywordsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            List<string> keywords = db.IsConnected ? db.GetAllKeywords() : filter.GetAll();

            var text = new StringBuilder();
            for (int i = 0; i < keywords.Count; i++)
                text.AppendLine((i + 1) + ". " + keywords[i]);

            MessageBox.Show(
                keywords.Count > 0 ? text.ToString() : "No keywords saved.",
                "Blocked keywords");
        }

        // Database > DeleteKeyword: deletes the word selected in the combo box
        private void deleteKeywordToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string word = toolStripComboBoxKeywords.SelectedItem as string;

            if (string.IsNullOrEmpty(word))
            {
                MessageBox.Show("Select a keyword first.");
                return;
            }

            DialogResult answer = MessageBox.Show(
                "Delete the keyword \"" + word + "\"?",
                "Delete keyword",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (answer != DialogResult.Yes)
                return;

            if (db.IsConnected)
                db.DeleteKeyword(word);

            filter.Remove(word);
            toolStripComboBoxKeywords.Items.Remove(word);
            toolStripComboBoxKeywords.Text = "";

            _ = logger.LogAsync("Keyword deleted: " + word);
        }

        // Help > About
        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(
                "My Browser\n" +
                "Mini-project 1 - Web Browser with blocked keywords (SQLite)\n" +
                "Author: Szilard",
                "About",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        // Makes the WebBrowser control use the IE11 engine instead of the old IE7 one
        private static void SetBrowserEmulation()
        {
            try
            {
                string exeName = Path.GetFileName(Application.ExecutablePath);

                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(
                    @"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION"))
                {
                    key.SetValue(exeName, 11001, RegistryValueKind.DWord);
                }
            }
            catch (Exception)
            {
                // Not important: the browser just uses the old engine
            }
        }
    }
}