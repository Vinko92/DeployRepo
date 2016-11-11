using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using System.Windows.Forms;
using GoogleScraper.Const;
using GoogleScraper.Enums;
using GoogleScraper.Parser;
using GoogleScraper.Utils;
using GoogleScraper.Worker;

namespace GoogleScraper
{
    public partial class App : Form
    {
        #region Variables
        private int index = -1;
        private int start = -1;

        private string googleResults = string.Empty;
        private BindingList<ScrapeData> urls = new BindingList<ScrapeData>();
        private Random random = new Random(DateTime.Now.Millisecond);

        private int urlWorkerSleepsFor = 0;
        private int emailWorkerSleepsFor = 0;
        private System.Windows.Forms.Timer detailsTimer;
        private Label detailsWorkerStatus;
        private Panel panel1;
        private Panel panel2;
        private Label label4;
        private Label label3;
        private Panel panel3;
        private Label appStatus;
        private bool stopScraping = false;
        #endregion

        #region Constructor
        public App()
        {
            InitializeComponent();
            Init();
        }
        #endregion

        #region Private Methods
        private void Init()
        {
            dataGrid.Visible = false;
            workerStatus.Visible = false;
            detailsWorkerStatus.Text = string.Empty;

            UpdateStatus("Application started");
            StartTimer();

            txtScrapUrlsFrom.Text = "360";
            txtScrapUrlsTo.Text = "600";

            txtScrapEmailsFrom.Text = "20";
            txtScrapEmailsTo.Text = "40";

            pauseButton.Enabled = !startButton.Enabled;
            stopButton.Enabled = !startButton.Enabled;

            StartPosition = FormStartPosition.CenterScreen;
        }

        private async void ScrapeDetails(bool skipChecking = false)
        {
            UpdateStatus("Scraping details.", true);
            int emailScraperSleepsFrom = int.Parse(txtScrapEmailsFrom.Text);
            int emailScraperSleepsTo = int.Parse(txtScrapEmailsTo.Text);
            emailWorkerSleepsFor = random.Next(emailScraperSleepsFrom, emailScraperSleepsTo);

            try
            {
                ScrapeWorker worker = new ScrapeWorker();

                string rawHtmlText = await worker.ScrapeDetailsAsync(urls[index].WebsiteUrl);

                var emails = keywords.Text.Split(' ').Where(x => x.Contains("@"));

                if (skipChecking)
                {
                    urls[index].Email = HtmlParser.ParseEmail(rawHtmlText, emails);
                    urls[index].PhoneNumber = HtmlParser.ParsePhone(rawHtmlText);
                    urls[index].CompanyName = HtmlParser.ParseCompanyName(rawHtmlText);
                }
                else
                {
                    if (cbhEmail.Checked) urls[index].Email = HtmlParser.ParseEmail(rawHtmlText, emails);
                    if (cbhPhoneNumber.Checked) urls[index].PhoneNumber = HtmlParser.ParsePhone(rawHtmlText);
                    if (cbhCompanyName.Checked) urls[index].CompanyName = HtmlParser.ParseCompanyName(rawHtmlText);
                }

                dataGrid.Rows[index].Selected = true;

                BindDataGrid();

               

                if (!skipChecking)
                    UpdateDetailsScraperWorkerStatus(null, new EventArgs());
            }
            catch (Exception ex)
            {
                UpdateStatus(ex.Message);
            }

            StartTimer();
        }

        private void ScrapeUrl(int start)
        {
            UpdateStatus("Scraping urls.", true);
            int intervalFrom = int.Parse(txtScrapUrlsFrom.Text);
            int intervalTo = int.Parse(txtScrapUrlsTo.Text);
            urlWorkerSleepsFor = random.Next(intervalFrom, intervalTo);

            try
            {
                ScrapeWorker worker = new ScrapeWorker();

                googleResults = worker.ScrapeUrlsAsync(BuildUrl(keywords.Text, start));
                HtmlParser.ParseUrls(googleResults).ForEach(x => urls.Add(x));

                BindDataGrid();

                UpdateStatus("Scraping done.");
                StartTimer();

                workerTimer.Start();
                UpdateWorkerStatus(null, new EventArgs());
            }
            catch (Exception ex)
            {
                UpdateStatus(ex.Message);
                string autosavePath = Config.GetAutosavePath();

                if (!string.IsNullOrEmpty(autosavePath))
                {
                    Save(Config.GetAutosavePath());
                }
            }
        }

        private void BindDataGrid()
        {
            dataGrid.Visible = true;
            dataGrid.DataSource = urls;
            dataGrid.AutoResizeRows();
            dataGrid.Update();
        }

        private void ClearDataGrid()
        {
            dataGrid.DataSource = null;
            urls.Clear();
            dataGrid.Invalidate();
        }

        private Uri BuildUrl(string keywords, int start)
        {
            Uri uri = new Uri(Constants.GOOGLE_URL);
            string encodedKeywords = HttpUtility.UrlEncode(keywords);

            if (!string.IsNullOrEmpty(keywords))
            {
                uri = new Uri(string.Format("{0}/search?q={1}&num=100&start={2}",
                                  Constants.GOOGLE_URL, encodedKeywords, start));
            }

            return uri;
        }

        private void InvalidateButtons(ButtonsState state)
        {
            switch (state)
            {
                case ButtonsState.Paused:
                    pauseButton.Text = "RESUME";
                    startButton.Enabled = false;
                    break;
                case ButtonsState.Resumed:
                    pauseButton.Text = "PAUSE";
                    startButton.Enabled = false;
                    break;
                case ButtonsState.Started:
                    startButton.Enabled = false;
                    break;
                case ButtonsState.Stopped:
                    startButton.Enabled = true;
                    break;
                default:
                    break;

            }

            stopButton.Enabled = !startButton.Enabled;
            pauseButton.Enabled = !startButton.Enabled;
            saveButton.Enabled = startButton.Enabled;
            loadFileButton.Enabled = startButton.Enabled;
            removeDuplicatesButton.Enabled = startButton.Enabled;
            detailsButton.Enabled = startButton.Enabled;

        }

        private void UpdateStatus(string message, bool useSpinner = false)
        {
            timer1.Stop();
            appStatus.Text = message;


            Padding margin = appStatus.Margin;
            margin.Left = appStatus.Width + 10;

            statusSpinner.Margin = margin;
            statusSpinner.Visible = useSpinner;
            statusBar.Invalidate();
        }

        private void StartTimer()
        {
            timer1.Start();
        }



        #endregion

        #region Events

        #region Button Events
        private void Start(object sender, EventArgs e)
        {
            index = -1;
            if (urls.Count > 0)
            {
                if (MessageBox.Show("Do you want to drop previously scraped data?", "Drop data", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    ClearDataGrid();
                }
            }

            ScrapeUrl(++start);

            stopScraping = false;

            InvalidateButtons(ButtonsState.Started);

            if (cbhScrapeUrlsAnd.Checked)
            {
                UpdateDetailsScraperWorkerStatus(null, new EventArgs());
                detailsTimer.Start();
            }

            UpdateWorkerStatus(null, new EventArgs());

            workerTimer.Start();
        }

        private void Save(object sender, EventArgs e)
        {
            SaveToFile(sender, e);
        }

        private void SaveToFile(object sender, EventArgs args)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = dialog.Filter = "xls files (*.xls)|";
            dialog.RestoreDirectory = true;

            UpdateStatus("Saving urls to file.", true);

            try
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    OleDbHelper helper = new OleDbHelper();
                    helper.CreateTableIfNotExists(dialog.FileName, "URLS");

                    foreach (var scrapedData in urls)
                    {
                        helper.InsertRow(dialog.FileName, scrapedData.WebsiteUrl, scrapedData.Root, scrapedData.Email, scrapedData.PhoneNumber, scrapedData.CompanyName);
                    }

                    UpdateStatus("File saved.");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus(ex.Message);
            }


            StartTimer();
        }

        private void Save(string filepath)
        {
            OleDbHelper helper = new OleDbHelper();
            helper.CreateTableIfNotExists(filepath, "URLS");

            foreach (var scrapedData in urls)
            {
                helper.InsertRow(filepath, scrapedData.WebsiteUrl, scrapedData.Root, scrapedData.Email, scrapedData.PhoneNumber, scrapedData.CompanyName);
            }
        }

        /// <summary>
        /// Load excel file with urls.
        /// </summary>
        /// <param name="sender">Button.</param>
        /// <param name="e">Event arguments.</param>
        private void LoadFile(object sender, EventArgs e)
        {
            UpdateStatus("Loading urls from file.", true);

            if (urls.Count > 0)
            {
                if (MessageBox.Show("Do you want to drop previously scraped data?", "Drop data", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    ClearDataGrid();
                }
            }

            OleDbHelper helper = new OleDbHelper();

            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "xls files (*.xls)|";
            dialog.RestoreDirectory = true;

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                helper.ReadFile(dialog.FileName).ForEach(x => urls.Add(x));

                BindDataGrid();
            }

            UpdateStatus(string.Format("Loaded {0} results.", urls.Count));
            StartTimer();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            if (urls.Count == 0)
            {
                MessageBox.Show("Search grid is empty.");
            }
            else
            {
                BindingList<ScrapeData> tempList = new BindingList<ScrapeData>();
                urls.ToList().Distinct().ToList().ForEach(x => tempList.Add(x));
                urls = tempList;

                BindDataGrid();
            }
            UpdateStatus(string.Format("{0} results.", urls.Count));
            StartTimer();
        }

        private void Stop(object sender, EventArgs e)
        {
            start = 0;
            index = 0;
            stopScraping = true;
            urlWorkerSleepsFor = 0;
            emailWorkerSleepsFor = 0;
            InvalidateButtons(ButtonsState.Stopped);

            UpdateWorkerStatus(null, new EventArgs());
        }

        private void PauseAndResume(object sender, EventArgs e)
        {
            Button button = sender as Button;

            if (button.Text == "PAUSE")
            {
                workerStatus.Visible = true;
                workerTimer.Stop();
                workerStatus.Text = string.Format("Url worker paused");

                detailsTimer.Stop();
                detailsWorkerStatus.Text = string.Format("Details worker paused");

                InvalidateButtons(ButtonsState.Paused);
            }
            else
            {
                workerTimer.Start();
                UpdateWorkerStatus(null, new EventArgs());


                detailsTimer.Start();
                UpdateDetailsScraperWorkerStatus(null, new EventArgs());

                InvalidateButtons(ButtonsState.Resumed);
            }
        }


        private void detailsButton_Click(object sender, EventArgs e)
        {
            if (dataGrid.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please, select row within Search results.");
            }
            else
            {
                index = dataGrid.SelectedRows[0].Index;
                ScrapeDetails(true);
                UpdateStatus("Scraping details.", true);
                StartTimer();
            }
        }
        #endregion

        #region Timer events

        private void timer1_Tick(object sender, EventArgs e)
        {
            appStatus.Text = string.Empty;
            statusSpinner.Visible = false;
        }

        private void UpdateWorkerStatus(object sender, EventArgs args)
        {
            workerStatus.Visible = true;
            if (urlWorkerSleepsFor <= 0 && !stopScraping)
            {
                ScrapeUrl(++start);
            }
            else if (stopScraping)
            {
                workerTimer.Stop();
                UpdateStatus("Scraping urls stopped.");
                workerStatus.Visible = false;
            }
            else
            {
                urlWorkerSleepsFor--;
            }

            string timeFormat = string.Format("{0}:{1}", urlWorkerSleepsFor / 60, (urlWorkerSleepsFor % 60).ToString("D2"));
            workerStatus.Text = string.Format("Url worker sleeps for {0}", timeFormat);
            StartTimer();
        }

        private void UpdateDetailsScraperWorkerStatus(object sender, EventArgs args)
        {
            if (emailWorkerSleepsFor <= 0 && !stopScraping && urls.Count > 0)
            {
                detailsWorkerStatus.Visible = true;
                ++index;

                if (index != 0 && dataGrid.Rows.Count > 0)
                {
                    dataGrid.Rows[index - 1].Selected = false;
                }

                ScrapeDetails();
            }
            else if (stopScraping || index >= urls.Count)
            {
                detailsTimer.Stop();
                UpdateStatus("Scraping details stopped.");
                detailsWorkerStatus.Visible = false;
            }
            else
            {
                emailWorkerSleepsFor--;
            }

            if (urls.Count > 0)
            {
                string timeFormat = string.Format("{0}:{1}", emailWorkerSleepsFor / 60, (emailWorkerSleepsFor % 60).ToString("D2"));
                detailsWorkerStatus.Text = string.Format("Details worker sleeps for {0}", timeFormat);

            }
        }

        #endregion

        #region Checkbox Events
        private void cbhUrlsOnly_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox urlsOnly = sender as CheckBox;

            if (urlsOnly.Checked)
            {
                cbhScrapeUrlsAnd.Checked = false;
                cbhCompanyName.Checked = false;
                cbhEmail.Checked = false;
                cbhPhoneNumber.Checked = false;

                cbhCompanyName.Enabled = false;
                cbhEmail.Enabled = false;
                cbhPhoneNumber.Enabled = false;
            }
        }

        private void cbhScrapeUrlsAnd_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox urlsAnd = sender as CheckBox;

            if (urlsAnd.Checked)
            {
                cbhScrapeUrlsOnly.Checked = false;

                cbhCompanyName.Enabled = true;
                cbhEmail.Enabled = true;
                cbhPhoneNumber.Enabled = true;
            }
            else
            {
                cbhCompanyName.Enabled = false;
                cbhEmail.Enabled = false;
                cbhPhoneNumber.Enabled = false;
            }

        }

        #endregion

        #endregion


        #region Autogenerated
        private Button startButton;
        private Button stopButton;
        private Button pauseButton;
        private Button saveButton;
        private Button loadFileButton;
        private Button removeDuplicatesButton;
        private Button detailsButton;
        private GroupBox groupBox1;
        private GroupBox groupBox2;
        private CheckBox cbhScrapeUrlsAnd;
        private CheckBox cbhScrapeUrlsOnly;
        private CheckBox cbhCompanyName;
        private CheckBox cbhPhoneNumber;
        private CheckBox cbhEmail;
        private TextBox keywords;
        private GroupBox groupBox3;
        private Label label2;
        private Label label1;
        private TextBox txtScrapEmailsTo;
        private TextBox txtScrapEmailsFrom;
        private TextBox txtScrapUrlsTo;
        private TextBox txtScrapUrlsFrom;
        private Label label7;
        private Label label8;
        private Label label9;
        private Label label10;
        private Panel statusBar;
        private Label workerStatus;
        private PictureBox statusSpinner;
        private Label status;
        private System.Windows.Forms.Timer timer1;
        private System.ComponentModel.IContainer components;
        private GroupBox groupBox4;
        private DataGridView dataGrid;
        private System.Windows.Forms.Timer workerTimer;

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.startButton = new System.Windows.Forms.Button();
            this.stopButton = new System.Windows.Forms.Button();
            this.pauseButton = new System.Windows.Forms.Button();
            this.saveButton = new System.Windows.Forms.Button();
            this.loadFileButton = new System.Windows.Forms.Button();
            this.removeDuplicatesButton = new System.Windows.Forms.Button();
            this.detailsButton = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.cbhScrapeUrlsAnd = new System.Windows.Forms.CheckBox();
            this.cbhScrapeUrlsOnly = new System.Windows.Forms.CheckBox();
            this.cbhCompanyName = new System.Windows.Forms.CheckBox();
            this.cbhPhoneNumber = new System.Windows.Forms.CheckBox();
            this.cbhEmail = new System.Windows.Forms.CheckBox();
            this.keywords = new System.Windows.Forms.TextBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.txtScrapEmailsTo = new System.Windows.Forms.TextBox();
            this.txtScrapEmailsFrom = new System.Windows.Forms.TextBox();
            this.txtScrapUrlsTo = new System.Windows.Forms.TextBox();
            this.txtScrapUrlsFrom = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.statusBar = new System.Windows.Forms.Panel();
            this.panel3 = new System.Windows.Forms.Panel();
            this.appStatus = new System.Windows.Forms.Label();
            this.statusSpinner = new System.Windows.Forms.PictureBox();
            this.panel2 = new System.Windows.Forms.Panel();
            this.label4 = new System.Windows.Forms.Label();
            this.detailsWorkerStatus = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.label3 = new System.Windows.Forms.Label();
            this.workerStatus = new System.Windows.Forms.Label();
            this.status = new System.Windows.Forms.Label();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.dataGrid = new System.Windows.Forms.DataGridView();
            this.workerTimer = new System.Windows.Forms.Timer(this.components);
            this.detailsTimer = new System.Windows.Forms.Timer(this.components);
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.statusBar.SuspendLayout();
            this.panel3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.statusSpinner)).BeginInit();
            this.panel2.SuspendLayout();
            this.panel1.SuspendLayout();
            this.groupBox4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGrid)).BeginInit();
            this.SuspendLayout();
            // 
            // startButton
            // 
            this.startButton.Location = new System.Drawing.Point(19, 39);
            this.startButton.Name = "startButton";
            this.startButton.Size = new System.Drawing.Size(75, 23);
            this.startButton.TabIndex = 30;
            this.startButton.Text = "START";
            this.startButton.UseVisualStyleBackColor = true;
            this.startButton.Click += new System.EventHandler(this.Start);
            // 
            // stopButton
            // 
            this.stopButton.Location = new System.Drawing.Point(19, 68);
            this.stopButton.Name = "stopButton";
            this.stopButton.Size = new System.Drawing.Size(75, 23);
            this.stopButton.TabIndex = 31;
            this.stopButton.Text = "STOP";
            this.stopButton.UseVisualStyleBackColor = true;
            this.stopButton.Click += new System.EventHandler(this.Stop);
            // 
            // pauseButton
            // 
            this.pauseButton.Location = new System.Drawing.Point(19, 97);
            this.pauseButton.Name = "pauseButton";
            this.pauseButton.Size = new System.Drawing.Size(75, 23);
            this.pauseButton.TabIndex = 32;
            this.pauseButton.Text = "PAUSE";
            this.pauseButton.UseVisualStyleBackColor = true;
            this.pauseButton.Click += new System.EventHandler(this.PauseAndResume);
            // 
            // saveButton
            // 
            this.saveButton.Location = new System.Drawing.Point(19, 126);
            this.saveButton.Name = "saveButton";
            this.saveButton.Size = new System.Drawing.Size(75, 23);
            this.saveButton.TabIndex = 33;
            this.saveButton.Text = "SAVE";
            this.saveButton.UseVisualStyleBackColor = true;
            this.saveButton.Click += new System.EventHandler(this.Save);
            // 
            // loadFileButton
            // 
            this.loadFileButton.Location = new System.Drawing.Point(19, 168);
            this.loadFileButton.Name = "loadFileButton";
            this.loadFileButton.Size = new System.Drawing.Size(104, 23);
            this.loadFileButton.TabIndex = 34;
            this.loadFileButton.Text = "LOAD URL LIST";
            this.loadFileButton.UseVisualStyleBackColor = true;
            this.loadFileButton.Click += new System.EventHandler(this.LoadFile);
            // 
            // removeDuplicatesButton
            // 
            this.removeDuplicatesButton.Location = new System.Drawing.Point(19, 197);
            this.removeDuplicatesButton.Name = "removeDuplicatesButton";
            this.removeDuplicatesButton.Size = new System.Drawing.Size(104, 23);
            this.removeDuplicatesButton.TabIndex = 35;
            this.removeDuplicatesButton.Text = "REMOVE DUP. URLS";
            this.removeDuplicatesButton.UseVisualStyleBackColor = true;
            this.removeDuplicatesButton.Click += new System.EventHandler(this.button6_Click);
            // 
            // detailsButton
            // 
            this.detailsButton.Location = new System.Drawing.Point(19, 226);
            this.detailsButton.Name = "detailsButton";
            this.detailsButton.Size = new System.Drawing.Size(104, 23);
            this.detailsButton.TabIndex = 36;
            this.detailsButton.Text = "DETAILS";
            this.detailsButton.UseVisualStyleBackColor = true;
            this.detailsButton.Click += new System.EventHandler(this.detailsButton_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.groupBox1.Controls.Add(this.detailsButton);
            this.groupBox1.Controls.Add(this.removeDuplicatesButton);
            this.groupBox1.Controls.Add(this.saveButton);
            this.groupBox1.Controls.Add(this.loadFileButton);
            this.groupBox1.Controls.Add(this.pauseButton);
            this.groupBox1.Controls.Add(this.startButton);
            this.groupBox1.Controls.Add(this.stopButton);
            this.groupBox1.Location = new System.Drawing.Point(757, 17);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(236, 328);
            this.groupBox1.TabIndex = 4;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "FUNCTIONS";
            // 
            // groupBox2
            // 
            this.groupBox2.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.groupBox2.Controls.Add(this.cbhScrapeUrlsAnd);
            this.groupBox2.Controls.Add(this.cbhScrapeUrlsOnly);
            this.groupBox2.Controls.Add(this.cbhCompanyName);
            this.groupBox2.Controls.Add(this.cbhPhoneNumber);
            this.groupBox2.Controls.Add(this.cbhEmail);
            this.groupBox2.Controls.Add(this.keywords);
            this.groupBox2.Location = new System.Drawing.Point(21, 17);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(731, 191);
            this.groupBox2.TabIndex = 5;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "KEYWORDS";
            // 
            // cbhScrapeUrlsAnd
            // 
            this.cbhScrapeUrlsAnd.AutoSize = true;
            this.cbhScrapeUrlsAnd.Location = new System.Drawing.Point(373, 50);
            this.cbhScrapeUrlsAnd.Name = "cbhScrapeUrlsAnd";
            this.cbhScrapeUrlsAnd.Size = new System.Drawing.Size(110, 17);
            this.cbhScrapeUrlsAnd.TabIndex = 7;
            this.cbhScrapeUrlsAnd.Text = "SCRAPE URLS +";
            this.cbhScrapeUrlsAnd.UseVisualStyleBackColor = true;
            this.cbhScrapeUrlsAnd.CheckedChanged += new System.EventHandler(this.cbhScrapeUrlsAnd_CheckedChanged);
            // 
            // cbhScrapeUrlsOnly
            // 
            this.cbhScrapeUrlsOnly.AutoSize = true;
            this.cbhScrapeUrlsOnly.Checked = true;
            this.cbhScrapeUrlsOnly.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbhScrapeUrlsOnly.Location = new System.Drawing.Point(373, 29);
            this.cbhScrapeUrlsOnly.Name = "cbhScrapeUrlsOnly";
            this.cbhScrapeUrlsOnly.Size = new System.Drawing.Size(133, 17);
            this.cbhScrapeUrlsOnly.TabIndex = 6;
            this.cbhScrapeUrlsOnly.Text = "SCRAPE URLS ONLY";
            this.cbhScrapeUrlsOnly.UseVisualStyleBackColor = true;
            this.cbhScrapeUrlsOnly.CheckedChanged += new System.EventHandler(this.cbhUrlsOnly_CheckedChanged);
            // 
            // cbhCompanyName
            // 
            this.cbhCompanyName.AutoSize = true;
            this.cbhCompanyName.Enabled = false;
            this.cbhCompanyName.Location = new System.Drawing.Point(438, 119);
            this.cbhCompanyName.Name = "cbhCompanyName";
            this.cbhCompanyName.Size = new System.Drawing.Size(113, 17);
            this.cbhCompanyName.TabIndex = 5;
            this.cbhCompanyName.Text = "COMPANY NAME";
            this.cbhCompanyName.UseVisualStyleBackColor = true;
            // 
            // cbhPhoneNumber
            // 
            this.cbhPhoneNumber.AutoSize = true;
            this.cbhPhoneNumber.Enabled = false;
            this.cbhPhoneNumber.Location = new System.Drawing.Point(438, 96);
            this.cbhPhoneNumber.Name = "cbhPhoneNumber";
            this.cbhPhoneNumber.Size = new System.Drawing.Size(114, 17);
            this.cbhPhoneNumber.TabIndex = 4;
            this.cbhPhoneNumber.Text = "PHONE NUMBER";
            this.cbhPhoneNumber.UseVisualStyleBackColor = true;
            // 
            // cbhEmail
            // 
            this.cbhEmail.AutoSize = true;
            this.cbhEmail.Enabled = false;
            this.cbhEmail.Location = new System.Drawing.Point(438, 73);
            this.cbhEmail.Name = "cbhEmail";
            this.cbhEmail.Size = new System.Drawing.Size(58, 17);
            this.cbhEmail.TabIndex = 3;
            this.cbhEmail.Text = "EMAIL";
            this.cbhEmail.UseVisualStyleBackColor = true;
            // 
            // keywords
            // 
            this.keywords.Location = new System.Drawing.Point(6, 29);
            this.keywords.Multiline = true;
            this.keywords.Name = "keywords";
            this.keywords.Size = new System.Drawing.Size(361, 147);
            this.keywords.TabIndex = 1;
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.label2);
            this.groupBox3.Controls.Add(this.label1);
            this.groupBox3.Controls.Add(this.txtScrapEmailsTo);
            this.groupBox3.Controls.Add(this.txtScrapEmailsFrom);
            this.groupBox3.Controls.Add(this.txtScrapUrlsTo);
            this.groupBox3.Controls.Add(this.txtScrapUrlsFrom);
            this.groupBox3.Controls.Add(this.label7);
            this.groupBox3.Controls.Add(this.label8);
            this.groupBox3.Controls.Add(this.label9);
            this.groupBox3.Controls.Add(this.label10);
            this.groupBox3.Location = new System.Drawing.Point(21, 212);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(731, 133);
            this.groupBox3.TabIndex = 6;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "BASIC SETTINGS";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(17, 71);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(165, 13);
            this.label2.TabIndex = 29;
            this.label2.Text = "CLICK SPEED EMAIL SCRAPER";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(17, 44);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(155, 13);
            this.label1.TabIndex = 28;
            this.label1.Text = "CLICK SPEED URL SCRAPER";
            // 
            // txtScrapEmailsTo
            // 
            this.txtScrapEmailsTo.Location = new System.Drawing.Point(641, 64);
            this.txtScrapEmailsTo.Name = "txtScrapEmailsTo";
            this.txtScrapEmailsTo.Size = new System.Drawing.Size(53, 20);
            this.txtScrapEmailsTo.TabIndex = 27;
            // 
            // txtScrapEmailsFrom
            // 
            this.txtScrapEmailsFrom.Location = new System.Drawing.Point(470, 64);
            this.txtScrapEmailsFrom.Name = "txtScrapEmailsFrom";
            this.txtScrapEmailsFrom.Size = new System.Drawing.Size(53, 20);
            this.txtScrapEmailsFrom.TabIndex = 26;
            // 
            // txtScrapUrlsTo
            // 
            this.txtScrapUrlsTo.Location = new System.Drawing.Point(641, 41);
            this.txtScrapUrlsTo.Name = "txtScrapUrlsTo";
            this.txtScrapUrlsTo.Size = new System.Drawing.Size(53, 20);
            this.txtScrapUrlsTo.TabIndex = 25;
            // 
            // txtScrapUrlsFrom
            // 
            this.txtScrapUrlsFrom.Location = new System.Drawing.Point(470, 41);
            this.txtScrapUrlsFrom.Name = "txtScrapUrlsFrom";
            this.txtScrapUrlsFrom.Size = new System.Drawing.Size(53, 20);
            this.txtScrapUrlsFrom.TabIndex = 24;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(568, 71);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(30, 13);
            this.label7.TabIndex = 23;
            this.label7.Text = "AND";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(347, 71);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(92, 13);
            this.label8.TabIndex = 22;
            this.label8.Text = "WAIT BETWEEN";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(568, 44);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(30, 13);
            this.label9.TabIndex = 21;
            this.label9.Text = "AND";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(347, 44);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(92, 13);
            this.label10.TabIndex = 20;
            this.label10.Text = "WAIT BETWEEN";
            // 
            // statusBar
            // 
            this.statusBar.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.statusBar.Controls.Add(this.panel3);
            this.statusBar.Controls.Add(this.panel2);
            this.statusBar.Controls.Add(this.panel1);
            this.statusBar.Controls.Add(this.status);
            this.statusBar.Location = new System.Drawing.Point(0, 685);
            this.statusBar.Name = "statusBar";
            this.statusBar.Size = new System.Drawing.Size(1007, 27);
            this.statusBar.TabIndex = 7;
            // 
            // panel3
            // 
            this.panel3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel3.Controls.Add(this.appStatus);
            this.panel3.Controls.Add(this.statusSpinner);
            this.panel3.Location = new System.Drawing.Point(0, 0);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(358, 27);
            this.panel3.TabIndex = 7;
            // 
            // appStatus
            // 
            this.appStatus.AutoSize = true;
            this.appStatus.Location = new System.Drawing.Point(3, 6);
            this.appStatus.Name = "appStatus";
            this.appStatus.Size = new System.Drawing.Size(35, 13);
            this.appStatus.TabIndex = 0;
            this.appStatus.Text = "label6";
            // 
            // statusSpinner
            // 
            this.statusSpinner.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.statusSpinner.BackgroundImage = global::GoogleScraper.Properties.Resources.firefox_spinner;
            this.statusSpinner.Cursor = System.Windows.Forms.Cursors.Default;
            this.statusSpinner.Image = global::GoogleScraper.Properties.Resources.firefox_spinner;
            this.statusSpinner.Location = new System.Drawing.Point(337, 4);
            this.statusSpinner.Name = "statusSpinner";
            this.statusSpinner.Size = new System.Drawing.Size(16, 16);
            this.statusSpinner.TabIndex = 1;
            this.statusSpinner.TabStop = false;
            // 
            // panel2
            // 
            this.panel2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel2.Controls.Add(this.label4);
            this.panel2.Controls.Add(this.detailsWorkerStatus);
            this.panel2.Location = new System.Drawing.Point(662, 0);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(345, 27);
            this.panel2.TabIndex = 6;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(4, 6);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(111, 13);
            this.label4.TabIndex = 5;
            this.label4.Text = "Details scraper status:";
            // 
            // detailsWorkerStatus
            // 
            this.detailsWorkerStatus.AutoSize = true;
            this.detailsWorkerStatus.Location = new System.Drawing.Point(112, 7);
            this.detailsWorkerStatus.Name = "detailsWorkerStatus";
            this.detailsWorkerStatus.Size = new System.Drawing.Size(35, 13);
            this.detailsWorkerStatus.TabIndex = 4;
            this.detailsWorkerStatus.Text = "label4";
            // 
            // panel1
            // 
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel1.Controls.Add(this.label3);
            this.panel1.Controls.Add(this.workerStatus);
            this.panel1.Location = new System.Drawing.Point(351, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(311, 27);
            this.panel1.TabIndex = 5;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 6);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(92, 13);
            this.label3.TabIndex = 4;
            this.label3.Text = "Url scraper status:";
            // 
            // workerStatus
            // 
            this.workerStatus.AutoSize = true;
            this.workerStatus.Location = new System.Drawing.Point(101, 7);
            this.workerStatus.Name = "workerStatus";
            this.workerStatus.Size = new System.Drawing.Size(35, 13);
            this.workerStatus.TabIndex = 3;
            this.workerStatus.Text = "label4";
            this.workerStatus.Click += new System.EventHandler(this.workerStatus_Click);
            // 
            // status
            // 
            this.status.AutoSize = true;
            this.status.Location = new System.Drawing.Point(14, 4);
            this.status.Name = "status";
            this.status.Size = new System.Drawing.Size(0, 13);
            this.status.TabIndex = 0;
            // 
            // timer1
            // 
            this.timer1.Interval = 3000;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.dataGrid);
            this.groupBox4.Location = new System.Drawing.Point(21, 351);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(972, 331);
            this.groupBox4.TabIndex = 8;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "SEARCH GRID";
            // 
            // dataGrid
            // 
            this.dataGrid.AllowUserToAddRows = false;
            this.dataGrid.AllowUserToDeleteRows = false;
            this.dataGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
            this.dataGrid.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
            this.dataGrid.BackgroundColor = System.Drawing.SystemColors.ButtonHighlight;
            this.dataGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGrid.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
            this.dataGrid.Location = new System.Drawing.Point(3, 16);
            this.dataGrid.MultiSelect = false;
            this.dataGrid.Name = "dataGrid";
            this.dataGrid.ReadOnly = true;
            this.dataGrid.RowHeadersWidth = 10;
            this.dataGrid.Size = new System.Drawing.Size(966, 312);
            this.dataGrid.TabIndex = 1;
            // 
            // workerTimer
            // 
            this.workerTimer.Interval = 1000;
            this.workerTimer.Tick += new System.EventHandler(this.UpdateWorkerStatus);
            // 
            // detailsTimer
            // 
            this.detailsTimer.Interval = 1000;
            this.detailsTimer.Tick += new System.EventHandler(this.UpdateDetailsScraperWorkerStatus);
            // 
            // App
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.ClientSize = new System.Drawing.Size(1005, 712);
            this.Controls.Add(this.groupBox4);
            this.Controls.Add(this.statusBar);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Name = "App";
            this.Text = "Google Scrapere";
            this.groupBox1.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.statusBar.ResumeLayout(false);
            this.statusBar.PerformLayout();
            this.panel3.ResumeLayout(false);
            this.panel3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.statusSpinner)).EndInit();
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.groupBox4.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGrid)).EndInit();
            this.ResumeLayout(false);

        }

        private void workerStatus_Click(object sender, EventArgs e)
        {

        }
        #endregion

    }
}
